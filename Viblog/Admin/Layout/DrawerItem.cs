namespace Viblog.Admin.Layout;

/// <summary>
/// Represents a navigation item in the admin sidebar
/// </summary>
public record DrawerItem
{
    /// <summary>
    /// The display text for the menu item
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The Material Symbols icon name to display for the menu item
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// The URL this menu item navigates to
    /// </summary>
    public required string Url { get; init; }
}
