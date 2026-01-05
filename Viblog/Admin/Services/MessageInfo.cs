namespace Vilog.Admin.Services;

/// <summary>
/// Types of messages that can be displayed
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Informational message (blue)
    /// </summary>
    Info,

    /// <summary>
    /// Success message (green)
    /// </summary>
    Success,

    /// <summary>
    /// Warning message (yellow/orange)
    /// </summary>
    Warning,

    /// <summary>
    /// Error message (red)
    /// </summary>
    Error
}

/// <summary>
/// Represents a message to be displayed to the user
/// </summary>
public class MessageInfo
{
    /// <summary>
    /// Type of message
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// User-friendly message text
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional exception for detailed error information (only shown in development)
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Whether to show detailed exception information
    /// </summary>
    public bool ShowDetails { get; set; } = true;

    /// <summary>
    /// Whether to include stack trace in details
    /// </summary>
    public bool IncludeStackTrace { get; set; } = true;

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}
