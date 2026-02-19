using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Admin.Configuration;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Admin.Services.Authentication;

/// <summary>
/// Auth0-specific implementation of identity provider synchronization service
/// STUB IMPLEMENTATION: Will be fully implemented in Step 11
/// Handles syncing users between Auth0 and local database
/// </summary>
public class Auth0SyncService : IIdentityProviderSyncService
{
    private readonly Auth0Settings _auth0Settings;
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<Auth0SyncService> _logger;

    public Auth0SyncService(
        IOptions<Auth0Settings> auth0Settings,
        IUserManagementService userManagementService,
        ILogger<Auth0SyncService> logger)
    {
        _auth0Settings = auth0Settings?.Value ?? throw new ArgumentNullException(nameof(auth0Settings));
        _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<AdminUser?> SyncUserAsync(
        string externalUserId,
        string email,
        string name,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SyncUserAsync called but not yet implemented. ExternalUserId: {ExternalUserId}", externalUserId);
        throw new NotImplementedException("User sync will be implemented in Step 11");
    }

    /// <inheritdoc/>
    public Task<(AdminUser? User, UserValidationResult ValidationResult)> CreateUserAsync(
        string email,
        string name,
        string password,
        IEnumerable<string> claims,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("CreateUserAsync called but not yet implemented. Email: {Email}", email);
        throw new NotImplementedException("User creation in identity provider will be implemented in Step 11");
    }

    /// <inheritdoc/>
    public Task<UserValidationResult> ResetPasswordAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ResetPasswordAsync called but not yet implemented. UserId: {UserId}", userId);
        throw new NotImplementedException("Password reset in identity provider will be implemented in Step 11");
    }

    /// <inheritdoc/>
    public Task<bool> DeleteUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("DeleteUserAsync called but not yet implemented. UserId: {UserId}", userId);
        throw new NotImplementedException("User deletion in identity provider will be implemented in Step 11");
    }

    /// <inheritdoc/>
    public Task<AdminUser> GetOrCreateDefaultAdminAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("GetOrCreateDefaultAdminAsync called but not yet implemented");
        throw new NotImplementedException("Default admin creation in identity provider will be implemented in Step 11");
    }
}
