using Microsoft.EntityFrameworkCore;
using Viblog.Data.CosmosDb.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
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
        entity.UpdateSearchIndex();
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
            entity.UpdateSearchIndex();
        }

        await base.AddRangeAsync(entityList, cancellationToken);
    }

    /// <inheritdoc/>
    public override Task UpdateAsync(Page entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.SetPartitionKey();
        entity.UpdateSearchIndex();
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

        // Promote scheduled draft if ready
        if (page != null && page.PromoteDraftIfScheduled())
        {
            _dbSet.Update(page);
            await _context.SaveChangesAsync(cancellationToken);
        }

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

        // Promote scheduled draft if ready
        if (page != null && page.PromoteDraftIfScheduled())
        {
            _dbSet.Update(page);
            await _context.SaveChangesAsync(cancellationToken);
        }

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
        var pages = await _dbSet
            .Where(p => !p.IsDeleted && 
                       p.PublishDate.HasValue && 
                       p.PublishDate.Value <= DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        return pages;
    }

    /// <inheritdoc/>
    public virtual async Task<int> PromoteScheduledPagesAsync(
        CancellationToken cancellationToken = default)
    {
        var pagesToPromote = await GetScheduledPagesReadyToPublishAsync(cancellationToken);
        var promotedCount = 0;

        foreach (var page in pagesToPromote)
        {
            if (page.PromoteDraftIfScheduled())
            {
                _dbSet.Update(page);
                promotedCount++;
            }
        }

        if (promotedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return promotedCount;
    }
}
