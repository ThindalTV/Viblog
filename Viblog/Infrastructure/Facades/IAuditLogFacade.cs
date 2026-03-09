using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Infrastructure.Facades;

/// <summary>
/// Facade interface for admin audit log operations
/// </summary>
public interface IAuditLogFacade
{
    /// <summary>
    /// Get audit logs for a specific user
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
    /// Get audit logs for a specific entity
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
    /// Get recent system activity
    /// </summary>
    /// <param name="count">Number of logs to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recent audit logs</returns>
    Task<IEnumerable<AuditLog>> GetRecentActivityAsync(
        int count = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get statistics for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Number of days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Action statistics</returns>
    Task<Dictionary<AuditAction, int>> GetUserStatisticsAsync(
        string userId,
        int days = 30,
        CancellationToken cancellationToken = default);
}
