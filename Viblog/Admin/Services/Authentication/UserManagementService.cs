using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Admin.Services.Authentication;

/// <summary>
/// User management service implementation
/// Handles user business logic using repository pattern
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly IAdminUserRepository _userRepository;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        IAdminUserRepository userRepository,
        ILogger<UserManagementService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AdminUser>> GetUsersAsync(
        PagingParameters pagingParameters,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await _userRepository.GetAllAsync(pagingParameters, includeInactive, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<AdminUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _userRepository.GetByIdAsync(userId, "users", cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<AdminUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return await _userRepository.GetByEmailAsync(email, cancellationToken);
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
            var user = await _userRepository.GetByIdAsync(userId, "users", cancellationToken);

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

            user.DisplayName = name.Trim();
            user.CustomClaims = claimsList;
            user.IsActive = isActive;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

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
            await _userRepository.DeleteAsync(userId, "users", softDelete: true, cancellationToken);

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
            var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);

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
        return await _userRepository.AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<AdminUser?> GetUserByExternalIdAsync(string externalUserId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUserId);

        return await _userRepository.GetByExternalIdAsync(externalUserId, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<AdminUser?> CreateOrUpdateFromExternalLoginAsync(
        string externalUserId,
        string email,
        string displayName,
        IEnumerable<string>? claims = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        try
        {
            // Check if user exists by external ID
            var existingUser = await GetUserByExternalIdAsync(externalUserId, cancellationToken);

            if (existingUser != null)
            {
                // Update existing user
                existingUser.Email = email.Trim().ToLowerInvariant();
                existingUser.DisplayName = displayName?.Trim() ?? email;
                existingUser.ExternalUserLastSync = DateTimeOffset.UtcNow;
                existingUser.UpdatedAt = DateTimeOffset.UtcNow;
                existingUser.LastLoginAt = DateTimeOffset.UtcNow;

                await _userRepository.UpdateAsync(existingUser, cancellationToken);
                return existingUser;
            }

            // Check if user exists by email (migration scenario)
            var userByEmail = await GetUserByEmailAsync(email, cancellationToken);

            if (userByEmail != null)
            {
                // Link existing local user to external provider
                await LinkToExternalProviderAsync(userByEmail.Id, externalUserId, cancellationToken);
                userByEmail.LastLoginAt = DateTimeOffset.UtcNow;
                await _userRepository.UpdateAsync(userByEmail, cancellationToken);
                return userByEmail;
            }

            // Create new user
            var newUser = new AdminUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email.Trim().ToLowerInvariant(),
                DisplayName = displayName?.Trim() ?? email,
                ExternalUserId = externalUserId,
                ExternalUserLastSync = DateTimeOffset.UtcNow,
                CustomClaims = claims?.ToList() ?? [],
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            };

            await _userRepository.AddAsync(newUser, cancellationToken);
            return newUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or updating user from external login. ExternalUserId: {ExternalUserId}, Email: {Email}",
                externalUserId, email);
            return null;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> LinkToExternalProviderAsync(
        string userId,
        string externalUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUserId);

        try
        {
            var user = await GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return false;
            }

            user.ExternalUserId = externalUserId;
            user.ExternalUserLastSync = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking user {UserId} to external provider {ExternalUserId}",
                userId, externalUserId);
            return false;
        }
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
