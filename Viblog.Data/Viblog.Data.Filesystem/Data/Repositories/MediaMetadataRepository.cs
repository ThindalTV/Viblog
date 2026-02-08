using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Data.Filesystem.Configuration;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Infrastructure.Shared.Helpers;

namespace Viblog.Data.Filesystem.Data.Repositories;

/// <summary>
/// Filesystem-based repository implementation for media metadata operations
/// </summary>
public class MediaMetadataRepository : FilesystemRepository<MediaItem>, IMediaMetadataRepository
{
    public MediaMetadataRepository(
        IOptions<FilesystemStorageOptions> options,
        ILogger<FilesystemRepository<MediaItem>> logger)
        : base(options, logger)
    {
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MediaItem>> GetItemsByTypeAsync(
        string mimeTypePattern,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        if (mimeTypePattern.EndsWith("/*"))
        {
            var prefix = mimeTypePattern[..^1];
            return await FindAsync(
                m => m.MimeType.StartsWith(prefix),
                pagingParameters,
                m => m.CreatedAt,
                ascending: false,
                includeDeleted: false,
                cancellationToken);
        }
        else
        {
            return await FindAsync(
                m => m.MimeType == mimeTypePattern,
                pagingParameters,
                m => m.CreatedAt,
                ascending: false,
                includeDeleted: false,
                cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MediaItem>> SearchAsync(
        string searchTerm,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllAsync(
                pagingParameters,
                m => m.CreatedAt,
                ascending: false,
                includeDeleted: false,
                cancellationToken);
        }

        var searchLower = searchTerm.ToLowerInvariant();

        return await FindAsync(
            m => m.FileName.ToLower().Contains(searchLower) ||
                 (m.Title != null && m.Title.ToLower().Contains(searchLower)) ||
                 (m.Description != null && m.Description.ToLower().Contains(searchLower)),
            pagingParameters,
            m => m.CreatedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MediaItem?> GetByStoragePathAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        return await FirstOrDefaultAsync(
            m => m.StoragePath == storagePath,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MediaItem>> GetItemsInUseAsync(
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await FindAsync(
            m => m.UsageCount > 0,
            pagingParameters,
            m => m.UsageCount,
            ascending: false,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MediaItem>> GetUnusedItemsAsync(
        TimeSpan olderThan,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var threshold = DateTimeOffset.UtcNow - olderThan;

        return await FindAsync(
            m => m.UsageCount == 0 && m.CreatedAt < threshold,
            pagingParameters,
            m => m.CreatedAt,
            ascending: true,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetDateFoldersAsync(
        MediaTypeCategory? mediaTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        await _indexManager.LoadIndexAsync(cancellationToken);

        var allItems = await LoadAllEntitiesAsync(includeDeleted: false, cancellationToken);

        var query = allItems.AsEnumerable();

        // Apply media type filter if specified
        if (mediaTypeFilter.HasValue)
        {
            var categoryFolder = MediaTypeHelper.GetFolderName(mediaTypeFilter.Value);
            query = query.Where(m => m.StoragePath.StartsWith(categoryFolder + "/"));
        }

        return query
            .Select(m => MediaTypeHelper.ExtractDateFolder(m.StoragePath))
            .Where(dateFolder => dateFolder != null)
            .Distinct()
            .OrderByDescending(df => df)
            .ToList()!;
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

        // Parse date folder (yyyyMM format)
        if (dateFolder.Length != 6 || !int.TryParse(dateFolder, out _))
        {
            throw new ArgumentException("Date folder must be in yyyyMM format", nameof(dateFolder));
        }

        var year = dateFolder[..4];
        var month = dateFolder[4..];
        var datePattern = $"/{year}/{month}/";

        if (mediaTypeFilter.HasValue)
        {
            var categoryFolder = MediaTypeHelper.GetFolderName(mediaTypeFilter.Value);
            return await FindAsync(
                m => m.StoragePath.StartsWith(categoryFolder + "/") && m.StoragePath.Contains(datePattern),
                pagingParameters,
                m => m.CreatedAt,
                ascending: false,
                includeDeleted: false,
                cancellationToken);
        }
        else
        {
            return await FindAsync(
                m => m.StoragePath.Contains(datePattern),
                pagingParameters,
                m => m.CreatedAt,
                ascending: false,
                includeDeleted: false,
                cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateUsageCountAsync(
        string id,
        string partitionKey,
        int increment,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        var mediaItem = await GetByIdAsync(id, partitionKey, cancellationToken);

        if (mediaItem is null)
        {
            return false;
        }

        // Update usage count atomically
        mediaItem.UsageCount = Math.Max(0, mediaItem.UsageCount + increment);
        mediaItem.LastAccessedAt = DateTimeOffset.UtcNow;
        mediaItem.UpdatedAt = DateTimeOffset.UtcNow;

        // Update status based on usage count
        if (mediaItem.UsageCount > 0 && mediaItem.Status == MediaStatus.Available)
        {
            mediaItem.Status = MediaStatus.InUse;
        }
        else if (mediaItem.UsageCount == 0 && mediaItem.Status == MediaStatus.InUse)
        {
            mediaItem.Status = MediaStatus.Available;
        }

        await SaveEntityAsync(mediaItem, cancellationToken);

        return true;
    }
}
