using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Viblog.Admin.Configuration;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Admin.Services;

/// <summary>
/// Authentication state provider for admin area using pluggable authentication provider
/// </summary>
public class AdminAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IAuthenticationProvider _authenticationProvider;
    private readonly IUserRepository _userRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminAuthenticationStateProvider> _logger;

    public AdminAuthenticationStateProvider(
        IAuthenticationProvider authenticationProvider,
        IUserRepository userRepository,
        IHttpContextAccessor httpContextAccessor,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _authenticationProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = loggerFactory?.CreateLogger<AdminAuthenticationStateProvider>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Validate credentials using the authentication provider
    /// </summary>
    public async Task<AuthenticationResult> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return await _authenticationProvider.AuthenticateAsync(email, password, cancellationToken);
    }

    /// <summary>
    /// Create authenticated user with optional persistent cookie
    /// </summary>
    /// <param name="user">The authenticated user</param>
    /// <param name="isPersistent">Whether to create a persistent cookie that survives browser restart</param>
    public async Task MarkUserAsAuthenticatedAsync(User user, bool isPersistent = false)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, "Admin")
        };

        // Add user-specific claims
        foreach (var userClaim in user.Claims)
        {
            claims.Add(new Claim("permission", userClaim));
        }

        var identity = new ClaimsIdentity(claims, AdminAuthenticationSettings.AuthenticationScheme);
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
                AdminAuthenticationSettings.AuthenticationScheme,
                principal,
                authProperties);

            _logger.LogInformation("User {Email} signed in successfully", user.Email);
        }
    }

    /// <summary>
    /// Sign out the user
    /// </summary>
    public async Task MarkUserAsLoggedOutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await httpContext.SignOutAsync(AdminAuthenticationSettings.AuthenticationScheme);
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
        var dbUser = await _userRepository.GetByIdAsync(userIdClaim.Value, "users", cancellationToken);

        return dbUser is not null && dbUser.IsActive;
    }
}
