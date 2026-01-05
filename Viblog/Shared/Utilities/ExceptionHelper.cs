using System.Text;

namespace Vilog.Shared.Utilities;

/// <summary>
/// Utility methods for exception handling and formatting
/// </summary>
public static class ExceptionHelper
{
    /// <summary>
    /// Gets a detailed error message including all inner exceptions
    /// </summary>
    /// <param name="exception">The exception to format</param>
    /// <param name="includeStackTrace">Whether to include stack trace (useful for debug mode)</param>
    /// <returns>Formatted exception message</returns>
    public static string GetDetailedMessage(Exception exception, bool includeStackTrace = false)
    {
        if (exception == null)
            return string.Empty;

        var sb = new StringBuilder();
        var currentException = exception;
        var level = 0;

        while (currentException != null)
        {
            if (level > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Inner Exception {level}:");
            }

            sb.AppendLine($"Type: {currentException.GetType().Name}");
            sb.AppendLine($"Message: {currentException.Message}");

            if (includeStackTrace && !string.IsNullOrWhiteSpace(currentException.StackTrace))
            {
                sb.AppendLine("Stack Trace:");
                sb.AppendLine(currentException.StackTrace);
            }

            currentException = currentException.InnerException;
            level++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets a user-friendly error message (without stack trace)
    /// </summary>
    /// <param name="exception">The exception to format</param>
    /// <returns>User-friendly error message</returns>
    public static string GetUserFriendlyMessage(Exception exception)
    {
        return GetDetailedMessage(exception, includeStackTrace: false);
    }

    /// <summary>
    /// Gets all exception messages concatenated (useful for logging)
    /// </summary>
    /// <param name="exception">The exception to format</param>
    /// <returns>All exception messages</returns>
    public static string GetAllMessages(Exception exception)
    {
        if (exception == null)
            return string.Empty;

        var messages = new List<string>();
        var currentException = exception;

        while (currentException != null)
        {
            messages.Add(currentException.Message);
            currentException = currentException.InnerException;
        }

        return string.Join(" --> ", messages);
    }
}
