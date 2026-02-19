using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Shared.Authentication;

/// <summary>
/// Interface for authentication providers that validate credentials and manage passwords
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// Authenticate a user with email and password
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="password">The user's password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with user if successful</returns>
    Task<AuthenticationResult> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate password strength
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <returns>Validation result with any errors</returns>
    UserValidationResult ValidatePassword(string password);

    /// <summary>
    /// Change a user's password
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="currentPassword">The user's current password</param>
    /// <param name="newPassword">The new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password change result</returns>
    Task<PasswordChangeResult> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}
