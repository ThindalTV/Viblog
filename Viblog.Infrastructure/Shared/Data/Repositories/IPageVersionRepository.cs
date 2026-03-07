using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Infrastructure.Shared.Data.Repositories;

/// <summary>
/// Repository for Page version history.
/// </summary>
public interface IPageVersionRepository : IRepository<PageVersion>
{
    /// <summary>
    /// Get all versions for a specific Page.
    /// </summary>
    Task<IEnumerable<PageVersion>> GetVersionsForContentAsync(string contentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest version number for a Page.
    /// </summary>
    Task<int> GetLatestVersionNumberAsync(string contentId, CancellationToken cancellationToken = default);
}
