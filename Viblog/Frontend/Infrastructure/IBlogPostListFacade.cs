using Vilog.Shared.Data.Common;
using Vilog.Shared.Data.Entities;

namespace Vilog.Frontend.Infrastructure;

/// <summary>
/// Facade for blog post list operations
/// </summary>
public interface IBlogPostListFacade
{
    /// <summary>
    /// Get paginated published posts ordered by publish date (newest first)
    /// </summary>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of published blog posts</returns>
    Task<PagedResult<BlogPost>> GetPaginatedPostsAsync(PagingParameters pagingParameters, CancellationToken cancellationToken = default);
}
