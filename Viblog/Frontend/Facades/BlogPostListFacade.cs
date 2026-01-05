using Vilog.Frontend.Infrastructure;
using Vilog.Shared.Data.Common;
using Vilog.Shared.Data.Entities;
using Vilog.Shared.Data.Repositories;

namespace Vilog.Frontend.Facades;

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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await _blogPostRepository.GetPublishedPostsAsync(
            pagingParameters,
            cancellationToken);
    }
}
