namespace Viblog.Infrastructure.Shared.Data.Entities;

/// <summary>
/// Represents a user in the system
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// User's display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// User's email address (used for authentication)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password for authentication
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Collection of claims assigned to this user
    /// </summary>
    public List<string> Claims { get; set; } = [];

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }
}
