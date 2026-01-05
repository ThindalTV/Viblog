using Vilog.Shared.Data.Common;
using Vilog.Shared.Data.Entities;

namespace Vilog.Admin.Infrastructure;

/// <summary>
/// Facade interface for admin post management operations
/// </summary>
public interface IPostsAdminFacade
{
    /// <summary>
    /// Get blog posts with pagination, sorting, and optional published status filtering
    /// </summary>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="publishedOnly">Filter for published posts only (null = all posts)</param>
    /// <param name="sortField">Field to sort by</param>
    /// <param name="ascending">Sort direction (true = ascending, false = descending)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of blog posts</returns>
    Task<PagedResult<BlogPost>> GetPostsAsync(
        PagingParameters pagingParameters,
        bool? publishedOnly = null,
        PostSortField sortField = PostSortField.CreatedAt,
        bool ascending = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Fields available for sorting posts
/// </summary>
public enum PostSortField
{
    Title,
    Slug,
    CreatedAt,
    PublishedAt,
    IsFeatured,
    IsPublished
}
