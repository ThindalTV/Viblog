using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Shared.Data.Repositories;

namespace Viblog.Shared.Data;

/// <summary>
/// Extension methods for registering data access services
/// </summary>
public static class DataServiceExtensions
{
    /// <summary>
    /// Register repository services with the dependency injection container.
    /// Note: Repository implementations are now registered via the specific data provider library
    /// (e.g., Viblog.Data.CosmosDb.AddCosmosDbRepositories()).
    /// This method is kept for backward compatibility but does nothing.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    [Obsolete("Use AddCosmosDbRepositories() from Viblog.Data.CosmosDb instead")]
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Repository registrations moved to data provider libraries
        // For CosmosDB: use services.AddCosmosDbRepositories()
        return services;
    }
}
