using Microsoft.EntityFrameworkCore;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Repositories;

namespace Viblog.Shared.Data.Sources.CosmosDb.Data.Repositories;

/// <summary>
/// CosmosDB repository for Page version history.
/// All versions stored in the same container with GroupKey "PageVersion".
/// </summary>
public class PageVersionRepository : CosmosDbRepository<PageVersion>, IPageVersionRepository
{
    public PageVersionRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PageVersion>> GetVersionsForContentAsync(string contentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.GroupKey == "PageVersion" && v.ContentId == contentId && !v.IsDeleted)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetLatestVersionNumberAsync(string contentId, CancellationToken cancellationToken = default)
    {
        var latest = await _dbSet
            .Where(v => v.GroupKey == "PageVersion" && v.ContentId == contentId && !v.IsDeleted)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return latest?.VersionNumber ?? 0;
    }
}
