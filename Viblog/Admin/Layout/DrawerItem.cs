using Telerik.SvgIcons;

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
    /// The icon to display for the menu item
    /// </summary>
    public ISvgIcon? Icon { get; init; }

    /// <summary>
    /// The URL this menu item navigates to
    /// </summary>
    public required string Url { get; init; }
}
