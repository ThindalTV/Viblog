using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Shared.Data.Repositories.Storage;

/// <summary>
/// Azure Blob Storage implementation of media storage repository
/// </summary>
public class BlobStorageRepository : IMediaStorageRepository
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly string? _cdnUrl;
    private readonly ILogger<BlobStorageRepository> _logger;

    public BlobStorageRepository(
        IConfiguration configuration,
        ILogger<BlobStorageRepository> logger)
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

            // Generate storage path with date-based structure: yyyy/MM/filename
            var now = DateTimeOffset.UtcNow;
            var storagePath = $"{now:yyyy}/{now:MM}/{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
            
            // Apply folder path if not root
            if (!string.IsNullOrWhiteSpace(folderPath) && folderPath != "/")
            {
                storagePath = $"{folderPath.TrimStart('/')}/{storagePath}";
            }

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
    public async Task<MediaStorageResult> MoveAsync(
        string currentStoragePath,
        string newFolderPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var sourceBlobClient = containerClient.GetBlobClient(currentStoragePath);

            // Generate new storage path maintaining the filename
            var fileName = Path.GetFileName(currentStoragePath);
            var newStoragePath = string.IsNullOrWhiteSpace(newFolderPath) || newFolderPath == "/"
                ? fileName
                : $"{newFolderPath.TrimStart('/')}/{fileName}";

            var destinationBlobClient = containerClient.GetBlobClient(newStoragePath);

            // Copy to new location
            await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri, cancellationToken: cancellationToken);

            // Wait for copy to complete
            BlobProperties properties;
            do
            {
                await Task.Delay(100, cancellationToken);
                properties = await destinationBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            }
            while (properties.CopyStatus == CopyStatus.Pending);

            if (properties.CopyStatus != CopyStatus.Success)
            {
                throw new InvalidOperationException($"Failed to copy blob. Copy status: {properties.CopyStatus}");
            }

            // Delete original
            await sourceBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            // Get file size from properties
            var fileSize = properties.ContentLength;

            // Generate public URL
            var publicUrl = _cdnUrl != null
                ? $"{_cdnUrl.TrimEnd('/')}/{newStoragePath}"
                : destinationBlobClient.Uri.ToString();

            _logger.LogInformation("Moved file in blob storage from {CurrentPath} to {NewPath}", 
                currentStoragePath, newStoragePath);

            return new MediaStorageResult
            {
                StoragePath = newStoragePath,
                PublicUrl = publicUrl,
                FileSize = fileSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move file in blob storage: {CurrentPath} to {NewPath}", 
                currentStoragePath, newFolderPath);
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

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return null;
            }

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return properties.Value.ContentLength;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file size from blob storage: {StoragePath}", storagePath);
            throw;
        }
    }
}
