using Viblog.Infrastructure.Admin.Facades;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Admin.Facades;

/// <summary>
/// Facade implementation for admin audit log operations
/// </summary>
public class AuditLogFacade : IAuditLogFacade
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogFacade(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AuditLog>> GetUserActivityAsync(
        string userId,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await _auditLogService.GetUserActivityAsync(userId, pagingParameters, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AuditLog>> GetEntityHistoryAsync(
        EntityType entityType,
        string entityId,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await _auditLogService.GetEntityHistoryAsync(entityType, entityId, pagingParameters, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<AuditLog>> GetRecentActivityAsync(
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        return await _auditLogService.GetRecentActivityAsync(count, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<Dictionary<AuditAction, int>> GetUserStatisticsAsync(
        string userId,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate.AddDays(-days);

        return await _auditLogService.GetUserStatisticsAsync(userId, startDate, endDate, cancellationToken);
    }
}
