using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Vilog.Admin.Configuration;
using Vilog.Admin.Facades;
using Vilog.Admin.Infrastructure;
using Vilog.Admin.Services;

namespace Vilog.Admin;

public static class RegisterAdminExtensions
{
    /// <summary>
    /// Adds admin services to the service collection
    /// </summary>
    extension(IServiceCollection collection)
    {
        public IServiceCollection AddVilogAdmin()
        {
            // Register admin authentication settings
            var adminSettings = new AdminAuthenticationSettings();
            collection.AddSingleton(adminSettings);

            // Register HTTP context accessor for cookie authentication
            collection.AddHttpContextAccessor();

            // Register admin authentication state provider
            collection.AddScoped<AdminAuthenticationStateProvider>();
            
            // Register admin facades
            collection.AddScoped<IPostsAdminFacade, PostsAdminFacade>();
            
            // Register admin services
            collection.AddScoped<IMessageService, MessageService>();
            collection.AddScoped<IDialogService, DialogService>();
            
            // Add Telerik UI for Blazor services
            collection.AddTelerikBlazor();

            collection.AddCascadingAuthenticationState();
            collection.AddScoped<AuthenticationStateProvider, AdminAuthenticationStateProvider>();

            // Configure cookie authentication with custom admin login path
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
                    options.Cookie.Name = "Vilog.Admin.Auth";
                });

            return collection;
        }
    }

    /// <summary>
    /// Adds admin authentication middleware to the application pipeline
    /// </summary>
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseVilogAdmin()
        {
            app.UseAuthentication();
            app.UseAuthorization();
            
            return app;
        }
    }

    /// <summary>
    /// Maps admin endpoints (login, logout)
    /// </summary>
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapVilogAdminEndpoints()
        {
            // Map admin login endpoint
            endpoints.MapPost("/admin/api/login", async (HttpContext context, AdminAuthenticationStateProvider authProvider) =>
            {
                var form = await context.Request.ReadFormAsync();
                var email = form["email"].ToString();
                var password = form["password"].ToString();
                var rememberMe = form["rememberMe"].ToString() == "on";
                
                if (authProvider.ValidateCredentials(email, password))
                {
                    await authProvider.MarkUserAsAuthenticated(email, rememberMe);
                    
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
            .AllowAnonymous(); // Allow anonymous access to the login endpoint

            // Map admin logout endpoint
            endpoints.MapPost("/admin/api/logout", async (HttpContext context, AdminAuthenticationStateProvider authProvider) =>
            {
                await authProvider.MarkUserAsLoggedOut();
                context.Response.Redirect("/admin/login");
            })
            .RequireAuthorization(); // Require authentication for logout

            return endpoints;
        }
    }
}
