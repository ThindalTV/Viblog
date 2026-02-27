using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Admin.Facades;

/// <summary>
/// Facade interface for admin user management operations
/// </summary>
public interface IUserManagementFacade
{
    /// <summary>
    /// Get users with pagination
    /// </summary>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="includeInactive">Whether to include inactive users</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of users</returns>
    Task<PagedResult<AdminUser>> GetUsersAsync(
        PagingParameters pagingParameters,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a user by ID for editing
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user or null if not found</returns>
    Task<AdminUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="name">User's display name</param>
    /// <param name="email">User's email address</param>
    /// <param name="password">User's password</param>
    /// <param name="claims">Claims to assign to the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created user and validation result</returns>
    Task<(AdminUser? User, UserValidationResult ValidationResult)> CreateUserAsync(
        string name,
        string email,
        string password,
        IEnumerable<string> claims,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing user's profile information
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="name">Updated display name</param>
    /// <param name="email">Updated email address</param>
    /// <param name="claims">Updated claims</param>
    /// <param name="isActive">Whether the user is active</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated user and validation result</returns>
    Task<(AdminUser? User, UserValidationResult ValidationResult)> UpdateUserAsync(
        string userId,
        string name,
        string email,
        IEnumerable<string> claims,
        bool isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user was deleted</returns>
    Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all available claims in the system
    /// </summary>
    /// <returns>List of available claims</returns>
    IReadOnlyList<string> GetAvailableClaims();

    /// <summary>
    /// Reset a user's password (admin operation)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="newPassword">The new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result indicating success or errors</returns>
    Task<UserValidationResult> ResetPasswordAsync(
        string userId,
        string newPassword,
        CancellationToken cancellationToken = default);
}
