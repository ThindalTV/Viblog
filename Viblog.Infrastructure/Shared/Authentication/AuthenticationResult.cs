using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Shared.Authentication;

/// <summary>
/// Result of an authentication attempt
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// Whether authentication was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The authenticated user (null if authentication failed)
    /// </summary>
    public User? User { get; init; }

    /// <summary>
    /// Error message if authentication failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Create a successful authentication result
    /// </summary>
    public static AuthenticationResult Successful(User user) => new()
    {
        Success = true,
        User = user
    };

    /// <summary>
    /// Create a failed authentication result
    /// </summary>
    public static AuthenticationResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
