using Microsoft.AspNetCore.Identity;

namespace Viblog.Infrastructure.Shared.Data.Entities;

/// <summary>
/// Admin user entity for blog administration
/// All users are admin users - there is no public user registration
/// </summary>
public class AdminUser : IdentityUser
{
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

    // Note: Identity already provides:
    // - Email / NormalizedEmail (from IdentityUser)
    // - PasswordHash (from IdentityUser)
    // - UserName (from IdentityUser)
    // - EmailConfirmed, PhoneNumber, etc.
}
