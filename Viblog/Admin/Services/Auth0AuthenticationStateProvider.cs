using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Admin.Services;

/// <summary>
/// Authentication state provider for Auth0 integration
/// Handles claims transformation and user synchronization
/// </summary>
public class Auth0AuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IIdentityProviderSyncService _syncService;
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<Auth0AuthenticationStateProvider> _logger;

    public Auth0AuthenticationStateProvider(
        IIdentityProviderSyncService syncService,
        IUserManagementService userManagementService,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
        _logger = loggerFactory?.CreateLogger<Auth0AuthenticationStateProvider>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Revalidation interval - how often to check if user is still valid
    /// </summary>
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    /// <summary>
    /// Validate that the authenticated user is still valid
    /// Checks if user still exists and is active
    /// </summary>
    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = authenticationState.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            // Get user ID from claims
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Authenticated user has no NameIdentifier claim");
                return false;
            }

            // Check if user still exists and is active
            var adminUser = await _userManagementService.GetUserByIdAsync(userId, cancellationToken);
            if (adminUser == null || !adminUser.IsActive || adminUser.IsDeleted)
            {
                _logger.LogWarning("User {UserId} is no longer active or was deleted", userId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating authentication state");
            return false;
        }
    }

    /// <summary>
    /// Transform Auth0 claims into application-specific claims
    /// Called after successful Auth0 authentication
    /// </summary>
    public async Task<ClaimsPrincipal> TransformAuth0ClaimsAsync(ClaimsPrincipal auth0Principal)
    {
        try
        {
            if (auth0Principal?.Identity?.IsAuthenticated != true)
            {
                return auth0Principal ?? new ClaimsPrincipal();
            }

            // Extract Auth0 claims
            var externalUserId = auth0Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? auth0Principal.FindFirst("sub")?.Value;
            var email = auth0Principal.FindFirst(ClaimTypes.Email)?.Value
                ?? auth0Principal.FindFirst("email")?.Value;
            var name = auth0Principal.FindFirst(ClaimTypes.Name)?.Value
                ?? auth0Principal.FindFirst("name")?.Value
                ?? email;

            if (string.IsNullOrEmpty(externalUserId) || string.IsNullOrEmpty(email))
            {
                _logger.LogError("Auth0 claims missing required values. ExternalUserId: {ExternalUserId}, Email: {Email}",
                    externalUserId, email);
                return new ClaimsPrincipal();
            }

            // Sync user from Auth0 to local database
            var localUser = await _syncService.SyncUserAsync(externalUserId, email, name ?? email);
            if (localUser == null)
            {
                _logger.LogError("Failed to sync user from Auth0. Email: {Email}", email);
                return new ClaimsPrincipal();
            }

            // Build claims with local permissions
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, localUser.Id),
                new(ClaimTypes.Name, localUser.DisplayName),
                new(ClaimTypes.Email, localUser.Email),
                new(ClaimTypes.Role, "Admin"),
                new("external_user_id", externalUserId)
            };

            // Add custom permission claims from local database
            foreach (var customClaim in localUser.CustomClaims)
            {
                claims.Add(new Claim("permission", customClaim));
            }

            var identity = new ClaimsIdentity(claims, auth0Principal.Identity.AuthenticationType);
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming Auth0 claims");
            return new ClaimsPrincipal();
        }
    }
}
