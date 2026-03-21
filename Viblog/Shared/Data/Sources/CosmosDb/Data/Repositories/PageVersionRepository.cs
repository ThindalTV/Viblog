using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using System.Net;
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
        try
        {
            return await _dbSet
                .WithPartitionKey("PageVersion")
                .Where(v => v.ContentId == contentId && !v.IsDeleted)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (IsCosmosNotFound(ex))
        {
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetLatestVersionNumberAsync(string contentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var latest = await _dbSet
                .WithPartitionKey("PageVersion")
                .Where(v => v.ContentId == contentId && !v.IsDeleted)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            return latest?.VersionNumber ?? 0;
        }
        catch (Exception ex) when (IsCosmosNotFound(ex))
        {
            return 0;
        }
    }

    private static bool IsCosmosNotFound(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is CosmosException cosmosException && cosmosException.StatusCode == HttpStatusCode.NotFound)
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
