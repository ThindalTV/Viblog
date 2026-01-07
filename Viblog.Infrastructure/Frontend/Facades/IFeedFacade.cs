using Viblog.Infrastructure.Shared.Models.Feeds;

namespace Viblog.Infrastructure.Frontend.Facades;

/// <summary>
/// Facade for RSS/Atom feed generation
/// </summary>
public interface IFeedFacade
{
    /// <summary>
    /// Generate RSS 2.0 feed data for recent blog posts
    /// </summary>
    /// <param name="maxPosts">Maximum number of posts to include (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>RSS feed data</returns>
    Task<RssFeed> GenerateRssFeedAsync(int maxPosts = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate Atom 1.0 feed data for recent blog posts
    /// </summary>
    /// <param name="maxPosts">Maximum number of posts to include (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Atom feed data</returns>
    Task<AtomFeed> GenerateAtomFeedAsync(int maxPosts = 20, CancellationToken cancellationToken = default);
}
