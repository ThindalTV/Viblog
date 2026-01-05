using Vilog.Shared.Data.Entities;

namespace Vilog.Frontend.Infrastructure;

/// <summary>
/// Facade for blog post detail operations
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
    /// Get a blog post by its ID (for editing, bypasses publish check)
    /// </summary>
    /// <param name="id">The blog post ID</param>
    /// <param name="partitionKey">The partition key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The blog post or null if not found</returns>
    Task<BlogPost?> GetPostByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a blog post by its ID without partition key (for admin scenarios)
    /// Note: Less efficient than GetPostByIdAsync with partition key
    /// </summary>
    /// <param name="id">The blog post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The blog post or null if not found</returns>
    Task<BlogPost?> GetPostByIdWithoutPartitionKeyAsync(string id, CancellationToken cancellationToken = default);

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
}
