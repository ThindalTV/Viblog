namespace Viblog.Infrastructure.Admin.Services;

/// <summary>
/// Dialog type enumeration
/// </summary>
public enum DialogType
{
    /// <summary>
    /// Standard confirmation/alert dialog
    /// </summary>
    Standard,

    /// <summary>
    /// Markdown syntax cheatsheet
    /// </summary>
    MarkdownSyntax,

    /// <summary>
    /// Password reset dialog
    /// </summary>
    PasswordReset
}

/// <summary>
/// Base class for dialog information
/// </summary>
public abstract class DialogInfo
{
    /// <summary>
    /// Type of dialog to display
    /// </summary>
    public DialogType Type { get; set; }
}

/// <summary>
/// Information for message/confirmation dialogs
/// </summary>
public class MessageDialogInfo : DialogInfo
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

    public MessageDialogInfo()
    {
        Type = DialogType.Standard;
    }
}

/// <summary>
/// Information for markdown syntax cheatsheet dialog
/// </summary>
public class MarkdownSyntaxDialogInfo : DialogInfo
{
    public MarkdownSyntaxDialogInfo()
    {
        Type = DialogType.MarkdownSyntax;
    }
}

/// <summary>
/// Information for password reset dialog
/// </summary>
public class PasswordResetDialogInfo : DialogInfo
{
    /// <summary>
    /// Dialog title
    /// </summary>
    public string Title { get; set; } = "Reset Password";

    /// <summary>
    /// User ID whose password is being reset
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User name (for display purposes)
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Async action to execute when confirmed with new password
    /// </summary>
    public Func<string, Task>? OnConfirmAsync { get; set; }

    public PasswordResetDialogInfo()
    {
        Type = DialogType.PasswordReset;
    }
}
