using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Data.Filesystem.Configuration;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.Filesystem.Data.Repositories;

/// <summary>
/// Filesystem-based repository implementation for audit log operations
/// </summary>
public class AuditLogRepository : FilesystemRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(
        IOptions<FilesystemStorageOptions> options,
        ILogger<FilesystemRepository<AuditLog>> logger)
        : base(options, logger)
    {
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AuditLog>> GetByUserIdAsync(
        string userId,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await FindAsync(
            log => log.UserId == userId,
            pagingParameters,
            log => log.Timestamp,
            ascending: false,
            includeDeleted: false,
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

        return await FindAsync(
            log => log.EntityType == entityType && log.EntityId == entityId,
            pagingParameters,
            log => log.Timestamp,
            ascending: false,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AuditLog>> GetByActionAsync(
        AuditAction action,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await FindAsync(
            log => log.Action == action,
            pagingParameters,
            log => log.Timestamp,
            ascending: false,
            includeDeleted: false,
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

        return await FindAsync(
            log => log.Timestamp >= startDate && log.Timestamp <= endDate,
            pagingParameters,
            log => log.Timestamp,
            ascending: false,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<AuditLog>> GetRecentAsync(
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = count };

        var result = await GetAllAsync(
            pagingParams,
            log => log.Timestamp,
            ascending: false,
            includeDeleted: false,
            cancellationToken);

        return result.Items;
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AuditLog>> GetFailedActionsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await FindAsync(
            log => log.Result != ActionResult.Success &&
                   log.Timestamp >= startDate &&
                   log.Timestamp <= endDate,
            pagingParameters,
            log => log.Timestamp,
            ascending: false,
            includeDeleted: false,
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

        // Get all logs for the user in the date range
        var allLogs = await GetAllItemsAsync(
            log => log.UserId == userId &&
                   log.Timestamp >= startDate &&
                   log.Timestamp <= endDate,
            includeDeleted: false,
            cancellationToken);

        // Group by action and count
        return allLogs
            .GroupBy(log => log.Action)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <inheritdoc/>
    public virtual async Task<int> DeleteOldLogsAsync(
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default)
    {
        // Get all old logs
        var oldLogs = await GetAllItemsAsync(
            log => log.Timestamp < olderThan,
            includeDeleted: false,
            cancellationToken);

        var count = 0;
        foreach (var log in oldLogs)
        {
            await DeleteAsync(log.Id, log.GroupKey, softDelete: false, cancellationToken);
            count++;
        }

        if (count > 0)
        {
            await SaveChangesAsync(cancellationToken);
        }

        return count;
    }

    /// <summary>
    /// Helper method to get all items matching a predicate
    /// </summary>
    private async Task<List<AuditLog>> GetAllItemsAsync(
        System.Linq.Expressions.Expression<Func<AuditLog, bool>> predicate,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var allItems = new List<AuditLog>();
        var pageNumber = 1;
        const int pageSize = 100;

        while (true)
        {
            var page = await FindAsync(
                predicate,
                new PagingParameters { PageNumber = pageNumber, PageSize = pageSize },
                log => log.Timestamp,
                ascending: false,
                includeDeleted,
                cancellationToken);

            if (!page.Items.Any())
            {
                break;
            }

            allItems.AddRange(page.Items);

            if (page.Items.Count() < pageSize)
            {
                break;
            }

            pageNumber++;
        }

        return allItems;
    }
}
