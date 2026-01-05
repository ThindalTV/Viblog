namespace Viblog.Admin.Services;

/// <summary>
/// Represents a dialog to be displayed
/// </summary>
public class DialogInfo
{
    /// <summary>
    /// Dialog title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Dialog message/content
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Confirm button text
    /// </summary>
    public string ConfirmText { get; set; } = "Confirm";

    /// <summary>
    /// Cancel button text
    /// </summary>
    public string CancelText { get; set; } = "Cancel";

    /// <summary>
    /// Show confirm button
    /// </summary>
    public bool ShowConfirm { get; set; } = true;

    /// <summary>
    /// Show cancel button
    /// </summary>
    public bool ShowCancel { get; set; } = true;

    /// <summary>
    /// Action to execute when confirmed (synchronous)
    /// </summary>
    public Action? OnConfirm { get; set; }

    /// <summary>
    /// Async action to execute when confirmed
    /// </summary>
    public Func<Task>? OnConfirmAsync { get; set; }

    /// <summary>
    /// Action to execute when cancelled (synchronous)
    /// </summary>
    public Action? OnCancel { get; set; }

    /// <summary>
    /// Async action to execute when cancelled
    /// </summary>
    public Func<Task>? OnCancelAsync { get; set; }
}
