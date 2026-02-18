using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Data.Filesystem.Configuration;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.Filesystem.Data.Repositories;

/// <summary>
/// Filesystem-based repository implementation for user operations
/// </summary>
public class FileSystemUserRepository : FilesystemRepository<User>, IUserRepository
{
    public FileSystemUserRepository(
        IOptions<FilesystemStorageOptions> options,
        ILogger<FilesystemRepository<User>> logger)
        : base(options, logger)
    {
    }

    /// <inheritdoc/>
    public virtual async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return await FirstOrDefaultAsync(
            u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase),
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return await AnyAsync(
            u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase),
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<bool> EmailExistsAsync(string email, string excludeUserId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(excludeUserId);

        return await AnyAsync(
            u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.Id != excludeUserId,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task UpdateLastLoginAsync(string userId, DateTimeOffset loginTime, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Users use email as partition key
        var user = await GetByIdAsync(userId, "users", cancellationToken);
        if (user is not null)
        {
            user.LastLoginAt = loginTime;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await UpdateAsync(user, cancellationToken);
        }
    }
}
