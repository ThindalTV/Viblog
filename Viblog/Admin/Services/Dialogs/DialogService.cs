namespace Viblog.Admin.Services.Dialogs;

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
    public void ShowPasswordResetDialog(string userId, string userName, Func<string, Task> onConfirmAsync)
    {
        _currentDialog = new PasswordResetDialogInfo
        {
            UserId = userId,
            UserName = userName,
            OnConfirmAsync = onConfirmAsync
        };

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
