namespace Viblog.Admin.Services;

/// <summary>
/// Service for managing and displaying dialogs
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Event raised when a dialog should be shown
    /// </summary>
    event EventHandler? OnDialogChanged;

    /// <summary>
    /// Current dialog being displayed
    /// </summary>
    DialogInfo? CurrentDialog { get; }

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
    public event EventHandler? OnDialogChanged;

    /// <inheritdoc/>
    public DialogInfo? CurrentDialog => _currentDialog;

    /// <inheritdoc/>
    public void ShowConfirmation(string title, string message, Action onConfirm, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        _currentDialog = new DialogInfo
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText,
            CancelText = cancelText,
            ShowConfirm = true,
            ShowCancel = true,
            OnConfirm = onConfirm
        };

        NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void ShowConfirmationAsync(string title, string message, Func<Task> onConfirmAsync, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        _currentDialog = new DialogInfo
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText,
            CancelText = cancelText,
            ShowConfirm = true,
            ShowCancel = true,
            OnConfirmAsync = onConfirmAsync
        };

        NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void ShowAlert(string title, string message, Action? onConfirm = null, string confirmText = "OK")
    {
        _currentDialog = new DialogInfo
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText,
            ShowConfirm = true,
            ShowCancel = false,
            OnConfirm = onConfirm
        };

        NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void Close()
    {
        _currentDialog = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnDialogChanged?.Invoke(this, EventArgs.Empty);
    }
}
