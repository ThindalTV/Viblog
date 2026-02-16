using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.Filesystem.Authentication;

/// <summary>
/// Local authentication provider with password hashing for any repository-based storage
/// (Filesystem, SQL, CosmosDB, etc.)
/// </summary>
public class LocalAuthenticationProvider : IAuthenticationProvider
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService? _auditLogService;
    private readonly ILogger<LocalAuthenticationProvider> _logger;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    // Dummy hash used for timing attack mitigation when user doesn't exist
    private static readonly string _dummyPasswordHash = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    public LocalAuthenticationProvider(
        IUserRepository userRepository,
        ILogger<LocalAuthenticationProvider> logger,
        IAuditLogService? auditLogService = null)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            // Always perform password verification to prevent timing attacks
            // Use dummy hash if user doesn't exist to maintain constant time
            var hashToVerify = user?.PasswordHash ?? _dummyPasswordHash;
            var passwordValid = VerifyPassword(password, hashToVerify);

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
                        userName: user.Name,
                        userEmail: user.Email,
                        action: AuditAction.LoginFailed,
                        entityType: EntityType.Authentication,
                        description: $"Failed login attempt - account inactive",
                        result: ActionResult.Unauthorized,
                        errorMessage: "Account is inactive",
                        cancellationToken: cancellationToken);
                }

                return AuthenticationResult.Failed("User account is inactive.");
            }

            if (!passwordValid)
            {
                _logger.LogWarning("Authentication failed: Invalid password for {Email}", email);

                // Log failed login attempt due to wrong password
                if (_auditLogService != null)
                {
                    await _auditLogService.LogActionAsync(
                        userId: user.Id,
                        userName: user.Name,
                        userEmail: user.Email,
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
            await _userRepository.UpdateLastLoginAsync(user.Id, DateTimeOffset.UtcNow, cancellationToken);

            // Log successful login
            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    userId: user.Id,
                    userName: user.Name,
                    userEmail: user.Email,
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
    public virtual string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        // Generate random salt using modern static method
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Use static Pbkdf2 method instead of creating instance
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        // Combine salt and hash for storage
        var hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        return Convert.ToBase64String(hashBytes);
    }

    /// <inheritdoc/>
    public virtual bool VerifyPassword(string password, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        try
        {
            var hashBytes = Convert.FromBase64String(passwordHash);

            if (hashBytes.Length != SaltSize + HashSize)
            {
                return false;
            }

            // Extract salt from stored hash
            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            // Compute hash using static Pbkdf2 method
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            // Use constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                hashBytes.AsSpan(SaltSize),
                hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password hash");
            return false;
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
            var user = await _userRepository.GetByIdAsync(userId, "users", cancellationToken);

            if (user is null)
            {
                return PasswordChangeResult.Failed("User not found.");
            }

            // Verify current password
            if (!VerifyPassword(currentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Password change failed: Invalid current password for user {UserId}", userId);

                // Log failed password change attempt
                if (_auditLogService != null)
                {
                    await _auditLogService.LogActionAsync(
                        userId: user.Id,
                        userName: user.Name,
                        userEmail: user.Email,
                        action: AuditAction.PasswordChanged,
                        entityType: EntityType.User,
                        entityId: user.Id,
                        entityName: user.Name,
                        description: "Failed password change - incorrect current password",
                        result: ActionResult.Failed,
                        errorMessage: "Invalid current password",
                        cancellationToken: cancellationToken);
                }

                return PasswordChangeResult.Failed("Current password is incorrect.");
            }

            // Validate new password
            var validationResult = ValidatePassword(newPassword);
            if (!validationResult.IsValid)
            {
                // Log failed password change due to validation
                if (_auditLogService != null)
                {
                    await _auditLogService.LogActionAsync(
                        userId: user.Id,
                        userName: user.Name,
                        userEmail: user.Email,
                        action: AuditAction.PasswordChanged,
                        entityType: EntityType.User,
                        entityId: user.Id,
                        entityName: user.Name,
                        description: "Failed password change - validation error",
                        result: ActionResult.ValidationError,
                        errorMessage: string.Join(", ", validationResult.Errors),
                        cancellationToken: cancellationToken);
                }

                return PasswordChangeResult.Failed(string.Join(" ", validationResult.Errors));
            }

            // Update password
            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Log successful password change
            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    userId: user.Id,
                    userName: user.Name,
                    userEmail: user.Email,
                    action: AuditAction.PasswordChanged,
                    entityType: EntityType.User,
                    entityId: user.Id,
                    entityName: user.Name,
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
