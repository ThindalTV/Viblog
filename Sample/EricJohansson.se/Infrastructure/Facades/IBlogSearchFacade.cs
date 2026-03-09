using System.Threading;
using System.Threading.Tasks;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace EricJohansson.se.Infrastructure.Facades;

/// <summary>
/// Facade for blog search operations
/// </summary>
public interface IBlogSearchFacade
{
    /// <summary>
    /// Search for published blog posts by term with pagination
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of published blog posts matching the search term</returns>
    Task<PagedResult<BlogPost>> SearchPostsAsync(
        string searchTerm, 
        PagingParameters pagingParameters, 
        CancellationToken cancellationToken = default);
}
