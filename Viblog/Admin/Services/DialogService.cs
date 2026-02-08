namespace Viblog.Admin.Services;

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
    /// Close the current dialog
    /// </summary>
    void Close();
}

/// <summary>
/// Implementation of dialog service
/// </summary>
public class DialogService : IDialogService
{
    private DialogInfo? _currentDialog;

    /// <inheritdoc/>
    public event Func<DialogInfo?, Task>? OnDialogChanged;

    /// <inheritdoc/>
    public DialogInfo? CurrentDialog => _currentDialog;

    /// <inheritdoc/>
    public void ShowConfirmation(string title, string message, Action onConfirm, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        _currentDialog = new MessageDialogInfo
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText,
            CancelText = cancelText,
            ShowConfirm = true,
            ShowCancel = true,
            OnConfirm = onConfirm
        };

        _ = NotifyStateChangedAsync();
    }

    /// <inheritdoc/>
    public void ShowConfirmationAsync(string title, string message, Func<Task> onConfirmAsync, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        _currentDialog = new MessageDialogInfo
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText,
            CancelText = cancelText,
            ShowConfirm = true,
            ShowCancel = true,
            OnConfirmAsync = onConfirmAsync
        };

        _ = NotifyStateChangedAsync();
    }

    /// <inheritdoc/>
    public void ShowAlert(string title, string message, Action? onConfirm = null, string confirmText = "OK")
    {
        _currentDialog = new MessageDialogInfo
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText,
            ShowConfirm = true,
            ShowCancel = false,
            OnConfirm = onConfirm
        };

        _ = NotifyStateChangedAsync();
    }

    /// <inheritdoc/>
    public void ShowMarkdownSyntaxDialog()
    {
        _currentDialog = new MarkdownSyntaxDialogInfo();

        _ = NotifyStateChangedAsync();
    }

    /// <inheritdoc/>
    public void Close()
    {
        _currentDialog = null;
        _ = NotifyStateChangedAsync();
    }

    private async Task NotifyStateChangedAsync()
    {
        await (OnDialogChanged?.Invoke(_currentDialog) ?? Task.CompletedTask);
    }
}
