using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Admin.Services.Authentication;

/// <summary>
/// User management service implementation
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthenticationProvider _authenticationProvider;
    private readonly IAuditLogService? _auditLogService;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        IUserRepository userRepository,
        IAuthenticationProvider authenticationProvider,
        ILogger<UserManagementService> logger,
        IAuditLogService? auditLogService = null)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _authenticationProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditLogService = auditLogService; // Optional dependency
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<User>> GetUsersAsync(
        PagingParameters pagingParameters,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        if (includeInactive)
        {
            return await _userRepository.GetAllAsync(
                pagingParameters,
                u => u.Email,
                ascending: true,
                includeDeleted: false,
                cancellationToken);
        }

        return await _userRepository.FindAsync(
            u => u.IsActive,
            pagingParameters,
            u => u.Email,
            ascending: true,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _userRepository.GetByIdAsync(userId, "users", cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return await _userRepository.GetByEmailAsync(email, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<(User? User, UserValidationResult ValidationResult)> CreateUserAsync(
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
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                GroupKey = "users",
                Name = name.Trim(),
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = _authenticationProvider.HashPassword(password),
                Claims = claimsList,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Log user creation
            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    userId: user.Id,
                    userName: user.Name,
                    userEmail: user.Email,
                    action: AuditAction.UserCreated,
                    entityType: EntityType.User,
                    entityId: user.Id,
                    entityName: user.Name,
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
    public virtual async Task<(User? User, UserValidationResult ValidationResult)> UpdateUserAsync(
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
            var user = await _userRepository.GetByIdAsync(userId, "users", cancellationToken);
            if (user is null)
            {
                return (null, UserValidationResult.Invalid("User not found."));
            }

            // SECURITY: Email is the login identifier and cannot be changed
            // If a different email is needed, a new account must be created
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (user.Email != normalizedEmail)
            {
                _logger.LogWarning("Attempt to change email for user {UserId} from {OldEmail} to {NewEmail} was blocked", 
                    userId, user.Email, normalizedEmail);
                return (null, UserValidationResult.Invalid("Email cannot be changed. Create a new account if a different email is needed."));
            }

            var oldActive = user.IsActive;
            var oldClaims = user.Claims.ToList();

            user.Name = name.Trim();
            // Email deliberately NOT updated - see security note above
            user.Claims = claimsList;
            user.IsActive = isActive;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Log user update
            if (_auditLogService != null)
            {
                // Determine what changed
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
                    userName: user.Name,
                    userEmail: user.Email,
                    action: action,
                    entityType: EntityType.User,
                    entityId: user.Id,
                    entityName: user.Name,
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
            // Get user info before deleting for audit log
            var user = await _userRepository.GetByIdAsync(userId, "users", cancellationToken);

            await _userRepository.DeleteAsync(userId, "users", softDelete: true, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Log user deletion
            if (_auditLogService != null && user != null)
            {
                await _auditLogService.LogActionAsync(
                    userId: userId,
                    userName: user.Name,
                    userEmail: user.Email,
                    action: AuditAction.UserDeleted,
                    entityType: EntityType.User,
                    entityId: user.Id,
                    entityName: user.Name,
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
            var emailExists = string.IsNullOrWhiteSpace(excludeUserId)
                ? await _userRepository.EmailExistsAsync(email, cancellationToken)
                : await _userRepository.EmailExistsAsync(email, excludeUserId, cancellationToken);

            if (emailExists)
            {
                errors.Add("Email address is already in use.");
            }
        }

        return errors.Count > 0
            ? UserValidationResult.Invalid(errors)
            : UserValidationResult.Valid();
    }

    /// <inheritdoc/>
    public virtual async Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken = default)
    {
        return await _userRepository.AnyAsync(u => true, includeDeleted: false, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<User> CreateDefaultAdminUserAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating default admin user");

        var adminUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "users",
            Name = "Administrator",
            Email = "admin@viblog.local",
            PasswordHash = _authenticationProvider.HashPassword("admin123!"),
            Claims = UserClaims.DefaultAdminClaims.ToList(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _userRepository.AddAsync(adminUser, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

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
            var user = await _userRepository.GetByIdAsync(userId, "users", cancellationToken);
            if (user is null)
            {
                return UserValidationResult.Invalid("User not found.");
            }

            user.PasswordHash = _authenticationProvider.HashPassword(newPassword);
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Log password reset (admin-initiated)
            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    userId: user.Id,
                    userName: user.Name,
                    userEmail: user.Email,
                    action: AuditAction.PasswordReset,
                    entityType: EntityType.User,
                    entityId: user.Id,
                    entityName: user.Name,
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
