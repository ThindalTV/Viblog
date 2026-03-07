using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.CosmosDb.Data.Repositories;

/// <summary>
/// CosmosDB-specific generic base repository implementation providing basic CRUD operations
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseEntity</typeparam>
public class CosmosDbRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public CosmosDbRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
        // EnsureCreatedAsync is called once at application startup (Program.cs).
        // Do NOT call it here: an unawaited async call on the context would start a
        // background operation that leaves the DbContext "busy", causing
        // "A second operation was started on this context instance" for any method
        // called before it completes.
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .WithPartitionKey(partitionKey)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<TEntity>> GetAllAsync<TKey>(
        PagingParameters pagingParameters,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        bool ascending = true,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet.AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(e => !e.IsDeleted);
        }

        return await ApplyPagingAndSortingAsync(query, pagingParameters, orderBy, ascending, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<TEntity>> FindAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        PagingParameters pagingParameters,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        bool ascending = true,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet.AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(e => !e.IsDeleted);
        }

        query = query.Where(predicate);

        return await ApplyPagingAndSortingAsync(query, pagingParameters, orderBy, ascending, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(e => !e.IsDeleted);
        }

        query = query.Where(predicate);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbSet.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();
        var now = DateTimeOffset.UtcNow;

        foreach (var entity in entityList)
        {
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
        }

        await _dbSet.AddRangeAsync(entityList, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        
        // Check if the entity is already being tracked (match by ID only, partition key can change)
        var tracked = _context.ChangeTracker.Entries<TEntity>()
            .FirstOrDefault(e => e.Entity.Id == entity.Id);

        if (tracked != null)
        {
            // Check if partition key has changed (Cosmos DB doesn't allow partition key updates)
            if (tracked.Entity.GroupKey != entity.GroupKey)
            {
                // For Cosmos DB: partition key change requires delete + add
                _dbSet.Remove(tracked.Entity);
                _dbSet.Add(entity);
            }
            else
            {
                // Update the tracked entity's properties (partition key unchanged)
                _context.Entry(tracked.Entity).CurrentValues.SetValues(entity);
            }
        }
        else
        {
            // Attach and mark as modified if not tracked
            _dbSet.Update(entity);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual async Task DeleteAsync(string id, string partitionKey, bool softDelete = true, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .WithPartitionKey(partitionKey)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity != null)
        {
            await DeleteAsync(entity, softDelete, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public virtual Task DeleteAsync(TEntity entity, bool softDelete = true, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (softDelete)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTimeOffset.UtcNow;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            _dbSet.Update(entity);
        }
        else
        {
            _dbSet.Remove(entity);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(e => !e.IsDeleted);
        }

        query = query.Where(predicate);

        // Use FirstOrDefaultAsync != null instead of AnyAsync due to CosmosDB query generation issues
        return await query.FirstOrDefaultAsync(cancellationToken) != null;
    }

    /// <inheritdoc/>
    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(e => !e.IsDeleted);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Apply paging and optional sorting to a query
    /// </summary>
    /// <param name="query">The base query</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="orderBy">Optional sort expression</param>
    /// <param name="ascending">Sort direction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with entities</returns>
    protected virtual async Task<PagedResult<TEntity>> ApplyPagingAndSortingAsync<TKey>(
        IQueryable<TEntity> query,
        PagingParameters pagingParameters,
        Expression<Func<TEntity, TKey>>? orderBy,
        bool ascending,
        CancellationToken cancellationToken = default)
    {
        // Get total count before paging
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting if specified
        if (orderBy != null)
        {
            query = ascending
                ? query.OrderBy(orderBy)
                : query.OrderByDescending(orderBy);
        }
        else
        {
            // Default ordering by CreatedAt if no orderBy specified
            query = query.OrderByDescending(e => e.CreatedAt);
        }

        // Apply paging
        var items = await query
            .Skip(pagingParameters.Skip)
            .Take(pagingParameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, totalCount, pagingParameters.PageNumber, pagingParameters.PageSize);
    }
}
