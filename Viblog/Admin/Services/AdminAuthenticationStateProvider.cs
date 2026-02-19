using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Admin.Services;

/// <summary>
/// Authentication state provider for admin area
/// TODO: Will be updated for Auth0 in Step 9
/// Currently stubbed to allow compilation
/// </summary>
public class AdminAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IUserManagementService _userManagementService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminAuthenticationStateProvider> _logger;

    private const string AuthenticationScheme = "AdminAuthenticationScheme";

    public AdminAuthenticationStateProvider(
        IUserManagementService userManagementService,
        IHttpContextAccessor httpContextAccessor,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = loggerFactory?.CreateLogger<AdminAuthenticationStateProvider>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Create authenticated user with optional persistent cookie
    /// TODO: Remove this method in Step 9 - Auth0 will handle authentication
    /// </summary>
    public async Task MarkUserAsAuthenticatedAsync(AdminUser user, bool isPersistent = false)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Role, "Admin")
        };

        // Add user-specific claims
        foreach (var userClaim in user.CustomClaims)
        {
            claims.Add(new Claim("permission", userClaim));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Sign in with cookie authentication
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent 
                    ? DateTimeOffset.UtcNow.AddDays(30) 
                    : DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            await httpContext.SignInAsync(
                AuthenticationScheme,
                principal,
                authProperties);

            _logger.LogInformation("User {Email} signed in successfully", user.Email);
        }
    }

    /// <summary>
    /// Sign out the user
    /// TODO: Update for Auth0 in Step 9
    /// </summary>
    public async Task MarkUserAsLoggedOutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await httpContext.SignOutAsync(AuthenticationScheme);
            _logger.LogInformation("User signed out");
        }
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        var user = authenticationState.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Get user ID from claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
        {
            return false;
        }

        // Verify user still exists and is active
        var dbUser = await _userManagementService.GetUserByIdAsync(userIdClaim.Value, cancellationToken);

        return dbUser is not null && dbUser.IsActive && !dbUser.IsDeleted;
    }
}
