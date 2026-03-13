using System.Threading;
using System.Threading.Tasks;
using Viblog.Infrastructure.Data.Entities;

namespace EricJohansson.se.Infrastructure.Facades;

/// <summary>
/// Facade for page detail operations (public-facing)
/// </summary>
public interface IPageDetailFacade
{
    /// <summary>
    /// Get a published page by its slug
    /// </summary>
    /// <param name="slug">The URL-friendly slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The page or null if not found</returns>
    Task<Page?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment the view count for a page
    /// </summary>
    /// <param name="id">The page ID</param>
    /// <param name="partitionKey">The partition key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task IncrementViewCountAsync(string id, string partitionKey, CancellationToken cancellationToken = default);
}
