using Microsoft.EntityFrameworkCore;
using Viblog.Data.CosmosDb.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.CosmosDb.Data.Repositories;

/// <summary>
/// CosmosDB-specific repository implementation for page operations
/// </summary>
public class CosmosDbPageRepository : CosmosDbRepository<Page>, IPageRepository
{
    public CosmosDbPageRepository(ApplicationDbContext context) : base(context)
    {
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
            query = query.Where(p => p.Live != null);
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
            query = query.Where(p => p.Live != null);
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

        var page = await _dbSet
            .WithPartitionKey(partitionKey)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

        if (page != null)
        {
            page.ViewCount++;
            page.UpdatedAt = DateTimeOffset.UtcNow;
            _dbSet.Update(page);
            await _context.SaveChangesAsync(cancellationToken);
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
