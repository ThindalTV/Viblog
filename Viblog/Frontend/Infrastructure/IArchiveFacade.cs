using Vilog.Shared.Data.Common;
using Vilog.Shared.Data.Entities;

namespace Vilog.Frontend.Infrastructure;

/// <summary>
/// Facade for blog archive operations
/// </summary>
public interface IArchiveFacade
{
    /// <summary>
    /// Get paginated published posts for a specific month
    /// </summary>
    /// <param name="year">The year</param>
    /// <param name="month">The month (1-12)</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of published blog posts from the specified month</returns>
    Task<PagedResult<BlogPost>> GetPostsByMonthAsync(
        int year, 
        int month, 
        PagingParameters pagingParameters, 
        CancellationToken cancellationToken = default);
}
