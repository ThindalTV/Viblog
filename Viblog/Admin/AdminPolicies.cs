namespace Viblog.Admin;

/// <summary>
/// Authorization policy names for admin area
/// </summary>
public static class AdminPolicies
{
    /// <summary>
    /// General admin access - requires authentication
    /// </summary>
    public const string Admin = nameof(Admin);

    /// <summary>
    /// Ability to create, edit, and delete blog posts
    /// </summary>
    public const string RequirePostWrite = nameof(RequirePostWrite);

    /// <summary>
    /// Ability to create, edit, and delete pages
    /// </summary>
    public const string RequirePageWrite = nameof(RequirePageWrite);

    /// <summary>
    /// Ability to read statistics and analytics
    /// </summary>
    public const string RequireStatisticsRead = nameof(RequireStatisticsRead);

    /// <summary>
    /// Ability to view users in the admin interface
    /// </summary>
    public const string RequireUserRead = nameof(RequireUserRead);

    /// <summary>
    /// Ability to create, edit, and delete users
    /// </summary>
    public const string RequireUserWrite = nameof(RequireUserWrite);
}
