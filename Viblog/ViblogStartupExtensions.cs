using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Viblog.Admin;
using Viblog.Admin.Authentication;
using Viblog.Admin.Configuration;
using Viblog.Admin.Facades;
using Viblog.Admin.Services;
using Viblog.Admin.Services.Auditing;
using Viblog.Admin.Services.Dialogs;
using Viblog.Admin.Services.Messaging;
using Viblog.Admin.Workers;
using Viblog.Api.Endpoints;
using Viblog.Components;
using Viblog.Helpers;
using Viblog.Infrastructure.Auditing;
using Viblog.Infrastructure.Authentication;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Facades;
using Viblog.Infrastructure.Services;
using Viblog.Shared;
using Viblog.Shared.Configuration;
using Viblog.Shared.Data.Seeders;
using Microsoft.Extensions.Options;
using Viblog.Shared.Data.Sources.AzureStorage;
using Viblog.Shared.Data.Sources.CosmosDb;
using Viblog.Shared.Data.Sources.CosmosDb.Data;
using Viblog.Shared.Facades;
using Viblog.Shared.Services;
using Viblog.Shared.Services.Content;

namespace Viblog;

/// <summary>
/// Extension methods for adding and configuring Viblog in a web application.
/// </summary>
public static class ViblogStartupExtensions
{
    extension(IServiceCollection collection)
    {
        /// <summary>
        /// Registers all Viblog services with the dependency injection container.
        /// </summary>
        public IServiceCollection AddViblogServices()
        {
            var configuration = collection.BuildServiceProvider().GetRequiredService<IConfiguration>();

            collection.AddHttpContextAccessor();

            collection.AddViblogConfiguration(configuration);

            // Make sure that we have access to server logic
            collection.AddRazorComponents().AddInteractiveServerComponents();

            // Data storage
            collection.AddCosmosDbRepositories();
            collection.AddDatabaseDeveloperPageExceptionFilter();
            collection.AddAzureBlobStorageRepository();

            // Register text utilities
            collection.AddScoped<ITextUtilities, TextUtilities>();

            // Register markdown service
            collection.AddScoped<IMarkdownService, MarkdownService>();

            // Register media services
            collection.AddScoped<IMediaService, MediaService>();
            collection.AddScoped<IMetadataExtractorService, MetadataExtractorService>();
            collection.AddScoped<IMediaFacade, MediaFacade>();

            // Shared services between admin and frontend
            // Register search service
            collection.AddScoped<IBlogSearchService, BlogSearchService>();

            AddFacades(collection);
            AddServices(collection);

            AddWorkers(collection);

            AddAuthentication(collection);
            AddAuthorizationPolicies(collection);
            Auth0SetupHelper.SetupAuth0(collection);

            return collection;
        }

        /// <summary>
        /// Registers facade services for UI operations with the dependency injection container.
        /// </summary>
        /// <remarks>This method configures the dependency injection container to provide scoped instances
        /// of the facades used for administrative and user interface operations. Each facade is registered with its
        /// corresponding interface, enabling consumers to request the abstractions rather than concrete
        /// implementations.</remarks>
        /// <param name="services">The service collection to which the facade services will be added. Cannot be null.</param>
        private static void AddFacades(IServiceCollection services)
        {
            // Add facades for UI operations
            services.AddScoped<IPostsAdminFacade, PostsAdminFacade>();
            services.AddScoped<IDashboardFacade, DashboardFacade>();
            services.AddScoped<IPagesAdminFacade, PagesAdminFacade>();
            services.AddScoped<IUserManagementFacade, UserManagementFacade>();
            services.AddScoped<IUserProfileFacade, UserProfileFacade>();
            services.AddScoped<IAuditLogFacade, AuditLogFacade>();
        }

        /// <summary>
        /// Registers application services required for administration, user management, audit logging, media
        /// broadcasting, and content scheduling with the dependency injection container.
        /// </summary>
        /// <remarks>This method configures the dependency injection container with scoped and singleton
        /// services used throughout the application, ensuring that required services are available for runtime
        /// operations.</remarks>
        /// <param name="services">The service collection to which application services will be added. Must not be null.</param>
        private static void AddServices(IServiceCollection services)
        {
            // Register admin services
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IDialogService, DialogService>();

            // Register user management service
            services.AddScoped<IUserManagementService, UserManagementService>();

            // Register audit logging service
            services.AddScoped<IAuditLogService, AuditLogService>();

            // Register media library broadcast service (singleton for cross-user notifications)
            services.AddSingleton<IMediaLibraryBroadcastService, InMemoryMediaLibraryBroadcastService>();

            // Register content scheduling services
            services.AddScoped<ContentSchedulingService>();
            services.AddScoped<ContentVersionService>();
            services.AddScoped<ContentProcessingService>();

            // Register data seeders
            services.AddScoped<BlogPostSeeder>();
        }

        /// <summary>
        /// Registers background workers for Viblog
        /// </summary>
        /// <param name="services"></param>
        private static void AddWorkers(IServiceCollection services)
        {
            // Register content publishing options
            services.Configure<ContentPublishingOptions>(
                services.BuildServiceProvider()
                    .GetRequiredService<IConfiguration>()
                    .GetSection("ContentPublishing"));

            // Register content publishing background service
            services.AddHostedService<ContentPublishingBackgroundService>();
        }

        private static void AddAuthentication(IServiceCollection services)
        {
            // Register Auth0 configuration
            services.Configure<Auth0Settings>(services.BuildServiceProvider()
                .GetRequiredService<IConfiguration>()
                .GetSection(Auth0Settings.SectionName));

            // Register identity provider sync service (Auth0 implementation)
            services.AddScoped<IIdentityProviderSyncService, Auth0SyncService>();

            services.AddCascadingAuthenticationState();
            services.AddScoped<AuthenticationStateProvider, Auth0AuthenticationStateProvider>();
            services.AddScoped<Auth0AuthenticationStateProvider>();

        }

        private static void AddAuthorizationPolicies(IServiceCollection services)
        {
            // Configure authorization policies based on claims
            services.AddAuthorizationBuilder()
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
        }
    }

    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Configures builder-level Viblog infrastructure, including <see cref="AddViblogServices"/>
        /// registrations and Aspire CosmosDB binding.
        /// Do not call <see cref="AddViblogServices"/> separately before this method.
        /// </summary>
        /// <param name="configure">Optional callback to configure host-level Viblog options.</param>
        public WebApplicationBuilder AddViblog(Action<ViblogOptions>? configure = null)
        {
            var options = new ViblogOptions();
            configure?.Invoke(options);
            builder.Services.AddSingleton(Options.Create(options));

            builder.Services.AddViblogServices();
            builder.AddCosmosDbContext<ApplicationDbContext>("aspireCosmosDatabase");
            return builder;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Adds the Viblog middleware pipeline: authentication, authorization, static assets,
        /// admin endpoints, and Razor component routing.
        /// </summary>
        public async Task UseViblogAsync()
        {
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always,
            });

            // Since appservice uses http internally and a https proxy, we need to forward the headers
            // Necessary because of auth0 missing cookie.
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            await app.Services.EnsureCosmosDbCreatedAsync();

            using var scope = app.Services.CreateScope();
            var syncService = scope.ServiceProvider.GetService<IIdentityProviderSyncService>();
            var userManagementService = scope.ServiceProvider.GetService<IUserManagementService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IUserManagementService>>();

            if (syncService == null || userManagementService == null)
            {
                logger.LogWarning("Identity provider sync service or user management service not registered. Skipping default admin user initialization.");
                return;
            }

            app.MapMediaServeEndpoints();
            if (app.Environment.IsDevelopment())
            {
                await SeedDatabaseAsync(app);
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }

            var mediaBasePath = app.Configuration["MediaStorage:FileSystem:BasePath"];
            if (!string.IsNullOrEmpty(mediaBasePath))
            {
                var absoluteMediaPath = Path.IsPathRooted(mediaBasePath)
                    ? mediaBasePath
                    : Path.GetFullPath(mediaBasePath);

                if (Directory.Exists(absoluteMediaPath))
                {
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(absoluteMediaPath),
                        RequestPath = "/media"
                    });
                }
                else
                {
                    app.Logger.LogWarning(
                        "Media storage path does not exist: {Path}. Static file serving for media will not be configured.",
                        absoluteMediaPath);
                }
            }

            app.UseAuthentication();
            app.UseAuthorization();

            MapMediaEndpoints(app);
        }
    }

    private static void MapMediaEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/callback", async context =>
        {
            // Let the OIDC middleware handle it - just needs to be a reachable route
            await context.Response.WriteAsync("Processing...");
        }).AllowAnonymous();

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
    }

    private static async Task SeedDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<BlogPostSeeder>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(ViblogStartupExtensions));

        try
        {
            logger.LogInformation("Checking if database seeding is needed...");
            await seeder.SeedAsync();
            logger.LogInformation("Database seeding check completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database seeding");
        }
    }
}
