namespace Vilog.Admin.Configuration;

/// <summary>
/// Configuration for admin authentication
/// This will be replaced with an external authentication service in the future
/// </summary>
public class AdminAuthenticationSettings
{
    /// <summary>
    /// Hardcoded admin email for temporary authentication
    /// </summary>
    public string AdminEmail { get; set; } = "eric@ericjohansson.se";

    /// <summary>
    /// Hardcoded admin password for temporary authentication
    /// </summary>
    public string AdminPassword { get; set; } = "admin123!";

    /// <summary>
    /// Cookie authentication scheme name
    /// </summary>
    public const string AuthenticationScheme = "AdminAuthenticationScheme";

    /// <summary>
    /// Login path
    /// </summary>
    public const string LoginPath = "/admin/login";

    /// <summary>
    /// Access denied path
    /// </summary>
    public const string AccessDeniedPath = "/admin/access-denied";
}
