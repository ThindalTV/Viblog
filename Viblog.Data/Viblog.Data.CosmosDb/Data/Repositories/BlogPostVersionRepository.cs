using Microsoft.EntityFrameworkCore;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.CosmosDb.Data.Repositories;

/// <summary>
/// CosmosDB repository for BlogPost version history.
/// All versions stored in the same container with GroupKey "BlogPostVersion".
/// </summary>
public class BlogPostVersionRepository : CosmosDbRepository<BlogPostVersion>, IBlogPostVersionRepository
{
    public BlogPostVersionRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BlogPostVersion>> GetVersionsForContentAsync(string contentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.GroupKey == "BlogPostVersion" && v.ContentId == contentId && !v.IsDeleted)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetLatestVersionNumberAsync(string contentId, CancellationToken cancellationToken = default)
    {
        var latest = await _dbSet
            .Where(v => v.GroupKey == "BlogPostVersion" && v.ContentId == contentId && !v.IsDeleted)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return latest?.VersionNumber ?? 0;
    }
}
