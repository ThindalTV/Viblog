using Vilog.Shared.Data.Entities;
using Vilog.Shared.Models.Sitemap;

namespace Vilog.Shared.Services;

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
