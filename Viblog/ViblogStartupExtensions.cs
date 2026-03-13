using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Viblog.Admin;
using Viblog.Components;
using Viblog.Shared;
using Viblog.Shared.Configuration;
using Viblog.Shared.Data.Seeders;
using Viblog.Shared.Data.Sources.AzureStorage;
using Viblog.Shared.Data.Sources.CosmosDb;
using Viblog.Shared.Data.Sources.CosmosDb.Data;

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

            collection.AddViblogConfiguration(configuration);
            collection.AddRazorComponents().AddInteractiveServerComponents();
            collection.AddCosmosDbContext(configuration, false);
            collection.AddCosmosDbRepositories();
            collection.AddDatabaseDeveloperPageExceptionFilter();
            collection.AddBlogServices();
            collection.AddAzureBlobStorageRepository();
            collection.AddViblogAdmin();

            return collection;
        }
    }

    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Configures builder-level Viblog infrastructure (Aspire CosmosDB binding, Blazor circuit options).
        /// Call after <see cref="AddViblogServices"/>.
        /// </summary>
        public WebApplicationBuilder UseViblog()
        {
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
            await app.InitializeViblogAdminAsync();

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

            app.MapViblogAdminEndpoints();
        }
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
