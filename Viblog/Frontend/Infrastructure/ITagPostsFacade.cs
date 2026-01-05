using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;

namespace Viblog.Frontend.Infrastructure;

/// <summary>
/// Facade for tag-filtered blog post operations
/// </summary>
public interface ITagPostsFacade
{
    /// <summary>
    /// Get paginated published posts for a specific tag
    /// </summary>
    /// <param name="tag">The tag to filter by</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of published blog posts with the tag</returns>
    Task<PagedResult<BlogPost>> GetPostsByTagAsync(
        string tag, 
        PagingParameters pagingParameters, 
        CancellationToken cancellationToken = default);
}
