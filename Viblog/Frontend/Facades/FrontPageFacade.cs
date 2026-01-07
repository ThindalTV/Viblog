using Viblog.Infrastructure.Frontend.Facades;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Frontend.Facades;

/// <summary>
/// Facade implementation for front page operations
/// </summary>
public class FrontPageFacade : IFrontPageFacade
{
    private readonly IBlogPostRepository _blogPostRepository;

    public FrontPageFacade(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository ?? throw new ArgumentNullException(nameof(blogPostRepository));
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<BlogPost>> GetFrontPagePostsAsync(
        int maxPosts = 8,
        CancellationToken cancellationToken = default)
    {
        if (maxPosts <= 0)
        {
            throw new ArgumentException("Maximum posts must be greater than zero", nameof(maxPosts));
        }

        var posts = new List<BlogPost>();
        var oneMonthAgo = DateTimeOffset.UtcNow.AddMonths(-1);

        // Get featured posts from the last month
        var featuredPosts = await _blogPostRepository.FindAsync(
            p => p.IsFeatured && 
                 p.IsPublished && 
                 p.PublishedAt <= DateTimeOffset.UtcNow &&
                 p.PublishedAt >= oneMonthAgo,
            new PagingParameters(1, maxPosts),
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);

        posts.AddRange(featuredPosts?.Items ?? Enumerable.Empty<BlogPost>());

        // Calculate how many more posts we need
        var remainingSlots = maxPosts - posts.Count;

        if (remainingSlots > 0)
        {
            // Get latest posts, excluding already added featured posts
            var featuredPostIds = posts.Select(p => p.Id).ToHashSet();

            var latestPosts = await _blogPostRepository.GetPublishedPostsAsync(
                new PagingParameters(1, remainingSlots + featuredPostIds.Count),
                cancellationToken);

            // Filter out featured posts we already added and take only what we need
            var additionalPosts = (latestPosts?.Items ?? Enumerable.Empty<BlogPost>())
                .Where(p => !featuredPostIds.Contains(p.Id))
                .Take(remainingSlots);

            posts.AddRange(additionalPosts);
        }

        return posts;
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<BlogPost>> GetRecentFeaturedPostsAsync(
        int maxPosts = 5,
        CancellationToken cancellationToken = default)
    {
        if (maxPosts <= 0)
        {
            throw new ArgumentException("Maximum posts must be greater than zero", nameof(maxPosts));
        }

        var oneMonthAgo = DateTimeOffset.UtcNow.AddMonths(-1);

        var featuredPosts = await _blogPostRepository.FindAsync(
            p => p.IsFeatured && 
                 p.IsPublished && 
                 p.PublishedAt <= DateTimeOffset.UtcNow &&
                 p.PublishedAt >= oneMonthAgo,
            new PagingParameters(1, maxPosts),
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);

        return featuredPosts?.Items ?? Enumerable.Empty<BlogPost>();
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<BlogPost>> GetLatestPostsAsync(
        int maxPosts = 8,
        CancellationToken cancellationToken = default)
    {
        if (maxPosts <= 0)
        {
            throw new ArgumentException("Maximum posts must be greater than zero", nameof(maxPosts));
        }

        var latestPosts = await _blogPostRepository.GetPublishedPostsAsync(
            new PagingParameters(1, maxPosts),
            cancellationToken);

        return latestPosts.Items ?? Enumerable.Empty<BlogPost>();
    }
}
