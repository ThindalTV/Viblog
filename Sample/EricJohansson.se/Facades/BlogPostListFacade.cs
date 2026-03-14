using EricJohansson.se.Infrastructure.Facades;
using System;
using System.Threading;
using System.Threading.Tasks;
using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Repositories;

namespace EricJohansson.se.Facades;

/// <summary>
/// Facade implementation for blog post list operations
/// </summary>
public class BlogPostListFacade : IBlogPostListFacade
{
    private readonly IBlogPostRepository _blogPostRepository;

    public BlogPostListFacade(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository ?? throw new ArgumentNullException(nameof(blogPostRepository));
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPaginatedPostsAsync(
        PagingParameters pagingParameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await _blogPostRepository.GetPublishedPostsAsync(
            pagingParameters,
            cancellationToken);
    }
}
