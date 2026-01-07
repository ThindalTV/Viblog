using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Frontend.Facades;

/// <summary>
/// Facade for category-filtered blog post operations
/// </summary>
public interface ICategoryPostsFacade
{
    /// <summary>
    /// Get paginated published posts for a specific category
    /// </summary>
    /// <param name="categoryId">The category ID to filter by</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of published blog posts in the category</returns>
    Task<PagedResult<BlogPost>> GetPostsByCategoryAsync(
        string categoryId, 
        PagingParameters pagingParameters, 
        CancellationToken cancellationToken = default);
}
