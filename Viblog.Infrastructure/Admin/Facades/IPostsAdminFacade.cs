using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Admin.Facades;

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

    /// <summary>
    /// Get a blog post by its ID for editing (bypasses publish check)
    /// </summary>
    /// <param name="id">The blog post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The blog post or null if not found</returns>
    Task<BlogPost?> GetPostByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new blog post
    /// </summary>
    /// <param name="post">The blog post to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CreatePostAsync(BlogPost post, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing blog post
    /// </summary>
    /// <param name="post">The blog post to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdatePostAsync(BlogPost post, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a blog post (soft delete)
    /// </summary>
    /// <param name="id">The blog post ID</param>
    /// <param name="partitionKey">The partition key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeletePostAsync(string id, string partitionKey, CancellationToken cancellationToken = default);
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
