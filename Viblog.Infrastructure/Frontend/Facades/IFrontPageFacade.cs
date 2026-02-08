using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Frontend.Facades;

/// <summary>
/// Facade for front page operations
/// </summary>
public interface IFrontPageFacade
{
    /// <summary>
    /// Get posts for the front page including featured posts from the last month and latest posts
    /// </summary>
    /// <param name="maxPosts">Maximum number of posts to return (default: 8)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of blog posts for display on the front page</returns>
    Task<IEnumerable<BlogPost>> GetFrontPagePostsAsync(int maxPosts = 8, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get featured posts from the last month
    /// </summary>
    /// <param name="maxPosts">Maximum number of featured posts to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of featured blog posts</returns>
    Task<IEnumerable<BlogPost>> GetRecentFeaturedPostsAsync(int maxPosts = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get latest published posts
    /// </summary>
    /// <param name="maxPosts">Maximum number of posts to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of latest blog posts</returns>
    Task<IEnumerable<BlogPost>> GetLatestPostsAsync(int maxPosts = 8, CancellationToken cancellationToken = default);
}
