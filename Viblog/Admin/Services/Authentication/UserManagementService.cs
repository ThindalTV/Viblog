using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Admin.Services.Authentication;

/// <summary>
/// User management service implementation using ASP.NET Core Identity
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthenticationProvider _authenticationProvider;
    private readonly IAuditLogService? _auditLogService;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        IAuthenticationProvider authenticationProvider,
        ILogger<UserManagementService> logger,
        IAuditLogService? auditLogService = null)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _authenticationProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditLogService = auditLogService; // Optional dependency
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<ApplicationUser>> GetUsersAsync(
        PagingParameters pagingParameters,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _userManager.Users.Where(u => !u.IsDeleted);

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

        return new PagedResult<ApplicationUser>(users, totalCount, pagingParameters.PageNumber, pagingParameters.PageSize);
    }

    /// <inheritdoc/>
    public virtual async Task<ApplicationUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var user = await _userManager.FindByIdAsync(userId);
        return user != null && !user.IsDeleted ? user : null;
    }

    /// <inheritdoc/>
    public virtual async Task<ApplicationUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var user = await _userManager.FindByEmailAsync(email);
        return user != null && !user.IsDeleted ? user : null;
    }

    /// <inheritdoc/>
    public virtual async Task<(ApplicationUser? User, UserValidationResult ValidationResult)> CreateUserAsync(
        string name,
        string email,
        string password,
        IEnumerable<string> claims,
        CancellationToken cancellationToken = default)
    {
        var claimsList = claims.ToList();

        // Validate user data
        var validationResult = await ValidateUserDataAsync(name, email, null, cancellationToken);
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Validate password
        var passwordValidation = _authenticationProvider.ValidatePassword(password);
        if (!passwordValidation.IsValid)
        {
            return (null, passwordValidation);
        }

        try
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email.Trim().ToLowerInvariant(),
                Email = email.Trim().ToLowerInvariant(),
                DisplayName = name.Trim(),
                CustomClaims = claimsList,
                IsActive = true,
                GroupKey = "users",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                EmailConfirmed = true // Auto-confirm for admin-created users
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return (null, UserValidationResult.Invalid(errors));
            }

            // Log user creation
            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    userId: user.Id,
                    userName: user.DisplayName,
                    userEmail: user.Email!,
                    action: AuditAction.UserCreated,
                    entityType: EntityType.User,
                    entityId: user.Id,
                    entityName: user.DisplayName,
                    description: $"User account created for {user.Email}",
                    result: ActionResult.Success,
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Created user {Email} with ID {UserId}", email, user.Id);
            return (user, UserValidationResult.Valid());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email}", email);
            return (null, UserValidationResult.Invalid("An error occurred while creating the user."));
        }
    }

    /// <inheritdoc/>
    public virtual async Task<(ApplicationUser? User, UserValidationResult ValidationResult)> UpdateUserAsync(
        string userId,
        string name,
        string email,
        IEnumerable<string> claims,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var claimsList = claims.ToList();

        // Validate user data (excluding email uniqueness check since we're not changing it)
        var validationResult = await ValidateUserDataAsync(name, email, userId, cancellationToken);
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.IsDeleted)
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

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return (null, UserValidationResult.Invalid(errors));
            }

            // Log user update
            if (_auditLogService != null)
            {
                var changes = new List<string>();
                if (oldActive != isActive)
                {
                    changes.Add(isActive ? "activated" : "deactivated");
                }
                if (!oldClaims.SequenceEqual(claimsList))
                {
                    changes.Add("claims modified");
                }

                var action = !isActive && oldActive ? AuditAction.UserDeactivated :
                            isActive && !oldActive ? AuditAction.UserActivated :
                            !oldClaims.SequenceEqual(claimsList) ? AuditAction.UserClaimsModified :
                            AuditAction.UserUpdated;

                await _auditLogService.LogActionAsync(
                    userId: userId,
                    userName: user.DisplayName,
                    userEmail: user.Email!,
                    action: action,
                    entityType: EntityType.User,
                    entityId: user.Id,
                    entityName: user.DisplayName,
                    description: changes.Any()
                        ? $"User updated: {string.Join(", ", changes)}"
                        : "User profile updated",
                    result: ActionResult.Success,
                    cancellationToken: cancellationToken);
            }

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
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.IsDeleted)
            {
                return false;
            }

            // Soft delete
            user.IsDeleted = true;
            user.DeletedAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return false;
            }

            // Log user deletion
            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    userId: userId,
                    userName: user.DisplayName,
                    userEmail: user.Email!,
                    action: AuditAction.UserDeleted,
                    entityType: EntityType.User,
                    entityId: user.Id,
                    entityName: user.DisplayName,
                    description: $"User account deleted: {user.Email}",
                    result: ActionResult.Success,
                    cancellationToken: cancellationToken);
            }

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
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && !existingUser.IsDeleted)
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
        return await _userManager.Users.AnyAsync(u => !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<ApplicationUser> CreateDefaultAdminUserAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating default admin user");

        var adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "admin@viblog.local",
            Email = "admin@viblog.local",
            DisplayName = "Administrator",
            CustomClaims = UserClaims.DefaultAdminClaims.ToList(),
            IsActive = true,
            GroupKey = "users",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(adminUser, "admin123!");
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create default admin user: {errors}");
        }

        _logger.LogInformation("Default admin user created with email {Email}", adminUser.Email);
        return adminUser;
    }

    /// <inheritdoc/>
    public virtual async Task<UserValidationResult> ResetPasswordAsync(
        string userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Validate password
        var passwordValidation = _authenticationProvider.ValidatePassword(newPassword);
        if (!passwordValidation.IsValid)
        {
            return passwordValidation;
        }

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.IsDeleted)
            {
                return UserValidationResult.Invalid("User not found.");
            }

            // Remove old password and set new one
            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, newPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return UserValidationResult.Invalid(errors);
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            // Log password reset (admin-initiated)
            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    userId: user.Id,
                    userName: user.DisplayName,
                    userEmail: user.Email!,
                    action: AuditAction.PasswordReset,
                    entityType: EntityType.User,
                    entityId: user.Id,
                    entityName: user.DisplayName,
                    description: $"Password reset by administrator for {user.Email}",
                    result: ActionResult.Success,
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Password reset for user {UserId}", userId);
            return UserValidationResult.Valid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            return UserValidationResult.Invalid("An error occurred while resetting the password.");
        }
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
