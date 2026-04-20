using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Repositories;

namespace Viblog.Shared.Data.Sources.CosmosDb.Data.Repositories;

/// <summary>
/// CosmosDB repository for BlogPost version history.
/// All versions stored in the same container with GroupKey "BlogPostVersion".
/// </summary>
public class BlogPostVersionRepository : CosmosDbRepository<BlogPostVersion>, IBlogPostVersionRepository
{
    private readonly ILogger<BlogPostVersionRepository> _logger;

    public BlogPostVersionRepository(ApplicationDbContext context, ILogger<BlogPostVersionRepository> logger) : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public override async Task AddAsync(BlogPostVersion entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogDebug("Adding BlogPostVersion {VersionId} for content {ContentId}, version {VersionNumber}",
            entity.Id, entity.ContentId, entity.VersionNumber);

        try
        {
            await base.AddAsync(entity, cancellationToken);
            _logger.LogDebug("Successfully staged BlogPostVersion {VersionId} for content {ContentId}", entity.Id, entity.ContentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add BlogPostVersion {VersionId} for content {ContentId}, version {VersionNumber}. Exception: {ExceptionType}: {ExceptionMessage}",
                entity.Id, entity.ContentId, entity.VersionNumber, ex.GetType().Name, ex.Message);
            throw new InvalidOperationException($"Failed to stage BlogPostVersion for content '{entity.ContentId}', version {entity.VersionNumber}. {ex.GetType().Name}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BlogPostVersion>> GetVersionsForContentAsync(string contentId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .WithPartitionKey("BlogPostVersion")
                .Where(v => v.ContentId == contentId && !v.IsDeleted)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (IsCosmosNotFound(ex))
        {
            _logger.LogWarning(ex,
                "Version lookup returned NotFound for content {ContentId} in partition BlogPostVersion. Returning empty result.",
                contentId);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetLatestVersionNumberAsync(string contentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting latest version number for content {ContentId}", contentId);

        try
        {
            var latest = await _dbSet
                .WithPartitionKey("BlogPostVersion")
                .Where(v => v.ContentId == contentId && !v.IsDeleted)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var versionNumber = latest?.VersionNumber ?? 0;
            _logger.LogDebug("Latest version number for content {ContentId} is {VersionNumber}", contentId, versionNumber);
            return versionNumber;
        }
        catch (Exception ex) when (IsCosmosNotFound(ex))
        {
            _logger.LogWarning(ex,
                "Latest-version lookup returned NotFound for content {ContentId} in partition BlogPostVersion. Returning 0.",
                contentId);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest version number for content {ContentId}. Exception: {ExceptionType}: {ExceptionMessage}",
                contentId, ex.GetType().Name, ex.Message);
            throw new InvalidOperationException($"Failed to get latest version number for content '{contentId}'. {ex.GetType().Name}: {ex.Message}", ex);
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
