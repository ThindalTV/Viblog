using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Admin.Facades;

/// <summary>
/// Facade interface for admin page management operations
/// </summary>
public interface IPagesAdminFacade
{
    /// <summary>
    /// Get pages with pagination, sorting, and optional published status filtering
    /// </summary>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="publishedOnly">Filter for published pages only (null = all pages)</param>
    /// <param name="sortField">Field to sort by</param>
    /// <param name="ascending">Sort direction (true = ascending, false = descending)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of pages</returns>
    Task<PagedResult<Page>> GetPagesAsync(
        PagingParameters pagingParameters,
        bool? publishedOnly = null,
        PageSortField sortField = PageSortField.Slug,
        bool ascending = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a page by ID for editing (bypasses published check)
    /// </summary>
    /// <param name="id">The page ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The page or null if not found</returns>
    Task<Page?> GetPageByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new page
    /// </summary>
    /// <param name="page">The page to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CreatePageAsync(Page page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing page
    /// </summary>
    /// <param name="page">The page to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdatePageAsync(Page page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a page (soft delete)
    /// </summary>
    /// <param name="id">The page ID</param>
    /// <param name="partitionKey">The partition key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeletePageAsync(string id, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a page immediately (promotes draft to live)
    /// </summary>
    /// <param name="id">The page ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishPageNowAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule a page for future publishing
    /// </summary>
    /// <param name="id">The page ID</param>
    /// <param name="publishDate">The date and time to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SchedulePagePublishingAsync(string id, DateTimeOffset publishDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpublish a page (keeps draft version)
    /// </summary>
    /// <param name="id">The page ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UnpublishPageAsync(string id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Fields available for sorting pages
/// </summary>
public enum PageSortField
{
    Slug,
    CreatedAt,
    UpdatedAt,
    IsPublished
}
