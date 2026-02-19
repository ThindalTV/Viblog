namespace Viblog.Infrastructure.Shared.Data.Entities;

/// <summary>
/// Admin user entity for blog administration
/// All users are admin users - there is no public user registration
/// Authentication is handled by Auth0, authorization (permissions) stored locally
/// </summary>
public class AdminUser
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public string Id { get; set; } = string.Empty;

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
    /// CosmosDB partition key for efficient querying
    /// </summary>
    public string GroupKey { get; set; } = "users";

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the user was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Whether the user has been soft-deleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When the user was deleted (if soft-deleted)
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    // External Identity Provider Integration
    // These link the local user to their external identity (e.g., Auth0, Azure AD B2C)

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
