using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Admin.Configuration;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Admin.Authentication;

/// <summary>
/// Auth0-specific implementation of identity provider synchronization service
/// Handles syncing users between Auth0 and local database via repository
/// </summary>
public class Auth0SyncService : IIdentityProviderSyncService
{
    private readonly Auth0Settings _auth0Settings;
    private readonly IAdminUserRepository _userRepository;
    private readonly ILogger<Auth0SyncService> _logger;

    public Auth0SyncService(
        IOptions<Auth0Settings> auth0Settings,
        IAdminUserRepository userRepository,
        ILogger<Auth0SyncService> logger)
    {
        _auth0Settings = auth0Settings?.Value ?? throw new ArgumentNullException(nameof(auth0Settings));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<AdminUser?> SyncUserAsync(
        string externalUserId,
        string email,
        string name,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Syncing user from Auth0. ExternalUserId: {ExternalUserId}, Email: {Email}", externalUserId, email);

        try
        {
            // Check if user exists by external ID
            var existingUser = await _userRepository.GetByExternalIdAsync(externalUserId, cancellationToken);

            if (existingUser != null)
            {
                // Update existing user
                existingUser.Email = email.Trim().ToLowerInvariant();
                existingUser.DisplayName = name?.Trim() ?? email;
                existingUser.ExternalUserLastSync = DateTimeOffset.UtcNow;
                existingUser.UpdatedAt = DateTimeOffset.UtcNow;
                existingUser.LastLoginAt = DateTimeOffset.UtcNow;

                await _userRepository.UpdateAsync(existingUser, cancellationToken);
                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully synced existing user {UserId} from Auth0", existingUser.Id);
                return existingUser;
            }

            // Check if user exists by email (migration scenario)
            var userByEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (userByEmail != null)
            {
                // Link existing local user to Auth0
                userByEmail.ExternalUserId = externalUserId;
                userByEmail.ExternalUserLastSync = DateTimeOffset.UtcNow;
                userByEmail.UpdatedAt = DateTimeOffset.UtcNow;
                userByEmail.LastLoginAt = DateTimeOffset.UtcNow;

                await _userRepository.UpdateAsync(userByEmail, cancellationToken);
                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Linked existing user {UserId} to Auth0", userByEmail.Id);
                return userByEmail;
            }

            // Create new user (first-time Auth0 login)
            // Check if this is the first user in the system
            var isFirstUser = !(await _userRepository.AnyAsync(cancellationToken));

            var newUser = new AdminUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email.Trim().ToLowerInvariant(),
                DisplayName = name?.Trim() ?? email,
                ExternalUserId = externalUserId,
                ExternalUserLastSync = DateTimeOffset.UtcNow,
                CustomClaims = isFirstUser 
                    ? UserClaims.AllClaims.ToList()
                    : [], // First user gets full permissions, others get none
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            };

            await _userRepository.AddAsync(newUser, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            if (isFirstUser)
            {
                _logger.LogWarning("Created FIRST ADMIN USER {UserId} ({Email}) with full permissions from Auth0 login", newUser.Id, newUser.Email);
            }
            else
            {
                _logger.LogInformation("Created new user {UserId} from Auth0 login with no permissions", newUser.Id);
            }

            return newUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync user from Auth0. ExternalUserId: {ExternalUserId}, Email: {Email}",
                externalUserId, email);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<(AdminUser? User, UserValidationResult ValidationResult)> CreateUserAsync(
        string email,
        string name,
        string password,
        IEnumerable<string> claims,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating user in Auth0. Email: {Email}", email);

            // Validate locally first
            var validationResult = await ValidateUserDataAsync(name, email, cancellationToken);
            if (!validationResult.IsValid)
            {
                return (null, validationResult);
            }

            // Get Management API client
            var managementClient = await GetManagementApiClientAsync();

            // Create user in Auth0
            var auth0User = new UserCreateRequest
            {
                Email = email.Trim().ToLowerInvariant(),
                FullName = name.Trim(),
                Password = password,
                Connection = "Username-Password-Authentication",
                EmailVerified = true // Auto-verify admin-created users
            };

            var createdAuth0User = await managementClient.Users.CreateAsync(auth0User, cancellationToken);

            _logger.LogInformation("Created user in Auth0. Auth0UserId: {Auth0UserId}, Email: {Email}", createdAuth0User.UserId, email);

            // Create local user record with permissions
            var localUser = new AdminUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email.Trim().ToLowerInvariant(),
                DisplayName = name.Trim(),
                ExternalUserId = createdAuth0User.UserId,
                ExternalUserLastSync = DateTimeOffset.UtcNow,
                CustomClaims = claims.ToList(),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _userRepository.AddAsync(localUser, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created local user {UserId} linked to Auth0", localUser.Id);
            return (localUser, UserValidationResult.Valid());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user in Auth0. Email: {Email}", email);
            return (null, UserValidationResult.Invalid($"Failed to create user in Auth0: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async Task<UserValidationResult> ResetPasswordAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initiating password reset for user {UserId}", userId);

            // Get local user via repository
            var localUser = await _userRepository.GetByIdAsync(userId, "users", cancellationToken);

            if (localUser == null)
            {
                return UserValidationResult.Invalid("User not found.");
            }

            if (string.IsNullOrEmpty(localUser.ExternalUserId))
            {
                return UserValidationResult.Invalid("User is not linked to Auth0.");
            }

            // Get Management API client
            var managementClient = await GetManagementApiClientAsync();

            // Create password change ticket
            var ticket = new PasswordChangeTicketRequest
            {
                UserId = localUser.ExternalUserId,
                ResultUrl = $"{_auth0Settings.ManagementApiUrl}/admin/login",
                MarkEmailAsVerified = true
            };

            var createdTicket = await managementClient.Tickets.CreatePasswordChangeTicketAsync(ticket, cancellationToken);

            _logger.LogInformation("Password reset email sent for user {UserId}. Ticket: {TicketUrl}", userId, createdTicket.Value);

            return UserValidationResult.Valid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            return UserValidationResult.Invalid($"Failed to reset password: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting user {UserId} from Auth0", userId);

            // Get local user via repository
            var localUser = await _userRepository.GetByIdAsync(userId, "users", cancellationToken);

            if (localUser == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            // Delete from Auth0 if linked
            if (!string.IsNullOrEmpty(localUser.ExternalUserId))
            {
                try
                {
                    var managementClient = await GetManagementApiClientAsync();
                    await managementClient.Users.DeleteAsync(localUser.ExternalUserId);
                    _logger.LogInformation("Deleted user from Auth0. Auth0UserId: {Auth0UserId}", localUser.ExternalUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete user from Auth0 (may already be deleted). Auth0UserId: {Auth0UserId}", localUser.ExternalUserId);
                    // Continue with local deletion even if Auth0 delete fails
                }
            }

            // Delete locally via repository
            await _userRepository.DeleteAsync(userId, "users", softDelete: true, cancellationToken);

            _logger.LogInformation("Successfully deleted user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<AdminUser> GetOrCreateDefaultAdminAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting or creating default admin user in Auth0");

            const string defaultAdminEmail = "admin@viblog.local";
            const string defaultAdminPassword = "Admin123!@#"; // Strong password for initial setup

            // Check if default admin already exists locally via repository
            var existingAdmin = await _userRepository.GetByEmailAsync(defaultAdminEmail, cancellationToken);

            if (existingAdmin != null)
            {
                _logger.LogInformation("Default admin user already exists. UserId: {UserId}", existingAdmin.Id);
                return existingAdmin;
            }

            // Create in Auth0 first
            var managementClient = await GetManagementApiClientAsync();

            // Check if user exists in Auth0
            var existingAuth0Users = await managementClient.Users.GetUsersByEmailAsync(defaultAdminEmail, cancellationToken: cancellationToken);
            User? auth0User = null;

            if (existingAuth0Users != null && existingAuth0Users.Count > 0)
            {
                auth0User = existingAuth0Users[0];
                _logger.LogInformation("Found existing default admin in Auth0. Auth0UserId: {Auth0UserId}", auth0User.UserId);
            }
            else
            {
                // Create in Auth0
                var createRequest = new UserCreateRequest
                {
                    Email = defaultAdminEmail,
                    FullName = "Administrator",
                    Password = defaultAdminPassword,
                    Connection = "Username-Password-Authentication",
                    EmailVerified = true
                };

                auth0User = await managementClient.Users.CreateAsync(createRequest, cancellationToken);
                _logger.LogInformation("Created default admin in Auth0. Auth0UserId: {Auth0UserId}", auth0User.UserId);
            }

            // Create local user with all permissions
            var adminUser = new AdminUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = defaultAdminEmail,
                DisplayName = "Administrator",
                ExternalUserId = auth0User.UserId,
                ExternalUserLastSync = DateTimeOffset.UtcNow,
                CustomClaims = UserClaims.DefaultAdminClaims.ToList(),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _userRepository.AddAsync(adminUser, cancellationToken);

            _logger.LogWarning(
                "Default admin user created. Email: {Email}, Password: {Password} - CHANGE THIS PASSWORD IMMEDIATELY!",
                defaultAdminEmail, defaultAdminPassword);

            return adminUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default admin user");
            throw new InvalidOperationException("Failed to create default admin user in Auth0", ex);
        }
    }

    /// <summary>
    /// Get an authenticated Auth0 Management API client
    /// </summary>
    private async Task<ManagementApiClient> GetManagementApiClientAsync()
    {
        try
        {
            // Use client credentials flow to get Management API token
            var tokenClient = new Auth0.AuthenticationApi.AuthenticationApiClient(new Uri($"https://{_auth0Settings.Domain}"));

            var tokenRequest = new Auth0.AuthenticationApi.Models.ClientCredentialsTokenRequest
            {
                ClientId = _auth0Settings.ClientId,
                ClientSecret = _auth0Settings.ClientSecret,
                Audience = _auth0Settings.Audience
            };

            var tokenResponse = await tokenClient.GetTokenAsync(tokenRequest);

            // Create Management API client with token
            return new ManagementApiClient(tokenResponse.AccessToken, new Uri($"https://{_auth0Settings.Domain}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Auth0 Management API client");
            throw new InvalidOperationException("Failed to authenticate with Auth0 Management API", ex);
        }
    }

    /// <summary>
    /// Validate user data before creation
    /// </summary>
    private async Task<UserValidationResult> ValidateUserDataAsync(
        string name,
        string email,
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
                errors.Add("Email address is already in use.");
            }
        }

        return errors.Count > 0
            ? UserValidationResult.Invalid(errors)
            : UserValidationResult.Valid();
    }

    /// <summary>
    /// Validate email format
    /// </summary>
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
