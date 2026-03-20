using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Infrastructure.Data.Repositories;
using Viblog.Shared.Data.Sources.CosmosDb.Data.Entities;

namespace Viblog.Shared.Data.Sources.CosmosDb.Data.Repositories;

/// <summary>
/// CosmosDB-specific repository implementation for page operations
/// </summary>
public class CosmosDbPageRepository : CosmosDbRepository<Page>, IPageRepository
{
    private readonly ILogger<CosmosDbPageRepository> _logger;

    public CosmosDbPageRepository(ApplicationDbContext context, ILogger<CosmosDbPageRepository> logger) : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public override async Task AddAsync(Page entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.SetPartitionKey();
        await base.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task AddRangeAsync(IEnumerable<Page> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();
        foreach (var entity in entityList)
        {
            entity.SetPartitionKey();
        }

        await base.AddRangeAsync(entityList, cancellationToken);
    }

    /// <inheritdoc/>
    public override Task UpdateAsync(Page entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.SetPartitionKey();
        return base.UpdateAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<Page?> GetBySlugAsync(
        string slug,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var query = _dbSet
            .Where(p => !p.IsDeleted && p.Slug == slug);

        if (publishedOnly)
        {
            query = query.Where(p => p.IsPublished);
        }

        var page = await query.FirstOrDefaultAsync(cancellationToken);

        return page;
    }

    /// <inheritdoc/>
    public virtual async Task<Page?> GetByIdWithoutPartitionKeyAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var page = await _dbSet
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return page;
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<Page>> GetPagesAsync(
        PagingParameters pagingParameters,
        bool publishedOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(p => !p.IsDeleted);

        if (publishedOnly)
        {
            query = query.Where(p => p.IsPublished);
        }

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            p => p.Slug,
            ascending: true,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task IncrementViewCountAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        try
        {
            _logger.LogDebug("Incrementing view count for Page {PageId} in partition {PartitionKey}", id, partitionKey);
            
            var page = await _dbSet
                .WithPartitionKey(partitionKey)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            if (page != null)
            {
                page.ViewCount++;
                page.UpdatedAt = DateTimeOffset.UtcNow;
                _dbSet.Update(page);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Successfully incremented view count for Page {PageId}. New count: {ViewCount}", id, page.ViewCount);
            }
            else
            {
                _logger.LogWarning("Page {PageId} not found in partition {PartitionKey} for view count increment. " +
                                  "Attempting cross-partition fallback.", id, partitionKey);
                
                // Try fallback cross-partition query
                var pageFallback = await _dbSet
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
                
                if (pageFallback != null)
                {
                    pageFallback.ViewCount++;
                    pageFallback.UpdatedAt = DateTimeOffset.UtcNow;
                    _dbSet.Update(pageFallback);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Cross-partition fallback succeeded for Page {PageId}. Found in partition {ActualPartitionKey}. New count: {ViewCount}",
                        id, pageFallback.GroupKey, pageFallback.ViewCount);
                }
                else
                {
                    var warnMsg = $"Page {id} not found in any partition. View count increment skipped. " +
                                 $"Attempted partition: {partitionKey}";
                    _logger.LogWarning(warnMsg);
                }
            }
        }
        catch (Exception ex)
        {
            var diagnosticMsg = $"Error incrementing view count for Page {id} in partition {partitionKey}. " +
                               $"Exception: {ex.GetType().Name}: {ex.Message}";
            _logger.LogError(ex, diagnosticMsg);
            
            // Log but don't throw - view count increment is non-critical
            // This prevents a non-critical operation from breaking the request
            _logger.LogWarning("View count increment failed but continuing operation. " +
                              "Page {PageId} may have incorrect view count. Details: {DiagnosticMessage}",
                id, diagnosticMsg);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<Page>> GetScheduledPagesReadyToPublishAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => !p.IsDeleted &&
                        p.Schedule.Status == ContentStatus.Scheduled &&
                        p.Schedule.ScheduledPublishDate.HasValue &&
                        p.Schedule.ScheduledPublishDate.Value <= DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
    }
}
