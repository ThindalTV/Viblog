using Viblog.Infrastructure.Shared.Models.Sitemap;

namespace Viblog.Infrastructure.Shared.Services;

/// <summary>
/// Service for generating sitemap data
/// </summary>
public interface ISitemapService
{
    /// <summary>
    /// Generate sitemap data containing all public pages
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sitemap URL set data</returns>
    Task<SitemapUrlSet> GenerateSitemapAsync(CancellationToken cancellationToken = default);
}
