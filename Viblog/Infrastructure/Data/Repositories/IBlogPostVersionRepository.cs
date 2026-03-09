using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for BlogPost version history.
/// </summary>
public interface IBlogPostVersionRepository : IRepository<BlogPostVersion>
{
    /// <summary>
    /// Get all versions for a specific BlogPost.
    /// </summary>
    Task<IEnumerable<BlogPostVersion>> GetVersionsForContentAsync(string contentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest version number for a BlogPost.
    /// </summary>
    Task<int> GetLatestVersionNumberAsync(string contentId, CancellationToken cancellationToken = default);
}
