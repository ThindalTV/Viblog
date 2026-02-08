using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Infrastructure.Shared.Services;
using Viblog.Shared.Helpers;

namespace Viblog.Shared.Services;

/// <summary>
/// Service implementation for media management operations
/// </summary>
public class MediaService : IMediaService
{
    private readonly IMediaStorageRepository _storageRepository;
    private readonly IMediaMetadataRepository _metadataRepository;
    private readonly IMetadataExtractorService _metadataExtractor;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IMediaStorageRepository storageRepository,
        IMediaMetadataRepository metadataRepository,
        IMetadataExtractorService metadataExtractor,
        ILogger<MediaService> logger)
    {
        _storageRepository = storageRepository;
        _metadataRepository = metadataRepository;
        _metadataExtractor = metadataExtractor;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MediaItem> UploadAsync(
        string fileName,
        Stream fileStream,
        string mimeType,
        string folderPath,
        string? uploadedBy = null,
        CancellationToken cancellationToken = default)
    {
        return await UploadAsync(
            fileName,
            fileStream,
            mimeType,
            folderPath,
            title: null,
            description: null,
            altText: null,
            uploadedBy,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MediaItem> UploadAsync(
        string fileName,
        Stream fileStream,
        string mimeType,
        string folderPath,
        string? title,
        string? description,
        string? altText,
        string? uploadedBy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentNullException.ThrowIfNull(mimeType);

        try
        {
            _logger.LogInformation("Starting upload for file: {FileName}", fileName);

            // Create a copy of the stream for metadata extraction
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            fileStream.Position = 0;

            // Upload to storage
            var storageResult = await _storageRepository.UploadAsync(
                fileName,
                fileStream,
                mimeType,
                folderPath,
                cancellationToken);

            _logger.LogInformation("File uploaded to storage: {StoragePath}", storageResult.StoragePath);

            // Extract metadata
            memoryStream.Position = 0;
            var extractedMetadata = await _metadataExtractor.ExtractMetadataAsync(
                memoryStream,
                mimeType,
                cancellationToken);

            // Create media item entity
            var mediaItem = new MediaItem
            {
                FileName = fileName,
                FileExtension = Path.GetExtension(fileName),
                FileSize = storageResult.FileSize,
                MimeType = mimeType,
                StoragePath = storageResult.StoragePath,
                PublicUrl = storageResult.PublicUrl,
                FolderPath = string.IsNullOrWhiteSpace(folderPath) ? "/" : folderPath,
                Title = title,
                Description = description,
                AltText = altText,
                UploadedBy = uploadedBy,
                Status = MediaStatus.Available,
                AdditionalMetadata = extractedMetadata
            };

            // Set dimensions from extracted metadata
            if (extractedMetadata.TryGetValue("Width", out var widthStr) && int.TryParse(widthStr, out var width))
            {
                mediaItem.Width = width;
            }

            if (extractedMetadata.TryGetValue("Height", out var heightStr) && int.TryParse(heightStr, out var height))
            {
                mediaItem.Height = height;
            }

            // Set preview URL based on MIME type
            var iconPath = MediaIconHelper.GetFileTypeIcon(mimeType);
            mediaItem.PreviewUrl = iconPath ?? storageResult.PublicUrl; // Use actual image URL if icon is null

            // Update partition key
            mediaItem.UpdatePartitionKey();

            // Save to database
            await _metadataRepository.AddAsync(mediaItem, cancellationToken);
            await _metadataRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Media item created: {Id}", mediaItem.Id);

            await memoryStream.DisposeAsync();

            return mediaItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload media file: {FileName}", fileName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<MediaItem?> GetByIdAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        return await _metadataRepository.GetByIdAsync(id, partitionKey, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaItem = await _metadataRepository.GetByIdAsync(id, partitionKey, cancellationToken);

            if (mediaItem == null)
            {
                _logger.LogWarning("Media item not found for deletion: {Id}", id);
                return false;
            }

            // Soft delete in database
            mediaItem.Status = MediaStatus.Deleted;
            mediaItem.IsDeleted = true;
            mediaItem.DeletedAt = DateTimeOffset.UtcNow;

            await _metadataRepository.UpdateAsync(mediaItem, cancellationToken);
            await _metadataRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Media item soft deleted: {Id}", id);

            // Note: We keep the file in storage for now
            // Physical deletion can be handled by a separate cleanup job

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media item: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetPublicUrlAsync(
        MediaItem mediaItem,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaItem);

        return await _storageRepository.GetPublicUrlAsync(
            mediaItem.StoragePath,
            expiration,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetDateFoldersAsync(
        MediaTypeCategory? mediaTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _metadataRepository.GetDateFoldersAsync(mediaTypeFilter, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get date folders");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MediaItem>> GetItemsByDateFolderAsync(
        string dateFolder,
        MediaTypeCategory? mediaTypeFilter,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dateFolder);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        try
        {
            return await _metadataRepository.GetItemsByDateFolderAsync(
                dateFolder,
                mediaTypeFilter,
                pagingParameters,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media items for date folder: {DateFolder}", dateFolder);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MediaItem>> SearchAsync(
        string searchTerm,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        try
        {
            return await _metadataRepository.SearchAsync(searchTerm, pagingParameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search media items: {SearchTerm}", searchTerm);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<MediaItem?> UpdateMetadataAsync(
        string id,
        string partitionKey,
        string? title,
        string? description,
        string? altText,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaItem = await _metadataRepository.GetByIdAsync(id, partitionKey, cancellationToken);

            if (mediaItem == null)
            {
                _logger.LogWarning("Media item not found for metadata update: {Id}", id);
                return null;
            }

            // Update metadata fields
            mediaItem.Title = title;
            mediaItem.Description = description;
            mediaItem.AltText = altText;

            await _metadataRepository.UpdateAsync(mediaItem, cancellationToken);
            await _metadataRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Media item metadata updated: {Id}", id);

            return mediaItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update media item metadata: {Id}", id);
            throw;
        }
    }
}
