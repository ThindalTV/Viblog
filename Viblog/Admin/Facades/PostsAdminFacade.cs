using System.Linq.Expressions;
using Viblog.Infrastructure.Admin.Facades;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Admin.Facades;

/// <summary>
/// Facade implementation for admin post management operations
/// </summary>
public class PostsAdminFacade : IPostsAdminFacade
{
    private readonly IBlogPostRepository _blogPostRepository;

    public PostsAdminFacade(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository ?? throw new ArgumentNullException(nameof(blogPostRepository));
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPostsAsync(
        PagingParameters pagingParameters,
        bool? publishedOnly = null,
        PostSortField sortField = PostSortField.CreatedAt,
        bool ascending = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        // Build the predicate based on the filter
        Expression<Func<BlogPost, bool>> predicate = publishedOnly switch
        {
            true => p => p.IsPublished,
            false => p => !p.IsPublished,
            null => p => true // All posts
        };

        // Use the appropriate sort expression based on the sort field
        return sortField switch
        {
            PostSortField.Title => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.Title, ascending, false, cancellationToken),
            
            PostSortField.Slug => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.Slug, ascending, false, cancellationToken),
            
            PostSortField.PublishedAt => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.PublishedAt, ascending, false, cancellationToken),
            
            PostSortField.IsFeatured => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.IsFeatured, ascending, false, cancellationToken),
            
            PostSortField.IsPublished => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.IsPublished, ascending, false, cancellationToken),
            
            PostSortField.CreatedAt or _ => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.CreatedAt, ascending, false, cancellationToken)
        };
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> GetPostByIdAsync(
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

    /// <inheritdoc/>
    public virtual async Task DeletePostAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        await _blogPostRepository.DeleteAsync(id, partitionKey, softDelete: true, cancellationToken: cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);
    }
}
