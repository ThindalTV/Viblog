using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Shared.Data.Repositories;

/// <summary>
/// Repository interface for user operations
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Get a user by their email address
    /// </summary>
    /// <param name="email">The email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user or null if not found</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user with the specified email exists
    /// </summary>
    /// <param name="email">The email address to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a user exists with the email</returns>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user with the specified email exists, excluding a specific user ID
    /// </summary>
    /// <param name="email">The email address to check</param>
    /// <param name="excludeUserId">The user ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if another user exists with the email</returns>
    Task<bool> EmailExistsAsync(string email, string excludeUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the last login timestamp for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="loginTime">The login timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateLastLoginAsync(string userId, DateTimeOffset loginTime, CancellationToken cancellationToken = default);
}
