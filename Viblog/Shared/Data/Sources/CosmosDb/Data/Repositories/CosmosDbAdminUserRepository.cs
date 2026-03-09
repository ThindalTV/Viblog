using Microsoft.EntityFrameworkCore;
using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Repositories;

namespace Viblog.Shared.Data.Sources.CosmosDb.Data.Repositories;

/// <summary>
/// CosmosDB implementation of AdminUser repository
/// Inherits base CRUD operations and adds user-specific queries
/// </summary>
public class CosmosDbAdminUserRepository : CosmosDbRepository<AdminUser>, IAdminUserRepository
{
    public CosmosDbAdminUserRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<PagedResult<AdminUser>> GetAllAsync(
        PagingParameters pagingParameters,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        // Use WithPartitionKey for optimal CosmosDB query performance
        var query = _dbSet
            .WithPartitionKey("users")
            .Where(u => !u.IsDeleted);

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
            .WithPartitionKey("users")
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AdminUser?> GetByExternalIdAsync(string externalUserId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUserId);

        return await _dbSet
            .WithPartitionKey("users")
            .FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId && !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        // Use WithPartitionKey for optimal CosmosDB query performance
        // All AdminUser entities use "users" as their partition key
        // Note: Using FirstOrDefaultAsync != null instead of AnyAsync due to CosmosDB query generation issues
        return await _dbSet
            .WithPartitionKey("users")
            .FirstOrDefaultAsync(u => !u.IsDeleted, cancellationToken) is not null;
    }
}
