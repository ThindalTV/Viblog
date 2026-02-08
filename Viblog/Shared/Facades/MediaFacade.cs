using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Infrastructure.Shared.Services;
using Viblog.Infrastructure.Shared.Facades;

namespace Viblog.Shared.Facades;

/// <summary>
/// Facade implementation for media library operations
/// </summary>
public class MediaFacade : IMediaFacade
{
    private readonly IMediaService _mediaService;
    private readonly IMediaMetadataRepository _metadataRepository;
    private readonly ILogger<MediaFacade> _logger;

    public MediaFacade(
        IMediaService mediaService,
        IMediaMetadataRepository metadataRepository,
        ILogger<MediaFacade> logger)
    {
        _mediaService = mediaService;
        _metadataRepository = metadataRepository;
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
        string? mimeTypeFilter,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
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
                        item.GroupKey,
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
    public async Task<MediaItem?> GetByIdAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        return await _metadataRepository.GetByIdAsync(id, partitionKey, cancellationToken);
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
        return await _mediaService.UpdateMetadataAsync(id, partitionKey, title, description, altText, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> GetPublicUrlAsync(
        MediaItem mediaItem,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        return await _mediaService.GetPublicUrlAsync(mediaItem, expiration, cancellationToken);
    }
}
