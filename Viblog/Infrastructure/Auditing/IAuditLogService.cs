using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Infrastructure.Auditing;

/// <summary>
/// Service interface for audit logging operations
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Log an action
    /// </summary>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="userName">User name</param>
    /// <param name="userEmail">User email</param>
    /// <param name="action">Action type</param>
    /// <param name="entityType">Entity type</param>
    /// <param name="entityId">Entity ID (optional)</param>
    /// <param name="entityName">Entity name (optional)</param>
    /// <param name="description">Action description</param>
    /// <param name="metadata">Additional metadata (optional)</param>
    /// <param name="ipAddress">IP address (optional)</param>
    /// <param name="userAgent">User agent (optional)</param>
    /// <param name="result">Action result</param>
    /// <param name="errorMessage">Error message if failed (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogActionAsync(
        string userId,
        string userName,
        string userEmail,
        AuditAction action,
        EntityType entityType,
        string? entityId = null,
        string? entityName = null,
        string? description = null,
        string? metadata = null,
        string? ipAddress = null,
        string? userAgent = null,
        ActionResult result = ActionResult.Success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged audit logs</returns>
    Task<PagedResult<AuditLog>> GetUserActivityAsync(
        string userId,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for an entity
    /// </summary>
    /// <param name="entityType">Entity type</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged audit logs</returns>
    Task<PagedResult<AuditLog>> GetEntityHistoryAsync(
        EntityType entityType,
        string entityId,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent audit logs
    /// </summary>
    /// <param name="count">Number of logs to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recent audit logs</returns>
    Task<IEnumerable<AuditLog>> GetRecentActivityAsync(
        int count = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user statistics
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Action statistics</returns>
    Task<Dictionary<AuditAction, int>> GetUserStatisticsAsync(
        string userId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old audit logs
    /// </summary>
    /// <param name="olderThanDays">Delete logs older than this many days</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of logs deleted</returns>
    Task<int> CleanupOldLogsAsync(
        int olderThanDays = 90,
        CancellationToken cancellationToken = default);
}
