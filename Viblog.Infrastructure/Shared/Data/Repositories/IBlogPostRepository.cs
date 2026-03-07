using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Shared.Data.Repositories;

/// <summary>
/// Repository interface for blog post operations
/// </summary>
public interface IBlogPostRepository : IRepository<BlogPost>
{
    /// <summary>
    /// Get published blog posts with pagination and sorting
    /// </summary>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of published blog posts</returns>
    Task<PagedResult<BlogPost>> GetPublishedPostsAsync(
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get blog posts by category with pagination
    /// </summary>
    /// <param name="categoryId">The category ID to filter by</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="publishedOnly">Whether to return only published posts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of blog posts in the specified category</returns>
    Task<PagedResult<BlogPost>> GetPostsByCategoryAsync(
        string categoryId,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get blog posts by tag with pagination
    /// </summary>
    /// <param name="tag">The tag to filter by</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="publishedOnly">Whether to return only published posts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of blog posts with the specified tag</returns>
    Task<PagedResult<BlogPost>> GetPostsByTagAsync(
        string tag,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get featured blog posts with pagination
    /// </summary>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="publishedOnly">Whether to return only published posts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of featured blog posts</returns>
    Task<PagedResult<BlogPost>> GetFeaturedPostsAsync(
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get blog post by slug
    /// </summary>
    /// <param name="slug">The URL-friendly slug</param>
    /// <param name="publishedOnly">Whether to return only published posts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The blog post or null if not found</returns>
    Task<BlogPost?> GetBySlugAsync(
        string slug,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get blog post by ID without requiring partition key (useful for admin scenarios)
    /// Note: This is less efficient than GetByIdAsync with partition key
    /// </summary>
    /// <param name="id">The blog post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The blog post or null if not found</returns>
    Task<BlogPost?> GetByIdWithoutPartitionKeyAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get blog posts by author with pagination
    /// </summary>
    /// <param name="authorId">The author ID to filter by</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="publishedOnly">Whether to return only published posts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of blog posts by the specified author</returns>
    Task<PagedResult<BlogPost>> GetPostsByAuthorAsync(
        string authorId,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment the view count for a blog post
    /// </summary>
    /// <param name="id">The blog post ID</param>
    /// <param name="partitionKey">The partition key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task IncrementViewCountAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get blog posts for a specific month with pagination
    /// </summary>
    /// <param name="year">The year</param>
    /// <param name="month">The month (1-12)</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="publishedOnly">Whether to return only published posts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of blog posts from the specified month</returns>
    Task<PagedResult<BlogPost>> GetPostsByMonthAsync(
        int year,
        int month,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get related blog posts based on shared tags
    /// </summary>
    /// <param name="post">The reference post</param>
    /// <param name="maxPosts">Maximum number of related posts to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of related blog posts</returns>
    Task<IEnumerable<BlogPost>> GetRelatedPostsAsync(
        BlogPost post,
        int maxPosts = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the publication date of a blog post. 
    /// Since partition key is based on year, this may require deleting and recreating the post if the year changes.
    /// </summary>
    /// <param name="postId">The blog post ID</param>
    /// <param name="currentPartitionKey">The current partition key (year)</param>
    /// <param name="newPublishedAt">The new publication date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated blog post or null if not found</returns>
    Task<BlogPost?> UpdatePublicationDateAsync(
        string postId,
        string currentPartitionKey,
        DateTimeOffset newPublishedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get blog posts that are scheduled and ready to be published (scheduled date has passed).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Blog posts ready to publish</returns>
    Task<IEnumerable<BlogPost>> GetScheduledPostsReadyToPublishAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the nearest published posts with full content on either side of the given publish date.
    /// Posts without live markdown content (shorts) are skipped.
    /// </summary>
    /// <param name="publishedAt">The reference publish date of the current post</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (previous post, next post), either may be null</returns>
    Task<(BlogPost? previous, BlogPost? next)> GetAdjacentPostsAsync(
        DateTimeOffset publishedAt,
        CancellationToken cancellationToken = default);
}
