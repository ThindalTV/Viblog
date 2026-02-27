using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Shared.Data.Repositories;

/// <summary>
/// Repository interface for page operations
/// </summary>
public interface IPageRepository : IRepository<Page>
{
    /// <summary>
    /// Get page by slug
    /// </summary>
    /// <param name="slug">The URL-friendly slug</param>
    /// <param name="publishedOnly">Whether to return only published pages (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The page or null if not found</returns>
    Task<Page?> GetBySlugAsync(
        string slug,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get page by ID without requiring partition key (useful for admin scenarios)
    /// Note: This is less efficient than GetByIdAsync with partition key
    /// </summary>
    /// <param name="id">The page ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The page or null if not found</returns>
    Task<Page?> GetByIdWithoutPartitionKeyAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all pages with pagination
    /// </summary>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="publishedOnly">Whether to return only published pages (default: false for admin use)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of pages</returns>
    Task<PagedResult<Page>> GetPagesAsync(
        PagingParameters pagingParameters,
        bool publishedOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment the view count for a page
    /// </summary>
    /// <param name="id">The page ID</param>
    /// <param name="partitionKey">The partition key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task IncrementViewCountAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pages with scheduled publish dates that are due to be published.
    /// Called by <see cref="ContentPublishingBackgroundService"/> to find content ready to promote.
    /// </summary>
    Task<IEnumerable<Page>> GetScheduledPagesReadyToPublishAsync(
        CancellationToken cancellationToken = default);
}
