using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Viblog.Infrastructure.Data.Repositories;
using Viblog.Shared.Data.Sources.CosmosDb.Data;
using Viblog.Shared.Data.Sources.CosmosDb.Data.Repositories;

namespace Viblog.Shared.Data.Sources.CosmosDb;

/// <summary>
/// Extension methods for registering CosmosDB data access services
/// </summary>
public static class CosmosDbServiceExtensions
{
    /// <summary>
    /// Register CosmosDB-specific repository implementations
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCosmosDbRepositories(this IServiceCollection services)
    {
        // Register the generic repository
        services.AddScoped(typeof(IRepository<>), typeof(CosmosDbRepository<>));

        // Register specific repositories
        services.AddScoped<IAdminUserRepository, CosmosDbAdminUserRepository>();
        services.AddScoped<IAuditLogRepository, CosmosDbAuditLogRepository>();
        services.AddScoped<IBlogPostRepository, CosmosDbBlogPostRepository>();
        services.AddScoped<IMediaMetadataRepository, CosmosDbMediaMetadataRepository>();
        services.AddScoped<IPageRepository, CosmosDbPageRepository>();

        // Register version history repositories
        services.AddScoped<IBlogPostVersionRepository, BlogPostVersionRepository>();
        services.AddScoped<IPageVersionRepository, PageVersionRepository>();

        return services;
    }

    /// <summary>
    /// Add all CosmosDB services (DbContext + Repositories)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="isDevelopment">Whether the application is running in development mode</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCosmosDbDataAccess(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        services.AddCosmosDbRepositories();

        return services;
    }

    /// <summary>
    /// Ensure the CosmosDB database and containers are created
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving dependencies</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task EnsureCosmosDbCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetService<Microsoft.Extensions.Logging.ILogger<ApplicationDbContext>>();

        try
        {
            logger?.LogInformation("Ensuring CosmosDB database and containers are created...");
            await dbContext.Database.EnsureCreatedAsync();

            var cosmosClient = dbContext.Database.GetCosmosClient();
            var databaseId = dbContext.Database.GetCosmosDatabaseId();
            var database = cosmosClient.GetDatabase(databaseId);

            var containers = new[]
            {
                "Users",
                "BlogPosts",
                "Pages",
                "BlogPostVersions",
                "PageVersions",
                "MediaItems",
                "AuditLogs"
            };

            foreach (var containerName in containers)
            {
                await database.CreateContainerIfNotExistsAsync(new ContainerProperties(containerName, "/GroupKey"));
            }

            logger?.LogInformation("CosmosDB database and containers are ready.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while ensuring the database was created.");
            throw;
        }
    }
}