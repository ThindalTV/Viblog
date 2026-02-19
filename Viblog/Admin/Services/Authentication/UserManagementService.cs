using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Data.CosmosDb.Data;

namespace Viblog.Admin.Services.Authentication;

/// <summary>
/// User management service implementation
/// TEMPORARY STUB: Authentication operations disabled during Auth0 migration (Phase 1)
/// Will be fully implemented with Auth0 sync in Step 11 (Phase 2)
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        ApplicationDbContext dbContext,
        ILogger<UserManagementService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AdminUser>> GetUsersAsync(
        PagingParameters pagingParameters,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        // Direct DB query without UserManager
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
    public virtual async Task<AdminUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var user = await _dbContext.Set<AdminUser>()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        return user;
    }

    /// <inheritdoc/>
    public virtual async Task<AdminUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Set<AdminUser>()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted, cancellationToken);

        return user;
    }

    /// <inheritdoc/>
    public virtual Task<(AdminUser? User, UserValidationResult ValidationResult)> CreateUserAsync(
        string name,
        string email,
        string password,
        IEnumerable<string> claims,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("User creation will be implemented with Auth0 sync in Step 11");
    }

    /// <inheritdoc/>
    public virtual async Task<(AdminUser? User, UserValidationResult ValidationResult)> UpdateUserAsync(
        string userId,
        string name,
        string email,
        IEnumerable<string> claims,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var claimsList = claims.ToList();

        // Validate user data
        var validationResult = await ValidateUserDataAsync(name, email, userId, cancellationToken);
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        try
        {
            var user = await _dbContext.Set<AdminUser>()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

            if (user is null)
            {
                return (null, UserValidationResult.Invalid("User not found."));
            }

            // SECURITY: Email is the login identifier and cannot be changed
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (user.Email != normalizedEmail)
            {
                _logger.LogWarning("Attempt to change email for user {UserId} from {OldEmail} to {NewEmail} was blocked",
                    userId, user.Email, normalizedEmail);
                return (null, UserValidationResult.Invalid("Email cannot be changed. Create a new account if a different email is needed."));
            }

            var oldActive = user.IsActive;
            var oldClaims = user.CustomClaims.ToList();

            user.DisplayName = name.Trim();
            user.CustomClaims = claimsList;
            user.IsActive = isActive;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated user {UserId}", userId);
            return (user, UserValidationResult.Valid());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return (null, UserValidationResult.Invalid("An error occurred while updating the user."));
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        try
        {
            var user = await _dbContext.Set<AdminUser>()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

            if (user is null)
            {
                return false;
            }

            user.IsDeleted = true;
            user.DeletedAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return false;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<UserValidationResult> ValidateUserDataAsync(
        string name,
        string email,
        string? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name is required.");
        }
        else if (name.Trim().Length < 2)
        {
            errors.Add("Name must be at least 2 characters long.");
        }

        // Validate email
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("Email is required.");
        }
        else if (!IsValidEmail(email))
        {
            errors.Add("Email address is not valid.");
        }
        else
        {
            // Check email uniqueness
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var existingUser = await _dbContext.Set<AdminUser>()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted, cancellationToken);

            if (existingUser != null)
            {
                // If we're updating and it's the same user, that's OK
                if (string.IsNullOrWhiteSpace(excludeUserId) || existingUser.Id != excludeUserId)
                {
                    errors.Add("Email address is already in use.");
                }
            }
        }

        return errors.Count > 0
            ? UserValidationResult.Invalid(errors)
            : UserValidationResult.Valid();
    }

    /// <inheritdoc/>
    public virtual async Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<AdminUser>()
            .AnyAsync(u => !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<AdminUser> CreateDefaultAdminUserAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Default admin creation will be implemented with Auth0 sync in Step 11");
    }

    /// <inheritdoc/>
    public virtual Task<UserValidationResult> ResetPasswordAsync(
        string userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Password reset will be handled by Auth0 in Step 11");
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }
}
