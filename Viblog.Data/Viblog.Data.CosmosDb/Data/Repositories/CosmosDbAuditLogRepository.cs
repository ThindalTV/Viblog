using Microsoft.EntityFrameworkCore;
using Viblog.Data.CosmosDb.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.CosmosDb.Data.Repositories;

/// <summary>
/// CosmosDB-specific repository implementation for audit log operations
/// </summary>
public class CosmosDbAuditLogRepository : CosmosDbRepository<AuditLog>, IAuditLogRepository
{
    public CosmosDbAuditLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public override async Task AddAsync(AuditLog entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.SetPartitionKey();
        await base.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task AddRangeAsync(IEnumerable<AuditLog> entities, CancellationToken cancellationToken = default)
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
    public override Task UpdateAsync(AuditLog entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.SetPartitionKey();
        return base.UpdateAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AuditLog>> GetByUserIdAsync(
        string userId,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(a => !a.IsDeleted && a.UserId == userId);

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            a => a.Timestamp,
            ascending: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AuditLog>> GetByEntityAsync(
        EntityType entityType,
        string entityId,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(a => !a.IsDeleted && a.EntityType == entityType && a.EntityId == entityId);

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            a => a.Timestamp,
            ascending: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AuditLog>> GetByActionAsync(
        AuditAction action,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(a => !a.IsDeleted && a.Action == action);

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            a => a.Timestamp,
            ascending: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AuditLog>> GetByDateRangeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(a => !a.IsDeleted && a.Timestamp >= startDate && a.Timestamp <= endDate);

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            a => a.Timestamp,
            ascending: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<AuditLog>> GetRecentAsync(
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AuditLog>> GetFailedActionsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(a => !a.IsDeleted 
                && a.Result == ActionResult.Failed 
                && a.Timestamp >= startDate 
                && a.Timestamp <= endDate);

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            a => a.Timestamp,
            ascending: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<Dictionary<AuditAction, int>> GetUserStatisticsAsync(
        string userId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var logs = await _dbSet
            .Where(a => !a.IsDeleted 
                && a.UserId == userId 
                && a.Timestamp >= startDate 
                && a.Timestamp <= endDate)
            .ToListAsync(cancellationToken);

        return logs
            .GroupBy(a => a.Action)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <inheritdoc/>
    public virtual async Task<int> DeleteOldLogsAsync(
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default)
    {
        var oldLogs = await _dbSet
            .Where(a => a.Timestamp < olderThan)
            .ToListAsync(cancellationToken);

        if (oldLogs.Count == 0)
        {
            return 0;
        }

        _dbSet.RemoveRange(oldLogs);
        await _context.SaveChangesAsync(cancellationToken);

        return oldLogs.Count;
    }
}
