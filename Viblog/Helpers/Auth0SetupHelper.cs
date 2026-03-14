using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Text;
using Viblog.Admin.Authentication;
using Viblog.Admin.Configuration;

namespace Viblog.Helpers;

internal static class Auth0SetupHelper
{
    public static void SetupAuth0(IServiceCollection services)
    {
        // Register Auth0 authentication state provider

        // Configure Auth0 authentication
        var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        var auth0Settings = config.GetSection(Auth0Settings.SectionName).Get<Auth0Settings>();

        if (auth0Settings?.IsValid() == true)
        {
            services
                .AddAuthentication(ConfigureSchemes)
                .AddCookie(ConfigureCookieSettings)
                .AddOpenIdConnect(o => ConfigureOpenIdConnect(o, auth0Settings));
        }
    }

    private static void ConfigureOpenIdConnect(OpenIdConnectOptions options, Auth0Settings auth0Settings)
    {
        options.Authority = $"https://{auth0Settings.Domain}";
        options.ClientId = auth0Settings.ClientId;
        options.ClientSecret = auth0Settings.ClientSecret;
        options.ResponseType = "code";
        options.CallbackPath = auth0Settings.CallbackPath;

        // Configure sign-out paths
        options.SignedOutCallbackPath = "/viblog/signout-callback";
        options.SignedOutRedirectUri = auth0Settings.LogoutRedirectUri;

        // Configure cookies so that we can handle state on Azure App Service
        options.NonceCookie.SameSite = SameSiteMode.None;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

        // Request scopes
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Save tokens for API calls
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        // Clear default claim actions to prevent NullReferenceException 
        // We handle all claim transformation in OnTokenValidated via TransformAuth0ClaimsAsync
        options.ClaimActions.Clear();

        // Map claims properly
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        // Transform claims after authentication
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                var stateProvider = context.HttpContext.RequestServices
                    .GetRequiredService<Auth0AuthenticationStateProvider>();

                var transformedPrincipal = await stateProvider.TransformAuth0ClaimsAsync(context.Principal!);
                context.Principal = transformedPrincipal;
            },
            OnRedirectToIdentityProviderForSignOut = context =>
            {
                // Customize the logout redirect URL sent to Auth0
                var logoutUri = $"https://{auth0Settings.Domain}/v2/logout?client_id={auth0Settings.ClientId}";

                var postLogoutUri = context.Properties.RedirectUri;
                if (!string.IsNullOrEmpty(postLogoutUri))
                {
                    if (postLogoutUri.StartsWith("/"))
                    {
                        // Convert relative URL to absolute
                        var request = context.Request;
                        postLogoutUri = $"{request.Scheme}://{request.Host}{postLogoutUri}";
                    }
                    logoutUri += $"&returnTo={Uri.EscapeDataString(postLogoutUri)}";
                }

                context.Response.Redirect(logoutUri);
                context.HandleResponse();

                return Task.CompletedTask;
            },
            OnAccessDenied = context =>
            {
                context.Response.Redirect("/viblog/access-denied");
                context.HandleResponse();
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                context.Response.Redirect("/viblog/login?error=authentication_failed");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    }

    private static void ConfigureCookieSettings(CookieAuthenticationOptions options)
    {
        options.LoginPath = "/viblog/login";
        options.AccessDeniedPath = "/viblog/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.Name = "Viblog.Admin.Auth";
    }

    private static void ConfigureSchemes(AuthenticationOptions options)
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    }
}
