using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Viblog.Admin.Authentication;
using Viblog.Admin.Configuration;
using Viblog.Admin.Facades;
using Viblog.Admin.Services;
using Viblog.Admin.Services.Auditing;
using Viblog.Infrastructure.Admin.Facades;
using Viblog.Infrastructure.Admin.Services;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Admin;

public static class RegisterAdminExtensions
{
    /// <summary>
    /// Adds admin services to the service collection
    /// </summary>
    extension(IServiceCollection collection)
    {
        public IServiceCollection AddViblogAdmin()
        {
            // Register HTTP context accessor for authentication
            collection.AddHttpContextAccessor();

            // Register Auth0 configuration
            collection.Configure<Auth0Settings>(collection.BuildServiceProvider()
                .GetRequiredService<IConfiguration>()
                .GetSection(Auth0Settings.SectionName));

            // Register identity provider sync service (Auth0 implementation)
            collection.AddScoped<IIdentityProviderSyncService, Auth0SyncService>();

            // Register Auth0 authentication state provider
            collection.AddScoped<Auth0AuthenticationStateProvider>();

            // Register admin facades
            collection.AddScoped<IPostsAdminFacade, PostsAdminFacade>();
            collection.AddScoped<IPagesAdminFacade, PagesAdminFacade>();
            collection.AddScoped<IUserManagementFacade, UserManagementFacade>();
            collection.AddScoped<IUserProfileFacade, UserProfileFacade>();
            collection.AddScoped<IAuditLogFacade, AuditLogFacade>();

            // Register admin services
            collection.AddScoped<IMessageService, MessageService>();
            collection.AddScoped<IDialogService, DialogService>();

            // Register user management service
            collection.AddScoped<IUserManagementService, UserManagementService>();

            // Register audit logging service
            collection.AddScoped<IAuditLogService, AuditLogService>();

            // Register media library broadcast service (singleton for cross-user notifications)
            collection.AddSingleton<IMediaLibraryBroadcastService, InMemoryMediaLibraryBroadcastService>();

            // Register content scheduling services
            collection.AddScoped<Viblog.Shared.Services.Content.ContentSchedulingService>();
            collection.AddScoped<Viblog.Shared.Services.Content.ContentVersionService>();
            collection.AddScoped<Viblog.Shared.Services.Content.ContentProcessingService>();

            // Register data seeders
            collection.AddScoped<Viblog.Shared.Data.Seeders.BlogPostSeeder>();

            // Register content publishing background service
            collection.AddHostedService<Viblog.Admin.Workers.ContentPublishingBackgroundService>();

            // Register content publishing options
            collection.Configure<Viblog.Admin.Workers.ContentPublishingOptions>(
                collection.BuildServiceProvider()
                    .GetRequiredService<IConfiguration>()
                    .GetSection("ContentPublishing"));

            // Add Telerik UI for Blazor services
            collection.AddTelerikBlazor();

            collection.AddCascadingAuthenticationState();
            collection.AddScoped<AuthenticationStateProvider, Auth0AuthenticationStateProvider>();

            // Configure Auth0 authentication
            var config = collection.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var auth0Settings = config.GetSection(Auth0Settings.SectionName).Get<Auth0Settings>();

            if (auth0Settings?.IsValid() == true)
            {
                collection.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.LoginPath = "/viblog/login";
                    options.AccessDeniedPath = "/viblog/access-denied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.Name = "Viblog.Admin.Auth";
                })
                .AddOpenIdConnect(options =>
                {
                    options.Authority = $"https://{auth0Settings.Domain}";
                    options.ClientId = auth0Settings.ClientId;
                    options.ClientSecret = auth0Settings.ClientSecret;
                    options.ResponseType = "code";
                    options.CallbackPath = auth0Settings.CallbackPath;

                    // Configure sign-out paths
                    options.SignedOutCallbackPath = "/viblog/signout-callback";
                    options.SignedOutRedirectUri = auth0Settings.LogoutRedirectUri;

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
                        }
                    };
                });
            }
            else
            {
                // Auth0 not configured - add basic authentication for development
                collection.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.LoginPath = "/viblog/login";
                        options.AccessDeniedPath = "/viblog/access-denied";
                    });
            }

            // Configure authorization policies based on claims
            collection.AddAuthorizationBuilder()
                .AddPolicy(AdminPolicies.Admin, policy =>
                {
                    policy.RequireAuthenticatedUser();
                })
                .AddPolicy(AdminPolicies.RequirePostWrite, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("permission", UserClaims.PostWrite);
                })
                .AddPolicy(AdminPolicies.RequirePageWrite, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("permission", UserClaims.PageWrite);
                })
                .AddPolicy(AdminPolicies.RequireStatisticsRead, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("permission", UserClaims.StatisticsRead);
                })
                .AddPolicy(AdminPolicies.RequireUserRead, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("permission", UserClaims.UserRead);
                })
                .AddPolicy(AdminPolicies.RequireUserWrite, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("permission", UserClaims.UserWrite);
                });

            return collection;
        }
    }

    /// <summary>
    /// Initialize admin system asynchronously (creates default admin user if needed)
    /// Uses Auth0 for user creation
    /// </summary>
    extension(WebApplication app)
    {
        public async Task InitializeViblogAdminAsync()
        {
            using var scope = app.Services.CreateScope();
            var syncService = scope.ServiceProvider.GetService<IIdentityProviderSyncService>();
            var userManagementService = scope.ServiceProvider.GetService<IUserManagementService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IUserManagementService>>();

            if (syncService == null || userManagementService == null)
            {
                logger.LogWarning("Identity provider sync service or user management service not registered. Skipping default admin user initialization.");
                return;
            }
        }
    }

    /// <summary>
    /// Maps admin authentication endpoints (Auth0 integration)
    /// </summary>
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapViblogAdminEndpoints()
        {
            // Auth0 Challenge endpoint - initiates Auth0 login
            endpoints.MapGet("/viblog/auth/challenge", async (HttpContext context, string? returnUrl = null) =>
            {
                var properties = new AuthenticationProperties
                {
                    RedirectUri = returnUrl ?? "/viblog"
                };

                await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
            })
            .AllowAnonymous();

            // Auth0 Logout endpoint
            endpoints.MapGet("/viblog/logout", async (HttpContext context) =>
            {
                var properties = new AuthenticationProperties
                {
                    RedirectUri = "/"
                };

                // Sign out from both cookie and Auth0
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
            })
            .RequireAuthorization();

            // Access denied page
            endpoints.MapGet("/viblog/access-denied", (HttpContext context) =>
            {
                context.Response.Redirect("/viblog/login?error=access_denied");
                return Task.CompletedTask;
            })
            .AllowAnonymous();

            return endpoints;
        }
    }
}
