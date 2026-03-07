namespace Viblog.Infrastructure.Admin.Services;

/// <summary>
/// Service for managing and displaying messages to users
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Event raised when a message changes
    /// </summary>
    event EventHandler? OnMessageChanged;

    /// <summary>
    /// Current message being displayed
    /// </summary>
    MessageInfo? CurrentMessage { get; }

    /// <summary>
    /// Set a message with a specific type
    /// </summary>
    /// <param name="type">Type of message</param>
    /// <param name="message">Message text</param>
    void SetMessage(MessageType type, string message);

    /// <summary>
    /// Set a success message
    /// </summary>
    /// <param name="message">Success message text</param>
    void SetSuccess(string message);

    /// <summary>
    /// Set an informational message
    /// </summary>
    /// <param name="message">Info message text</param>
    void SetInfo(string message);

    /// <summary>
    /// Set a warning message
    /// </summary>
    /// <param name="message">Warning message text</param>
    void SetWarning(string message);

    /// <summary>
    /// Set a failure/error message
    /// </summary>
    /// <param name="message">Error message text</param>
    void SetFail(string message);

    /// <summary>
    /// Set an error message with exception details
    /// </summary>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="message">User-friendly error message</param>
    /// <param name="includeStackTrace">Whether to include stack trace (default: true)</param>
    void SetError(Exception exception, string message, bool includeStackTrace = true);

    /// <summary>
    /// Clear the current message
    /// </summary>
    void Clear();
}
