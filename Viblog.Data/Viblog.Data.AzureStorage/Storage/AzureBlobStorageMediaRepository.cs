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

        var connectionString = configuration["MediaStorage:BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage ConnectionString is not configured");

        _containerName = configuration["MediaStorage:BlobStorage:ContainerName"]
            ?? throw new InvalidOperationException("BlobStorage ContainerName is not configured");

        _cdnUrl = configuration["MediaStorage:BlobStorage:CdnUrl"];

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
            baseFileName = SanitizeFileName(baseFileName);

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

            // Generate public URL
            var publicUrl = _cdnUrl != null
                ? $"{_cdnUrl.TrimEnd('/')}/{storagePath}"
                : blobClient.Uri.ToString();

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
    /// Find an available filename by checking for collisions and adding a number suffix
    /// </summary>
    private async Task<string> FindAvailableFileNameAsync(
        BlobContainerClient containerClient,
        string categoryFolder,
        string datePath,
        string baseFileName,
        string extension,
        CancellationToken cancellationToken)
    {
        // Try the original filename first
        var storagePath = $"{categoryFolder}/{datePath}/{baseFileName}{extension}";
        var blobClient = containerClient.GetBlobClient(storagePath);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return storagePath;
        }

        // File exists, try with incrementing numbers
        var counter = 1;
        while (counter < 1000) // Prevent infinite loop
        {
            storagePath = $"{categoryFolder}/{datePath}/{baseFileName}{counter}{extension}";
            blobClient = containerClient.GetBlobClient(storagePath);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return storagePath;
            }

            counter++;
        }

        // If we still can't find a unique name after 1000 attempts, fall back to GUID
        storagePath = $"{categoryFolder}/{datePath}/{baseFileName}_{Guid.NewGuid():N}{extension}";
        return storagePath;
    }

    /// <summary>
    /// Sanitize filename to remove invalid characters
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

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

            // If no expiration is specified or CDN is configured, return the direct/CDN URL
            if (expiration == null)
            {
                return _cdnUrl != null
                    ? $"{_cdnUrl.TrimEnd('/')}/{storagePath}"
                    : blobClient.Uri.ToString();
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
