namespace Viblog.Infrastructure.Shared.Data.Entities;

/// <summary>
/// Admin user entity for blog administration
/// All users are admin users - there is no public user registration
/// Authentication is handled by external identity provider, authorization (permissions) stored locally
/// </summary>
public class AdminUser : BaseEntity
{
    public AdminUser()
    {
        GroupKey = "users"; // Set default partition key for AdminUser
    }

    /// <summary>
    /// User's email address (unique, immutable after creation)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's display name (for blog author attribution)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Additional claims assigned to this user for authorization
    /// </summary>
    public List<string> CustomClaims { get; set; } = [];

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// External identity provider user ID (e.g., "auth0|507f1f77bcf86cd799439011")
    /// Links this local user record to their external identity
    /// </summary>
    public string? ExternalUserId { get; set; }

    /// <summary>
    /// Last time this user was synchronized with the external identity provider
    /// Used to track when user data was last updated from the provider
    /// </summary>
    public DateTimeOffset? ExternalUserLastSync { get; set; }
}
