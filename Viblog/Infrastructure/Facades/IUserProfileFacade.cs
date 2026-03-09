using Viblog.Infrastructure.Authentication;
using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Infrastructure.Facades;

/// <summary>
/// Facade interface for user profile self-service operations
/// </summary>
public interface IUserProfileFacade
{
    /// <summary>
    /// Get the current user's profile by ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user or null if not found</returns>
    Task<AdminUser?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the current user's profile information (name and email only)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="name">Updated display name</param>
    /// <param name="email">Updated email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated user and validation result</returns>
    Task<(AdminUser? User, UserValidationResult ValidationResult)> UpdateProfileAsync(
        string userId,
        string name,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Change the current user's password
    /// TODO: Will be replaced with Auth0 password reset in Step 14
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="currentPassword">The user's current password</param>
    /// <param name="newPassword">The new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<UserValidationResult> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}
