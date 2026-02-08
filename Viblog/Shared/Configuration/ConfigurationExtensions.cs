namespace Viblog.Shared.Configuration;

/// <summary>
/// Extension methods for configuring Viblog settings
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Registers all Viblog configuration sections with the IOptions pattern
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddViblogConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the root configuration object
        services.Configure<ViblogConfiguration>(config =>
        {
            configuration.Bind(config);
        });

        // Register individual configuration sections for more granular injection
        services.Configure<SiteMetadata>(configuration.GetSection("SiteMetadata"));
        services.Configure<CosmosDbSettings>(configuration.GetSection("CosmosDb"));
        services.Configure<MediaLibrarySettings>(configuration.GetSection(MediaLibrarySettings.SectionName));

        return services;
    }
}
