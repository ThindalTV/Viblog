using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Admin.Services.Authentication;

/// <summary>
/// Local authentication provider using ASP.NET Core Identity
/// </summary>
public class LocalAuthenticationProvider : IAuthenticationProvider
{
    private readonly UserManager<AdminUser> _userManager;
    private readonly IAuditLogService? _auditLogService;
    private readonly ILogger<LocalAuthenticationProvider> _logger;

    public LocalAuthenticationProvider(
        UserManager<AdminUser> userManager,
        ILogger<LocalAuthenticationProvider> logger,
        IAuditLogService? auditLogService = null)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditLogService = auditLogService; // Optional dependency
    }

    /// <inheritdoc/>
    public virtual async Task<AuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        try
        {
            var user = await _userManager.FindByEmailAsync(email);

            // Filter out deleted users
            if (user != null && user.IsDeleted)
            {
                user = null;
            }

            if (user is null)
            {
                _logger.LogWarning("Authentication failed: User not found with email {Email}", email);

                // Log failed login attempt (no user ID available)
                if (_auditLogService != null)
                {
                    await _auditLogService.LogActionAsync(
                        userId: "unknown",
                        userName: email,
                        userEmail: email,
                        action: AuditAction.LoginFailed,
                        entityType: EntityType.Authentication,
                        description: $"Failed login attempt for {email} - user not found",
                        result: ActionResult.Failed,
                        errorMessage: "User not found",
                        cancellationToken: cancellationToken);
                }

                return AuthenticationResult.Failed("Invalid email or password.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Authentication failed: User account is inactive for {Email}", email);

                // Log failed login attempt due to inactive account
                if (_auditLogService != null)
                {
                    await _auditLogService.LogActionAsync(
                        userId: user.Id,
                        userName: user.DisplayName,
                        userEmail: user.Email!,
                        action: AuditAction.LoginFailed,
                        entityType: EntityType.Authentication,
                        description: $"Failed login attempt - account inactive",
                        result: ActionResult.Unauthorized,
                        errorMessage: "Account is inactive",
                        cancellationToken: cancellationToken);
                }

                return AuthenticationResult.Failed("User account is inactive.");
            }

            // Use Identity's built-in password verification
            var passwordValid = await _userManager.CheckPasswordAsync(user, password);

            if (!passwordValid)
            {
                _logger.LogWarning("Authentication failed: Invalid password for {Email}", email);

                // Log failed login attempt due to wrong password
                if (_auditLogService != null)
                {
                    await _auditLogService.LogActionAsync(
                        userId: user.Id,
                        userName: user.DisplayName,
                        userEmail: user.Email!,
                        action: AuditAction.LoginFailed,
                        entityType: EntityType.Authentication,
                        description: $"Failed login attempt - invalid password",
                        result: ActionResult.Failed,
                        errorMessage: "Invalid password",
                        cancellationToken: cancellationToken);
                }

                return AuthenticationResult.Failed("Invalid email or password.");
            }

            // Update last login timestamp
            user.LastLoginAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            // Log successful login
            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    userId: user.Id,
                    userName: user.DisplayName,
                    userEmail: user.Email!,
                    action: AuditAction.Login,
                    entityType: EntityType.Authentication,
                    description: $"User logged in successfully",
                    result: ActionResult.Success,
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("User {Email} authenticated successfully", email);
            return AuthenticationResult.Successful(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for {Email}", email);
            return AuthenticationResult.Failed("An error occurred during authentication.");
        }
    }

    /// <inheritdoc/>
    public virtual UserValidationResult ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return UserValidationResult.Invalid(errors);
        }

        if (password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one number.");
        }

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            errors.Add("Password must contain at least one special character.");
        }

        return errors.Count > 0
            ? UserValidationResult.Invalid(errors)
            : UserValidationResult.Valid();
    }

    /// <inheritdoc/>
    public virtual async Task<PasswordChangeResult> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        try
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null || user.IsDeleted)
            {
                return PasswordChangeResult.Failed("User not found.");
            }

            // Use Identity's built-in password change method (verifies current password internally)
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                var errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));

                _logger.LogWarning("Password change failed for user {UserId}: {Error}", userId, errorMessage);

                // Log failed password change attempt
                if (_auditLogService != null)
                {
                    // Check if it's a current password error or validation error
                    var isCurrentPasswordError = result.Errors.Any(e => e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) && 
                                                                          e.Description.Contains("incorrect", StringComparison.OrdinalIgnoreCase));

                    await _auditLogService.LogActionAsync(
                        userId: user.Id,
                        userName: user.DisplayName,
                        userEmail: user.Email!,
                        action: AuditAction.PasswordChanged,
                        entityType: EntityType.User,
                        entityId: user.Id,
                        entityName: user.DisplayName,
                        description: isCurrentPasswordError 
                            ? "Failed password change - incorrect current password" 
                            : "Failed password change - validation error",
                        result: isCurrentPasswordError ? ActionResult.Failed : ActionResult.ValidationError,
                        errorMessage: errorMessage,
                        cancellationToken: cancellationToken);
                }

                return PasswordChangeResult.Failed(errorMessage);
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            // Log successful password change
            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    userId: user.Id,
                    userName: user.DisplayName,
                    userEmail: user.Email!,
                    action: AuditAction.PasswordChanged,
                    entityType: EntityType.User,
                    entityId: user.Id,
                    entityName: user.DisplayName,
                    description: "Password changed successfully",
                    result: ActionResult.Success,
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);
            return PasswordChangeResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return PasswordChangeResult.Failed("An error occurred while changing the password.");
        }
    }
}
