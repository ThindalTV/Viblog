using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Shared.Data.Repositories;

/// <summary>
/// Repository interface for AdminUser entity
/// </summary>
public interface IAdminUserRepository
{
    /// <summary>
    /// Get all users with pagination
    /// </summary>
    Task<PagedResult<AdminUser>> GetAllAsync(
        PagingParameters pagingParameters,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a user by ID
    /// </summary>
    Task<AdminUser?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a user by email
    /// </summary>
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a user by external identity provider ID
    /// </summary>
    Task<AdminUser?> GetByExternalIdAsync(string externalUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if any users exist
    /// </summary>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new user
    /// </summary>
    Task<AdminUser> AddAsync(AdminUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing user
    /// </summary>
    Task<AdminUser> UpdateAsync(AdminUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a user (soft delete)
    /// </summary>
    Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default);
}
