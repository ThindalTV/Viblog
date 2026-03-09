using EricJohansson.se.Infrastructure.Facades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Repositories;

namespace EricJohansson.se.Facades;

/// <summary>
/// Facade implementation for blog post detail operations (public-facing)
/// </summary>
public class BlogPostDetailFacade : IBlogPostDetailFacade
{
    private readonly IBlogPostRepository _blogPostRepository;

    public BlogPostDetailFacade(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository ?? throw new ArgumentNullException(nameof(blogPostRepository));
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> GetPostBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        // Return null for invalid slug
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return await _blogPostRepository.GetBySlugAsync(
            slug,
            publishedOnly: true,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task IncrementViewCountAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        await _blogPostRepository.IncrementViewCountAsync(id, partitionKey, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<BlogPost>> GetRelatedPostsAsync(
        string slug,
        int maxPosts = 5,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var post = await _blogPostRepository.GetBySlugAsync(slug, publishedOnly: true, cancellationToken);

        if (post == null)
        {
            return Enumerable.Empty<BlogPost>();
        }

        return await _blogPostRepository.GetRelatedPostsAsync(post, maxPosts, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<(BlogPost? previous, BlogPost? next)> GetAdjacentPostsAsync(
        DateTimeOffset publishedAt,
        CancellationToken cancellationToken = default)
    {
        return await _blogPostRepository.GetAdjacentPostsAsync(publishedAt, cancellationToken);
    }
}
