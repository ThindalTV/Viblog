namespace Viblog.Admin.Services.Messaging;

/// <summary>
/// Implementation of message service
/// </summary>
public class MessageService : IMessageService
{
    private MessageInfo? _currentMessage;

    /// <inheritdoc/>
    public event EventHandler? OnMessageChanged;

    /// <inheritdoc/>
    public MessageInfo? CurrentMessage => _currentMessage;

    /// <inheritdoc/>
    public void SetMessage(MessageType type, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _currentMessage = new MessageInfo
        {
            Type = type,
            Message = message,
            ShowDetails = false
        };

        NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void SetSuccess(string message)
    {
        SetMessage(MessageType.Success, message);
    }

    /// <inheritdoc/>
    public void SetInfo(string message)
    {
        SetMessage(MessageType.Info, message);
    }

    /// <inheritdoc/>
    public void SetWarning(string message)
    {
        SetMessage(MessageType.Warning, message);
    }

    /// <inheritdoc/>
    public void SetFail(string message)
    {
        SetMessage(MessageType.Error, message);
    }

    /// <inheritdoc/>
    public void SetError(Exception exception, string message, bool includeStackTrace = true)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _currentMessage = new MessageInfo
        {
            Type = MessageType.Error,
            Message = message,
            Exception = exception,
            ShowDetails = true,
            IncludeStackTrace = includeStackTrace
        };

        NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _currentMessage = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnMessageChanged?.Invoke(this, EventArgs.Empty);
    }
}
