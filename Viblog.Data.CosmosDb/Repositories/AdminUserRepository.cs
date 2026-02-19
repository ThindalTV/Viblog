using Microsoft.EntityFrameworkCore;
using Viblog.Data.CosmosDb.Data;
using Viblog.Data.CosmosDb.Data.Repositories;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.CosmosDb.Repositories;

/// <summary>
/// CosmosDB implementation of AdminUser repository
/// Inherits base CRUD operations and adds user-specific queries
/// </summary>
public class AdminUserRepository : CosmosDbRepository<AdminUser>, IAdminUserRepository
{
    public AdminUserRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<PagedResult<AdminUser>> GetAllAsync(
        PagingParameters pagingParameters,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet.Where(u => !u.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.Email)
            .Skip(pagingParameters.Skip)
            .Take(pagingParameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUser>(users, totalCount, pagingParameters.PageNumber, pagingParameters.PageSize);
    }

    /// <inheritdoc/>
    public async Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AdminUser?> GetByExternalIdAsync(string externalUserId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUserId);

        return await _dbSet
            .FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId && !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(u => !u.IsDeleted, cancellationToken);
    }
}
