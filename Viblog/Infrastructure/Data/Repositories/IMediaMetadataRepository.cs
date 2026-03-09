using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for media metadata operations
/// </summary>
public interface IMediaMetadataRepository : IRepository<MediaItem>
{
    /// <summary>
    /// Get media items by type (MIME type pattern)
    /// </summary>
    /// <param name="mimeTypePattern">MIME type pattern (e.g., "image/*", "application/pdf")</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with media items</returns>
    Task<PagedResult<MediaItem>> GetItemsByTypeAsync(
        string mimeTypePattern,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search media items across filename, title, and description
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with matching media items</returns>
    Task<PagedResult<MediaItem>> SearchAsync(
        string searchTerm,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a media item by its storage path
    /// </summary>
    /// <param name="storagePath">The storage path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The media item or null if not found</returns>
    Task<MediaItem?> GetByStoragePathAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get media items that are currently in use (UsageCount > 0)
    /// </summary>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with media items in use</returns>
    Task<PagedResult<MediaItem>> GetItemsInUseAsync(
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unused media items older than a specified age
    /// </summary>
    /// <param name="olderThan">Age threshold</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with unused media items</returns>
    Task<PagedResult<MediaItem>> GetUnusedItemsAsync(
        TimeSpan olderThan,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all unique date-based folders (yyyyMM format) that contain media items
    /// </summary>
    /// <param name="mediaTypeFilter">Optional media type filter (Image, Video, Audio, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of date folders in descending order (newest first)</returns>
    Task<List<string>> GetDateFoldersAsync(
        MediaTypeCategory? mediaTypeFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get media items from a specific date folder with optional type filtering
    /// </summary>
    /// <param name="dateFolder">Date folder in yyyyMM format</param>
    /// <param name="mediaTypeFilter">Optional media type filter</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with media items</returns>
    Task<PagedResult<MediaItem>> GetItemsByDateFolderAsync(
        string dateFolder,
        MediaTypeCategory? mediaTypeFilter,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the usage count for a media item atomically
    /// </summary>
    /// <param name="id">Media item ID</param>
    /// <param name="partitionKey">Partition key</param>
    /// <param name="increment">Amount to increment (positive) or decrement (negative)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateUsageCountAsync(
        string id,
        string partitionKey,
        int increment,
        CancellationToken cancellationToken = default);
}
