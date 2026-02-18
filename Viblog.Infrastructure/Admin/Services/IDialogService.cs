namespace Viblog.Infrastructure.Admin.Services;

/// <summary>
/// Service for managing and displaying dialogs
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Event raised when a dialog should be shown or hidden
    /// </summary>
    event Func<DialogInfo?, Task>? OnDialogChanged;

    /// <summary>
    /// Show a confirmation dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Dialog message</param>
    /// <param name="onConfirm">Action to execute when confirmed</param>
    /// <param name="confirmText">Confirm button text</param>
    /// <param name="cancelText">Cancel button text</param>
    void ShowConfirmation(string title, string message, Action onConfirm, string confirmText = "Confirm", string cancelText = "Cancel");

    /// <summary>
    /// Show a confirmation dialog with async callback
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Dialog message</param>
    /// <param name="onConfirmAsync">Async action to execute when confirmed</param>
    /// <param name="confirmText">Confirm button text</param>
    /// <param name="cancelText">Cancel button text</param>
    void ShowConfirmationAsync(string title, string message, Func<Task> onConfirmAsync, string confirmText = "Confirm", string cancelText = "Cancel");

    /// <summary>
    /// Show an alert dialog (only confirm button)
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Dialog message</param>
    /// <param name="onConfirm">Optional action to execute when confirmed</param>
    /// <param name="confirmText">Confirm button text</param>
    void ShowAlert(string title, string message, Action? onConfirm = null, string confirmText = "OK");

    /// <summary>
    /// Show the markdown syntax cheatsheet dialog
    /// </summary>
    void ShowMarkdownSyntaxDialog();

    /// <summary>
    /// Show the password reset dialog
    /// </summary>
    /// <param name="userId">User ID whose password is being reset</param>
    /// <param name="userName">User name for display</param>
    /// <param name="onConfirmAsync">Async action to execute when confirmed with new password</param>
    void ShowPasswordResetDialog(string userId, string userName, Func<string, Task> onConfirmAsync);

    /// <summary>
    /// Close the current dialog
    /// </summary>
    void Close();
}
