using Microsoft.Extensions.Logging;
using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;
using Viblog.Shared.Data.Repositories;
using Viblog.Shared.Services;

namespace Viblog.Shared.Facades;

/// <summary>
/// Facade implementation for media library operations
/// </summary>
public class MediaFacade : IMediaFacade
{
    private readonly IMediaService _mediaService;
    private readonly IMediaMetadataRepository _metadataRepository;
    private readonly IMediaStorageRepository _storageRepository;
    private readonly ILogger<MediaFacade> _logger;

    public MediaFacade(
        IMediaService mediaService,
        IMediaMetadataRepository metadataRepository,
        IMediaStorageRepository storageRepository,
        ILogger<MediaFacade> logger)
    {
        _mediaService = mediaService;
        _metadataRepository = metadataRepository;
        _storageRepository = storageRepository;
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
        return await _mediaService.UploadAsync(
            fileName,
            fileStream,
            mimeType,
            folderPath,
            uploadedBy,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<MediaItem>> BulkUploadAsync(
        IEnumerable<(string FileName, Stream FileStream, string MimeType)> files,
        string folderPath,
        string? uploadedBy = null,
        CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();
        var uploadedItems = new List<MediaItem>();

        _logger.LogInformation("Starting bulk upload of {Count} files", fileList.Count);

        // Process uploads with concurrency limit (max 5 concurrent uploads)
        var semaphore = new SemaphoreSlim(5, 5);
        var uploadTasks = fileList.Select(async file =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var mediaItem = await _mediaService.UploadAsync(
                    file.FileName,
                    file.FileStream,
                    file.MimeType,
                    folderPath,
                    uploadedBy,
                    cancellationToken);

                lock (uploadedItems)
                {
                    uploadedItems.Add(mediaItem);
                }

                return mediaItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file during bulk upload: {FileName}", file.FileName);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(uploadTasks);

        _logger.LogInformation("Completed bulk upload of {Count} files", uploadedItems.Count);

        return uploadedItems;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MediaItem>> GetMediaItemsAsync(
        string? folderPath = null,
        string? mimeTypeFilter = null,
        PagingParameters? pagingParameters = null,
        CancellationToken cancellationToken = default)
    {
        pagingParameters ??= new PagingParameters { PageNumber = 1, PageSize = 50 };

        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            return await _metadataRepository.GetItemsInFolderAsync(
                folderPath,
                pagingParameters,
                mimeTypeFilter,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(mimeTypeFilter))
        {
            return await _metadataRepository.GetItemsByTypeAsync(
                mimeTypeFilter,
                pagingParameters,
                cancellationToken);
        }

        return await _metadataRepository.GetAllAsync(
            pagingParameters,
            m => m.CreatedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MediaItem?> MoveToFolderAsync(
        string id,
        string partitionKey,
        string newFolderPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaItem = await _metadataRepository.GetByIdAsync(id, partitionKey, cancellationToken);

            if (mediaItem == null)
            {
                _logger.LogWarning("Media item not found for move: {Id}", id);
                return null;
            }

            // Move in storage
            var storageResult = await _storageRepository.MoveAsync(
                mediaItem.StoragePath,
                newFolderPath,
                cancellationToken);

            // Update metadata
            mediaItem.StoragePath = storageResult.StoragePath;
            mediaItem.PublicUrl = storageResult.PublicUrl;
            mediaItem.FolderPath = string.IsNullOrWhiteSpace(newFolderPath) ? "/" : newFolderPath;

            await _metadataRepository.UpdateAsync(mediaItem, cancellationToken);
            await _metadataRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Moved media item {Id} to folder {FolderPath}", id, newFolderPath);

            return mediaItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move media item: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> BulkMoveAsync(
        IEnumerable<MediaItem> items,
        string newFolderPath,
        CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();
        var movedCount = 0;

        _logger.LogInformation("Starting bulk move of {Count} items to {FolderPath}", itemList.Count, newFolderPath);

        foreach (var item in itemList)
        {
            try
            {
                var result = await MoveToFolderAsync(
                    item.Id,
                    item.PartitionKey,
                    newFolderPath,
                    cancellationToken);

                if (result != null)
                {
                    movedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move item during bulk move: {Id}", item.Id);
                // Continue with other items
            }
        }

        _logger.LogInformation("Completed bulk move: {MovedCount}/{TotalCount} items moved", movedCount, itemList.Count);

        return movedCount;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        return await _mediaService.DeleteAsync(id, partitionKey, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> BulkDeleteAsync(
        IEnumerable<MediaItem> items,
        CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();
        var deletedCount = 0;

        _logger.LogInformation("Starting bulk delete of {Count} items", itemList.Count);

        // Process deletes in batches of 100
        var batches = itemList.Chunk(100);

        foreach (var batch in batches)
        {
            foreach (var item in batch)
            {
                try
                {
                    var success = await _mediaService.DeleteAsync(
                        item.Id,
                        item.PartitionKey,
                        cancellationToken);

                    if (success)
                    {
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete item during bulk delete: {Id}", item.Id);
                    // Continue with other items
                }
            }
        }

        _logger.LogInformation("Completed bulk delete: {DeletedCount}/{TotalCount} items deleted", deletedCount, itemList.Count);

        return deletedCount;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MediaItem>> SearchAsync(
        string searchTerm,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        return await _metadataRepository.SearchAsync(searchTerm, pagingParameters, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetAllFolderPathsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _metadataRepository.GetAllFolderPathsAsync(cancellationToken);
    }
}
