using Microsoft.EntityFrameworkCore;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.CosmosDb.Data.Repositories;

/// <summary>
/// CosmosDB-specific repository implementation for media metadata operations
/// </summary>
public class MediaMetadataRepository : Repository<MediaItem>, IMediaMetadataRepository
{
    public MediaMetadataRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MediaItem>> GetItemsInFolderAsync(
        string folderPath,
        PagingParameters pagingParameters,
        string? mimeTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(m => !m.IsDeleted && m.FolderPath == folderPath);

        // Apply MIME type filter if specified
        if (!string.IsNullOrWhiteSpace(mimeTypeFilter))
        {
            if (mimeTypeFilter.EndsWith("/*"))
            {
                // Pattern matching for type/* (e.g., image/*)
                var prefix = mimeTypeFilter[..^1]; // Remove the asterisk
                query = query.Where(m => m.MimeType.StartsWith(prefix));
            }
            else
            {
                // Exact match
                query = query.Where(m => m.MimeType == mimeTypeFilter);
            }
        }

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            m => m.CreatedAt,
            ascending: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MediaItem>> GetItemsByTypeAsync(
        string mimeTypePattern,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet.Where(m => !m.IsDeleted);

        // Apply MIME type pattern matching
        if (mimeTypePattern.EndsWith("/*"))
        {
            // Pattern matching for type/* (e.g., image/*)
            var prefix = mimeTypePattern[..^1]; // Remove the asterisk
            query = query.Where(m => m.MimeType.StartsWith(prefix));
        }
        else
        {
            // Exact match
            query = query.Where(m => m.MimeType == mimeTypePattern);
        }

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            m => m.CreatedAt,
            ascending: false,
            cancellationToken);
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

        var query = _dbSet.Where(m =>
            !m.IsDeleted &&
            (m.FileName.ToLower().Contains(searchLower) ||
             (m.Title != null && m.Title.ToLower().Contains(searchLower)) ||
             (m.Description != null && m.Description.ToLower().Contains(searchLower))));

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            m => m.CreatedAt,
            ascending: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MediaItem?> GetByStoragePathAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(m => m.StoragePath == storagePath && !m.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MediaItem>> GetItemsInUseAsync(
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet.Where(m => !m.IsDeleted && m.UsageCount > 0);

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            m => m.UsageCount,
            ascending: false,
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

        var query = _dbSet.Where(m =>
            !m.IsDeleted &&
            m.UsageCount == 0 &&
            m.CreatedAt < threshold);

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            m => m.CreatedAt,
            ascending: true,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetAllFolderPathsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => !m.IsDeleted)
            .Select(m => m.FolderPath)
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateUsageCountAsync(
        string id,
        string partitionKey,
        int increment,
        CancellationToken cancellationToken = default)
    {
        var mediaItem = await GetByIdAsync(id, partitionKey, cancellationToken);
        
        if (mediaItem == null)
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

        await UpdateAsync(mediaItem, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
