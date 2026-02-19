using Microsoft.EntityFrameworkCore;
using Viblog.Data.CosmosDb.Data;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.CosmosDb.Repositories;

/// <summary>
/// CosmosDB implementation of AdminUser repository
/// </summary>
public class AdminUserRepository : IAdminUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AdminUserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<PagedResult<AdminUser>> GetAllAsync(
        PagingParameters pagingParameters,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbContext.Set<AdminUser>().Where(u => !u.IsDeleted);

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
    public async Task<AdminUser?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _dbContext.Set<AdminUser>()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _dbContext.Set<AdminUser>()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AdminUser?> GetByExternalIdAsync(string externalUserId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUserId);

        return await _dbContext.Set<AdminUser>()
            .FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId && !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<AdminUser>()
            .AnyAsync(u => !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AdminUser> AddAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        _dbContext.Set<AdminUser>().Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    /// <inheritdoc/>
    public async Task<AdminUser> UpdateAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        _dbContext.Set<AdminUser>().Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var user = await GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
