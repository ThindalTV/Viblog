using System.Linq.Expressions;
using Vilog.Admin.Infrastructure;
using Vilog.Shared.Data.Common;
using Vilog.Shared.Data.Entities;
using Vilog.Shared.Data.Repositories;

namespace Vilog.Admin.Facades;

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
}
