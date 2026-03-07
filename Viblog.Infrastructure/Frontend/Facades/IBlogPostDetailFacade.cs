using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Frontend.Facades;

/// <summary>
/// Facade for blog post detail operations (public-facing)
/// </summary>
public interface IBlogPostDetailFacade
{
    /// <summary>
    /// Get a published blog post by its slug
    /// </summary>
    /// <param name="slug">The URL-friendly slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The blog post or null if not found</returns>
    Task<BlogPost?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment the view count for a blog post
    /// </summary>
    /// <param name="id">The blog post ID</param>
    /// <param name="partitionKey">The partition key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task IncrementViewCountAsync(string id, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get related blog posts based on shared tags
    /// </summary>
    /// <param name="slug">The post slug to find related posts for</param>
    /// <param name="maxPosts">Maximum number of related posts to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of related blog posts</returns>
    Task<IEnumerable<BlogPost>> GetRelatedPostsAsync(string slug, int maxPosts = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the previous and next published posts with full content relative to the given post.
    /// Posts without live markdown content (shorts) are skipped.
    /// </summary>
    /// <param name="publishedAt">The publish date of the current post</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (previous post, next post), either may be null</returns>
    Task<(BlogPost? previous, BlogPost? next)> GetAdjacentPostsAsync(DateTimeOffset publishedAt, CancellationToken cancellationToken = default);
}
