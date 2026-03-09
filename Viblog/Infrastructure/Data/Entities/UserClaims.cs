namespace Viblog.Infrastructure.Data.Entities;

/// <summary>
/// Standard claims available in the system
/// </summary>
public static class UserClaims
{
    /// <summary>
    /// Ability to create, edit, and delete blog posts
    /// </summary>
    public const string PostWrite = "post:write";

    /// <summary>
    /// Ability to create, edit, and delete pages
    /// </summary>
    public const string PageWrite = "page:write";

    /// <summary>
    /// Ability to read statistics and analytics
    /// </summary>
    public const string StatisticsRead = "statistics:read";

    /// <summary>
    /// Ability to view users in the admin interface
    /// </summary>
    public const string UserRead = "user:read";

    /// <summary>
    /// Ability to create, edit, and delete users
    /// </summary>
    public const string UserWrite = "user:write";

    /// <summary>
    /// Get all available claims
    /// </summary>
    public static IReadOnlyList<string> AllClaims => [
        PostWrite,
        PageWrite,
        StatisticsRead,
        UserRead,
        UserWrite
    ];

    /// <summary>
    /// Get default claims for new admin users
    /// </summary>
    public static IReadOnlyList<string> DefaultAdminClaims => AllClaims.ToList();
}
