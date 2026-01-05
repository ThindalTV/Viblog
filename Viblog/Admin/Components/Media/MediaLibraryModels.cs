namespace Viblog.Admin.Components.Media;

/// <summary>
/// Represents a folder node in the media library tree
/// </summary>
public class FolderNode
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ParentPath { get; set; }
    public bool IsUiOnly { get; set; }
}

/// <summary>
/// Represents a context menu item for media operations
/// </summary>
public class ContextMenuItem
{
    public string Text { get; set; } = "";
    public Telerik.SvgIcons.ISvgIcon? Icon { get; set; }
    public string Action { get; set; } = "";
}
