namespace Viblog.Infrastructure.Shared.Data.Entities;

/// <summary>
/// Audit log entry tracking user actions across the system
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// User ID who performed the action
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User name (denormalized for performance)
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User email (denormalized for performance)
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Type of action performed
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Type of entity affected
    /// </summary>
    public EntityType EntityType { get; set; }

    /// <summary>
    /// ID of the entity affected (post ID, page ID, user ID, etc.)
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Name/title of the entity (denormalized for display)
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Description of the action
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// IP address of the user (if available)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string (if available)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Result of the action (success, failed, etc.)
    /// </summary>
    public ActionResult Result { get; set; } = ActionResult.Success;

    /// <summary>
    /// Error message if action failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Types of actions that can be audited
/// </summary>
public enum AuditAction
{
    // Authentication (0–9)
    Login = 0,
    Logout = 1,
    LoginFailed = 2,
    PasswordChanged = 3,
    PasswordReset = 4,

    // User Management (10–19)
    UserCreated = 10,
    UserUpdated = 11,
    UserDeleted = 12,
    UserDeactivated = 13,
    UserActivated = 14,
    UserClaimsModified = 15,

    // Content — BlogPost, Page (EntityType distinguishes which) (20–29)
    ContentCreated = 20,
    ContentUpdated = 21,
    ContentDeleted = 22,
    ContentPublished = 23,
    ContentUnpublished = 24,
    ContentScheduled = 25,
    ContentScheduleUpdated = 26,
    ContentScheduleCancelled = 27,
    ContentOwnershipTransferred = 28,

    // Media (30–39)
    MediaUploaded = 30,
    MediaDeleted = 31,
    MediaRenamed = 32,

    // Categories/Tags (40–49)
    CategoryCreated = 40,
    CategoryUpdated = 41,
    CategoryDeleted = 42,
    TagCreated = 43,
    TagUpdated = 44,
    TagDeleted = 45,

    // System (50–59)
    SystemConfigurationChanged = 50,
    BackupCreated = 51,
    DataImported = 52,
    DataExported = 53
}

/// <summary>
/// Types of entities that can be audited
/// </summary>
public enum EntityType
{
    User,
    BlogPost,
    Page,
    Media,
    Category,
    Tag,
    System,
    Authentication
}

/// <summary>
/// Result of an audited action
/// </summary>
public enum ActionResult
{
    Success,
    Failed,
    PartialSuccess,
    Unauthorized,
    ValidationError
}
