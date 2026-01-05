using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Vilog.Admin.Configuration;

namespace Vilog.Admin.Services;

/// <summary>
/// Authentication state provider for admin area using hardcoded credentials
/// This is a temporary solution that will be replaced with an external authentication service
/// </summary>
public class AdminAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly AdminAuthenticationSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminAuthenticationStateProvider(
        AdminAuthenticationSettings settings,
        IHttpContextAccessor httpContextAccessor,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _settings = settings;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Validate credentials against hardcoded values
    /// </summary>
    public bool ValidateCredentials(string email, string password)
    {
        return email == _settings.AdminEmail && password == _settings.AdminPassword;
    }

    /// <summary>
    /// Create authenticated user with optional persistent cookie
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="isPersistent">Whether to create a persistent cookie that survives browser restart</param>
    public async Task MarkUserAsAuthenticated(string email, bool isPersistent = false)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "Admin")
        }, AdminAuthenticationSettings.AuthenticationScheme);

        var user = new ClaimsPrincipal(identity);

        // Sign in with cookie authentication
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
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
                AdminAuthenticationSettings.AuthenticationScheme,
                user,
                authProperties);
        }
    }

    /// <summary>
    /// Sign out the user
    /// </summary>
    public async Task MarkUserAsLoggedOut()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync(AdminAuthenticationSettings.AuthenticationScheme);
        }
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        // Return true to keep the authentication state valid
        // You can add custom logic here to revalidate the user
        return Task.FromResult(authenticationState.User.Identity?.IsAuthenticated ?? false);
    }
}
