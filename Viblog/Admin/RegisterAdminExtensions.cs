using Microsoft.AspNetCore.Components.Authorization;
using Viblog.Admin.Configuration;
using Viblog.Admin.Facades;
using Viblog.Admin.Services;
using Viblog.Admin.Services.Auditing;
using Viblog.Admin.Services.Authentication;
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

            // Register admin authentication state provider
            collection.AddScoped<AdminAuthenticationStateProvider>();

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

            // Add Telerik UI for Blazor services
            collection.AddTelerikBlazor();

            collection.AddCascadingAuthenticationState();
            collection.AddScoped<AuthenticationStateProvider, AdminAuthenticationStateProvider>();

            // TODO: Authentication middleware will be configured in Step 9 (Auth0 OpenID Connect)
            // Temporarily comment out to allow compilation
            /*
            collection.AddAuthentication(AdminAuthenticationSettings.AuthenticationScheme)
                .AddCookie(AdminAuthenticationSettings.AuthenticationScheme, options =>
                {
                    options.LoginPath = AdminAuthenticationSettings.LoginPath;
                    options.AccessDeniedPath = AdminAuthenticationSettings.AccessDeniedPath;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.Name = "Viblog.Admin.Auth";
                });
            */

            // Configure authorization policies based on claims
            collection.AddAuthorizationBuilder()
                .AddPolicy(AdminPolicies.Admin, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    // policy.AuthenticationSchemes.Add(AdminAuthenticationSettings.AuthenticationScheme);
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
    /// Adds admin authentication middleware (does not initialize default admin - call InitializeViblogAdminAsync separately)
    /// </summary>
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseViblogAdmin()
        {
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }

    /// <summary>
    /// Initialize admin system asynchronously (creates default admin user if needed)
    /// </summary>
    extension(WebApplication app)
    {
        public async Task InitializeViblogAdminAsync()
        {
            using var scope = app.Services.CreateScope();
            var userManagementService = scope.ServiceProvider.GetService<IUserManagementService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IUserManagementService>>();

            if (userManagementService == null)
            {
                logger.LogWarning("IUserManagementService not registered. Skipping default admin user initialization.");
                return;
            }

            try
            {
                logger.LogInformation("Checking if default admin user initialization is needed...");

                var usersExist = await userManagementService.AnyUsersExistAsync();

                if (!usersExist)
                {
                    logger.LogInformation("No users found. Creating default admin user...");
                    var defaultAdmin = await userManagementService.CreateDefaultAdminUserAsync();
                    logger.LogWarning("Default admin user created: {Email} with password 'admin123!' - CHANGE THIS PASSWORD IMMEDIATELY!", defaultAdmin.Email);
                }
                else
                {
                    logger.LogInformation("Users already exist. Skipping default admin creation.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during default admin user initialization");
            }
        }
    }

    /// <summary>
    /// Maps admin endpoints (login, logout)
    /// TODO: These endpoints will be replaced with Auth0 endpoints in Step 10
    /// </summary>
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapViblogAdminEndpoints()
        {
            // TODO: Auth0 endpoints will be added in Step 10
            // Old login/logout endpoints removed

            /* REMOVED - Will be replaced with Auth0 endpoints
            // Map admin login endpoint
            endpoints.MapPost("/admin/api/login", async (HttpContext context, AdminAuthenticationStateProvider authProvider) =>
            {
                var form = await context.Request.ReadFormAsync();
                var email = form["email"].ToString();
                var password = form["password"].ToString();
                var rememberMe = form["rememberMe"].ToString() == "on";

                var result = await authProvider.ValidateCredentialsAsync(email, password);
                if (result.Success && result.User is not null)
                {
                    await authProvider.MarkUserAsAuthenticatedAsync(result.User, rememberMe);

                    // Check if there's a return URL, otherwise redirect to admin dashboard
                    var returnUrl = context.Request.Query["ReturnUrl"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
                    {
                        context.Response.Redirect(returnUrl);
                    }
                    else
                    {
                        context.Response.Redirect("/admin");
                    }
                }
                else
                {
                    // Redirect back to login with error
                    context.Response.Redirect("/admin/login?error=invalid");
                }
            })
            .AllowAnonymous();

            // Map admin logout endpoint
            endpoints.MapPost("/admin/api/logout", async (HttpContext context, AdminAuthenticationStateProvider authProvider) =>
            {
                await authProvider.MarkUserAsLoggedOutAsync();
                context.Response.Redirect("/admin/login");
            })
            .RequireAuthorization();
            */
            return endpoints;
        }
    }
}
