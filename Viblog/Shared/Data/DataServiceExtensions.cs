using Microsoft.Extensions.DependencyInjection;
using Vilog.Shared.Data.Entities;
using Vilog.Shared.Data.Repositories;

namespace Vilog.Shared.Data;

/// <summary>
/// Extension methods for registering data access services
/// </summary>
public static class DataServiceExtensions
{
    /// <summary>
    /// Register repository services with the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Register the generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register specific repositories
        services.AddScoped<IBlogPostRepository, BlogPostRepository>();

        return services;
    }
}
