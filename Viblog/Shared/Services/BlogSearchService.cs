using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Infrastructure.Shared.Extensions;
using Viblog.Infrastructure.Shared.Services;
using Viblog.Shared.Extensions;

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

        if (publishedOnly)
        {
            return await _repository.FindAsync(
                p => p.Live != null && p.Live.SearchIndex.Contains(normalizedSearchTerm),
                pagingParameters,
                p => p.PublishedAt,
                ascending: false,
                includeDeleted: false,
                cancellationToken);
        }

        return await _repository.FindAsync(
            p => p.Draft.SearchIndex.Contains(normalizedSearchTerm) ||
                 (p.Live != null && p.Live.SearchIndex.Contains(normalizedSearchTerm)),
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

        if (publishedOnly)
        {
            return await _repository.FindAsync(
                p => p.Live != null && p.Live.SearchIndex.Contains(normalizedTitleTerm),
                pagingParameters,
                p => p.PublishedAt,
                ascending: false,
                includeDeleted: false,
                cancellationToken);
        }

        return await _repository.FindAsync(
            p => p.Draft.SearchIndex.Contains(normalizedTitleTerm) ||
                 (p.Live != null && p.Live.SearchIndex.Contains(normalizedTitleTerm)),
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

        if (publishedOnly)
        {
            return await _repository.FindAsync(
                p => p.Live != null && normalizedTerms.All(term => p.Live.SearchIndex.Contains(term)),
                pagingParameters,
                p => p.PublishedAt,
                ascending: false,
                includeDeleted: false,
                cancellationToken);
        }

        return await _repository.FindAsync(
            p => normalizedTerms.All(term =>
                p.Draft.SearchIndex.Contains(term) ||
                (p.Live != null && p.Live.SearchIndex.Contains(term))),
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
                 p.Live != null &&
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
