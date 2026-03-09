using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for audit log operations
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Get audit logs for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of audit logs</returns>
    Task<PagedResult<AuditLog>> GetByUserIdAsync(
        string userId,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    /// <param name="entityType">Type of entity</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of audit logs</returns>
    Task<PagedResult<AuditLog>> GetByEntityAsync(
        EntityType entityType,
        string entityId,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by action type
    /// </summary>
    /// <param name="action">Action type</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of audit logs</returns>
    Task<PagedResult<AuditLog>> GetByActionAsync(
        AuditAction action,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs within a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of audit logs</returns>
    Task<PagedResult<AuditLog>> GetByDateRangeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent audit logs
    /// </summary>
    /// <param name="count">Number of logs to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recent audit logs</returns>
    Task<IEnumerable<AuditLog>> GetRecentAsync(
        int count = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed actions within a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Failed audit logs</returns>
    Task<PagedResult<AuditLog>> GetFailedActionsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get statistics for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of action types and counts</returns>
    Task<Dictionary<AuditAction, int>> GetUserStatisticsAsync(
        string userId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old audit logs
    /// </summary>
    /// <param name="olderThan">Delete logs older than this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of logs deleted</returns>
    Task<int> DeleteOldLogsAsync(
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default);
}
