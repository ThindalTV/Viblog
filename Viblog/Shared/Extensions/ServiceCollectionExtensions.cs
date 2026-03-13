using Viblog.Shared.Facades;
using Viblog.Shared.Services;
using Viblog.Infrastructure.Facades;
using Viblog.Infrastructure.Services;

namespace Viblog.Shared.Extensions;

/// <summary>
/// Extension methods for registering services in the dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add media storage services.
    /// Note: The IMediaStorageRepository is now registered by the data provider library
    /// (e.g., Viblog.Data.Filesystem.AddFilesystemRepositories() or Viblog.Data.AzureBlobStorage.AddAzureBlobStorageRepository())
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMediaStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Note: IMediaStorageRepository and IMediaMetadataRepository are now registered 
        // by the data provider library (e.g., Viblog.Data.CosmosDb.AddCosmosDbRepositories() 
        // or Viblog.Data.Filesystem.AddFilesystemRepositories())

        // Register services
        services.AddScoped<IMetadataExtractorService, MetadataExtractorService>();
        services.AddScoped<IMediaService, MediaService>();

        // Register facade
        services.AddScoped<IMediaFacade, MediaFacade>();

        return services;
    }
}
