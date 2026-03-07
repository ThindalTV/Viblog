using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Infrastructure.Shared.Helpers;

namespace Viblog.Data.AzureStorage.Storage;

/// <summary>
/// Azure Blob Storage implementation of media storage repository
/// </summary>
public class AzureBlobStorageMediaRepository : IMediaStorageRepository
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly string? _cdnUrl;
    private readonly ILogger<AzureBlobStorageMediaRepository> _logger;

    public AzureBlobStorageMediaRepository(
        IConfiguration configuration,
        ILogger<AzureBlobStorageMediaRepository> logger)
    {
        _logger = logger;

        var connectionString =
            configuration.GetConnectionString("blogStorage") // Aspire
            ?? configuration["MediaStorage:BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage ConnectionString is not configured");

        var rawContainerName = configuration["MediaStorage:BlobStorage:ContainerName"]
            ?? throw new InvalidOperationException("BlobStorage ContainerName is not configured");

        // Azure Blob Storage container names must be lowercase
        _containerName = rawContainerName.ToLowerInvariant();

        // Treat empty string the same as null — avoids malformed relative URLs like "/images/..."
        var cdnUrl = configuration["MediaStorage:BlobStorage:CdnUrl"];
        _cdnUrl = string.IsNullOrWhiteSpace(cdnUrl) ? null : cdnUrl;

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    /// <inheritdoc/>
    public async Task<MediaStorageResult> UploadAsync(
        string fileName,
        Stream fileStream,
        string mimeType,
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            // Determine media type category and get folder name
            var category = MediaTypeHelper.GetCategory(mimeType, fileName);
            var categoryFolder = MediaTypeHelper.GetFolderName(category);

            // Generate storage path: category/yyyy/MM/filename with collision handling
            var now = DateTimeOffset.UtcNow;
            var datePath = $"{now:yyyy}/{now:MM}";
            var baseFileName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            // Sanitize filename
            baseFileName = MediaFileNameSanitizer.Sanitize(baseFileName);

            // Try to find an available filename (handle collisions)
            var storagePath = await FindAvailableFileNameAsync(
                containerClient,
                categoryFolder,
                datePath,
                baseFileName,
                extension,
                cancellationToken);

            var blobClient = containerClient.GetBlobClient(storagePath);

            // Set content type and other headers
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = mimeType
            };

            // Upload the file
            var originalPosition = fileStream.Position;
            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                },
                cancellationToken);

            // Get file size
            fileStream.Position = originalPosition;
            var fileSize = fileStream.Length;

            // Generate public URL — percent-encode each path segment so the URL is valid
            // even if the blob name contains spaces or other characters
            var publicUrl = BuildPublicUrl(storagePath);

            _logger.LogInformation("Uploaded file to blob storage: {StoragePath}", storagePath);

            return new MediaStorageResult
            {
                StoragePath = storagePath,
                PublicUrl = publicUrl,
                FileSize = fileSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to blob storage: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Find an available filename by delegating collision logic to <see cref="MediaFileNameSanitizer.ResolveCollisionAsync"/>
    /// </summary>
    private Task<string> FindAvailableFileNameAsync(
        BlobContainerClient containerClient,
        string categoryFolder,
        string datePath,
        string baseFileName,
        string extension,
        CancellationToken cancellationToken)
    {
        var prefix = $"{categoryFolder}/{datePath}/";
        return MediaFileNameSanitizer.ResolveCollisionAsync(
            prefix,
            baseFileName,
            extension,
            async (path, ct) => (await containerClient.GetBlobClient(path).ExistsAsync(ct)).Value,
            cancellationToken);
    }

    /// <summary>
    /// Build a public URL for <paramref name="storagePath"/>.
    /// No encoding is needed because <see cref="MediaFileNameSanitizer.Sanitize"/> guarantees the
    /// filename portion contains only URL-safe ASCII characters.
    /// </summary>
    private string BuildPublicUrl(string storagePath) =>
        _cdnUrl != null
            ? $"{_cdnUrl.TrimEnd('/')}/{storagePath}"
            : $"/media/{storagePath}";

    /// <inheritdoc/>
    public async Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(storagePath);

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            
            _logger.LogInformation("Downloaded file from blob storage: {StoragePath}", storagePath);
            
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from blob storage: {StoragePath}", storagePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(storagePath);

            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            
            _logger.LogInformation("Deleted file from blob storage: {StoragePath}", storagePath);
            
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from blob storage: {StoragePath}", storagePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetPublicUrlAsync(
        string storagePath,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(storagePath);

            // If no expiration is specified, return the CDN or app-relative URL
            if (expiration == null)
            {
                return BuildPublicUrl(storagePath);
            }

            // Generate SAS token for time-limited access
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = storagePath,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiration.Value)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasToken = blobClient.GenerateSasUri(sasBuilder);
            
            return sasToken.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate public URL for blob: {StoragePath}", storagePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(storagePath);

            return await blobClient.ExistsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if blob exists: {StoragePath}", storagePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<long?> GetFileSizeAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(storagePath);

            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            var properties = await blobClient.GetPropertiesAsync();
            return properties.Value.ContentLength;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file size for blob: {StoragePath}", storagePath);
            throw;
        }
    }
}
