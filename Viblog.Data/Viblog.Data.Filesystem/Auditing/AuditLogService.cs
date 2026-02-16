using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.Filesystem.Auditing;

/// <summary>
/// Service implementation for audit logging operations
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        ILogger<AuditLogService> logger)
    {
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public virtual async Task LogActionAsync(
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
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userEmail);

        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid().ToString(),
                GroupKey = "audit-logs",
                UserId = userId,
                UserName = userName,
                UserEmail = userEmail,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description ?? GetDefaultDescription(action, entityType, entityName),
                Metadata = metadata,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTimeOffset.UtcNow,
                Result = result,
                ErrorMessage = errorMessage,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            await _auditLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Audit log created: {Action} by {UserEmail} on {EntityType} {EntityId} - {Result}",
                action, userEmail, entityType, entityId ?? "N/A", result);
        }
        catch (Exception ex)
        {
            // Don't fail the operation if audit logging fails
            _logger.LogError(ex, 
                "Failed to create audit log for action {Action} by user {UserId}",
                action, userId);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AuditLog>> GetUserActivityAsync(
        string userId,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await _auditLogRepository.GetByUserIdAsync(userId, pagingParameters, cancellationToken);
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

        return await _auditLogRepository.GetByEntityAsync(entityType, entityId, pagingParameters, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<AuditLog>> GetRecentActivityAsync(
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        return await _auditLogRepository.GetRecentAsync(count, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<Dictionary<AuditAction, int>> GetUserStatisticsAsync(
        string userId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _auditLogRepository.GetUserStatisticsAsync(userId, startDate, endDate, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<int> CleanupOldLogsAsync(
        int olderThanDays = 90,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-olderThanDays);
        var deletedCount = await _auditLogRepository.DeleteOldLogsAsync(cutoffDate, cancellationToken);

        _logger.LogInformation("Cleaned up {Count} audit logs older than {Days} days", deletedCount, olderThanDays);

        return deletedCount;
    }

    /// <summary>
    /// Generate a default description for an action
    /// </summary>
    private static string GetDefaultDescription(AuditAction action, EntityType entityType, string? entityName)
    {
        var entity = string.IsNullOrWhiteSpace(entityName) ? entityType.ToString() : $"{entityType} '{entityName}'";

        return action switch
        {
            // Authentication
            AuditAction.Login => "User logged in",
            AuditAction.Logout => "User logged out",
            AuditAction.LoginFailed => "Failed login attempt",
            AuditAction.PasswordChanged => "Password changed",
            AuditAction.PasswordReset => "Password reset",

            // User Management
            AuditAction.UserCreated => $"Created user {entityName}",
            AuditAction.UserUpdated => $"Updated user {entityName}",
            AuditAction.UserDeleted => $"Deleted user {entityName}",
            AuditAction.UserActivated => $"Activated user {entityName}",
            AuditAction.UserDeactivated => $"Deactivated user {entityName}",
            AuditAction.UserClaimsModified => $"Modified claims for user {entityName}",

            // Blog Posts
            AuditAction.PostCreated => $"Created blog post {entityName}",
            AuditAction.PostUpdated => $"Updated blog post {entityName}",
            AuditAction.PostDeleted => $"Deleted blog post {entityName}",
            AuditAction.PostPublished => $"Published blog post {entityName}",
            AuditAction.PostUnpublished => $"Unpublished blog post {entityName}",
            AuditAction.PostScheduled => $"Scheduled blog post {entityName}",
            AuditAction.PostScheduleUpdated => $"Updated schedule for blog post {entityName}",
            AuditAction.PostScheduleCancelled => $"Cancelled schedule for blog post {entityName}",

            // Pages
            AuditAction.PageCreated => $"Created page {entityName}",
            AuditAction.PageUpdated => $"Updated page {entityName}",
            AuditAction.PageDeleted => $"Deleted page {entityName}",
            AuditAction.PagePublished => $"Published page {entityName}",
            AuditAction.PageUnpublished => $"Unpublished page {entityName}",

            // Media
            AuditAction.MediaUploaded => $"Uploaded media {entityName}",
            AuditAction.MediaDeleted => $"Deleted media {entityName}",
            AuditAction.MediaRenamed => $"Renamed media {entityName}",

            // Default
            _ => $"{action} on {entity}"
        };
    }
}
