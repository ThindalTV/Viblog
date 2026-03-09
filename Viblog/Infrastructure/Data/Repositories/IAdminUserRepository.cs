using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for AdminUser entity
/// Extends base repository with user-specific queries
/// </summary>
public interface IAdminUserRepository : IRepository<AdminUser>
{
    /// <summary>
    /// Get all users with pagination
    /// </summary>
    Task<PagedResult<AdminUser>> GetAllAsync(
        PagingParameters pagingParameters,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

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
}
