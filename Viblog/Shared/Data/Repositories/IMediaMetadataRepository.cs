using System.Linq.Expressions;
using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;

namespace Viblog.Shared.Data.Repositories;

/// <summary>
/// Repository interface for media metadata operations
/// </summary>
public interface IMediaMetadataRepository : IRepository<MediaItem>
{
    /// <summary>
    /// Get media items in a specific folder with paging and filtering
    /// </summary>
    /// <param name="folderPath">The folder path to query</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="mimeTypeFilter">Optional MIME type filter (e.g., "image/*")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with media items</returns>
    Task<PagedResult<MediaItem>> GetItemsInFolderAsync(
        string folderPath,
        PagingParameters pagingParameters,
        string? mimeTypeFilter = null,
        CancellationToken cancellationToken = default);

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
    /// Get all unique folder paths from the media library
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unique folder paths</returns>
    Task<List<string>> GetAllFolderPathsAsync(
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
