using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Infrastructure.Authentication;

/// <summary>
/// Interface for external identity provider synchronization service
/// Handles syncing users between the identity provider (e.g., Auth0) and local database
/// </summary>
public interface IIdentityProviderSyncService
{
    /// <summary>
    /// Synchronize a user from the identity provider to local database after login
    /// Creates or updates the local user record based on provider claims
    /// </summary>
    /// <param name="externalUserId">External provider user ID (e.g., "auth0|507f...")</param>
    /// <param name="email">User's email address from provider</param>
    /// <param name="name">User's name from provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The synchronized local user</returns>
    Task<AdminUser?> SyncUserAsync(
        string externalUserId,
        string email,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new user in the identity provider and sync to local database
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="name">User's display name</param>
    /// <param name="password">User's password</param>
    /// <param name="claims">Local authorization claims to assign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created local user and validation result</returns>
    Task<(AdminUser? User, UserValidationResult ValidationResult)> CreateUserAsync(
        string email,
        string name,
        string password,
        IEnumerable<string> claims,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset a user's password in the identity provider
    /// Sends a password reset email via the provider
    /// </summary>
    /// <param name="userId">Local user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<UserValidationResult> ResetPasswordAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a user from the identity provider and mark as deleted locally
    /// </summary>
    /// <param name="userId">Local user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or create the default admin user in the identity provider
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The admin user</returns>
    Task<AdminUser> GetOrCreateDefaultAdminAsync(CancellationToken cancellationToken = default);
}
