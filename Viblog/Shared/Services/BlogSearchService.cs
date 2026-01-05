using Microsoft.EntityFrameworkCore;
using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;
using Viblog.Shared.Data.Repositories;
using Viblog.Shared.Infrastructure;

namespace Viblog.Shared.Services;

/// <summary>
/// Default implementation of blog search service using the repository pattern
/// </summary>
public class BlogSearchService : IBlogSearchService
{
    private readonly IBlogPostRepository _repository;

    public BlogSearchService(IBlogPostRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> SearchAsync(
        string searchTerm,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var normalizedSearchTerm = searchTerm.ToLowerInvariant();

        return await _repository.FindAsync(
            p => p.SearchIndex.Contains(normalizedSearchTerm) &&
                 (!publishedOnly || (p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow)),
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> SearchByTitleAsync(
        string titleTerm,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titleTerm);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var normalizedTitleTerm = titleTerm.ToLowerInvariant();

        return await _repository.FindAsync(
            p => p.Title.ToLower().Contains(normalizedTitleTerm) &&
                 (!publishedOnly || (p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow)),
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> SearchMultipleTermsAsync(
        string[] searchTerms,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchTerms);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        if (searchTerms.Length == 0)
        {
            throw new ArgumentException("At least one search term is required", nameof(searchTerms));
        }

        var normalizedTerms = searchTerms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.ToLowerInvariant())
            .ToArray();

        if (normalizedTerms.Length == 0)
        {
            throw new ArgumentException("At least one non-empty search term is required", nameof(searchTerms));
        }

        return await _repository.FindAsync(
            p => normalizedTerms.All(term => p.SearchIndex.Contains(term)) &&
                 (!publishedOnly || (p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow)),
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<BlogPost>> GetRelatedPostsAsync(
        string postId,
        string partitionKey,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postId);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        if (maxResults <= 0)
        {
            throw new ArgumentException("Max results must be greater than zero", nameof(maxResults));
        }

        // Get the source post
        var sourcePost = await _repository.GetByIdAsync(postId, partitionKey, cancellationToken);
        if (sourcePost == null)
        {
            return Enumerable.Empty<BlogPost>();
        }

        // Find posts with matching tags or categories
        var relatedPosts = await _repository.FindAsync(
            p => p.Id != postId &&
                 p.IsPublished &&
                 p.PublishedAt <= DateTimeOffset.UtcNow &&
                 (p.Tags.Any(t => sourcePost.Tags.Contains(t)) ||
                  p.CategoryIds.Any(c => sourcePost.CategoryIds.Contains(c))),
            new PagingParameters(1, maxResults),
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);

        return relatedPosts.Items;
    }
}
