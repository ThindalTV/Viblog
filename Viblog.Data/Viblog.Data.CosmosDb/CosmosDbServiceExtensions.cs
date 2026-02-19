using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Viblog.Data.CosmosDb.Data;
using Viblog.Data.CosmosDb.Data.Repositories;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Infrastructure.Shared.Data.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;

namespace Viblog.Data.CosmosDb;

/// <summary>
/// Extension methods for registering CosmosDB data access services
/// </summary>
public static class CosmosDbServiceExtensions
{
    /// <summary>
    /// Add CosmosDB database context with proper configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="isDevelopment">Whether the application is running in development mode</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCosmosDbContext(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {/*
        var cosmosConnectionString =
            configuration.GetConnectionString("aspireCosmosDatabase") // Aspire
            //?? configuration.GetConnectionString("CosmosConnection")
            ?? throw new InvalidOperationException("Connection string 'CosmosConnection' not found.");
        var cosmosDatabaseName = "aspireCosmosDatabase"; configuration.GetConnectionString("cosmosStorage")
            //?? configuration["CosmosDb:DatabaseName"]
            ?? throw new InvalidOperationException("CosmosDb:DatabaseName configuration not found.");*/
        //services.AddCosmosDbContext<ApplicationDbContext>();
        //services.AddCosmosDbContext<ApplicationDbContext>(cosmosConnectionString);/*options =>
        /*{
            options.UseCosmos(
                cosmosConnectionString,
                cosmosDatabaseName,
                cosmosOptions =>
                {
                    // In development, configure for the emulator
                    if (isDevelopment)
                    {
                        // Use Gateway mode for the emulator (required for localhost)
                        // cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);

                        // Limit to endpoint to prevent DNS resolution to internal Docker IPs
                        // cosmosOptions.LimitToEndpoint();

                        // Accept self-signed certificates from the emulator
                        /*cosmosOptions.HttpClientFactory(() => new HttpClient(new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        }));
                    }
                    else
                    {
                        // Use Direct mode for production (better performance)
                        cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Direct);
                    }
                });
        });*/

        return services;
    }

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
        services.AddScoped<IAuditLogRepository, CosmosDbAuditLogRepository>();
        services.AddScoped<IBlogPostRepository, CosmosDbBlogPostRepository>();
        services.AddScoped<IMediaMetadataRepository, CosmosDbMediaMetadataRepository>();
        services.AddScoped<IPageRepository, CosmosDbPageRepository>();

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
        services.AddCosmosDbContext(configuration, isDevelopment);
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
            logger?.LogInformation("CosmosDB database and containers are ready.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while ensuring the database was created.");
            throw;
        }
    }
}
