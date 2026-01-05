using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Vilog.Shared.Configuration;

/// <summary>
/// Extension methods for configuring Vilog settings
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Registers all Vilog configuration sections with the IOptions pattern
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddVilogConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the root configuration object
        services.Configure<VilogConfiguration>(config =>
        {
            configuration.Bind(config);
        });

        // Register individual configuration sections for more granular injection
        services.Configure<SiteMetadata>(configuration.GetSection("SiteMetadata"));
        services.Configure<CosmosDbSettings>(configuration.GetSection("CosmosDb"));

        return services;
    }
}
