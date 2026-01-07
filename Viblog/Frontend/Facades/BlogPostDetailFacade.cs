using Viblog.Infrastructure.Frontend.Facades;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Frontend.Facades;

/// <summary>
/// Facade implementation for blog post detail operations
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
    public virtual async Task<BlogPost?> GetPostByIdAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        return await _blogPostRepository.GetByIdAsync(id, partitionKey, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> GetPostByIdWithoutPartitionKeyAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return await _blogPostRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task CreatePostAsync(
        BlogPost post,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        await _blogPostRepository.AddAsync(post, cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task UpdatePostAsync(
        BlogPost post,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        await _blogPostRepository.UpdateAsync(post, cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);
    }
}
