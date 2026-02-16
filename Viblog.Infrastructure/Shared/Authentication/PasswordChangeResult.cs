namespace Viblog.Infrastructure.Shared.Authentication;

/// <summary>
/// Result of a password change operation
/// </summary>
public class PasswordChangeResult
{
    /// <summary>
    /// Whether the password change was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Create a successful password change result
    /// </summary>
    public static PasswordChangeResult Successful() => new()
    {
        Success = true
    };

    /// <summary>
    /// Create a failed password change result
    /// </summary>
    public static PasswordChangeResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
