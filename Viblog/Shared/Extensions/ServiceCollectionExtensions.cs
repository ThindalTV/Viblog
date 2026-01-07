using Viblog.Shared.Data.Repositories;
using Viblog.Shared.Data.Repositories.Storage;
using Viblog.Shared.Facades;
using Viblog.Shared.Services;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Infrastructure.Shared.Services;
using Viblog.Infrastructure.Shared.Facades;

namespace Viblog.Shared.Extensions;

/// <summary>
/// Extension methods for registering services in the dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add media storage services with provider selection based on configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMediaStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get provider from configuration
        var provider = configuration["MediaStorage:Provider"]
            ?? throw new InvalidOperationException("MediaStorage:Provider is not configured");

        // Register the appropriate storage repository based on provider
        switch (provider.ToLowerInvariant())
        {
            case "blobstorage":
                services.AddScoped<IMediaStorageRepository, BlobStorageRepository>();
                break;

            case "filesystem":
                services.AddScoped<IMediaStorageRepository, FileSystemStorageRepository>();
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown media storage provider: {provider}. Supported providers: BlobStorage, FileSystem");
        }

        // Register metadata repository
        services.AddScoped<IMediaMetadataRepository, MediaMetadataRepository>();

        // Register services
        services.AddScoped<IMetadataExtractorService, MetadataExtractorService>();
        services.AddScoped<IMediaService, MediaService>();

        // Register facade
        services.AddScoped<IMediaFacade, MediaFacade>();

        return services;
    }
}
