using Microsoft.Extensions.DependencyInjection;
using Viblog.Data.AzureBlobStorage.Storage;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.AzureBlobStorage;

/// <summary>
/// Extension methods for registering Azure Blob Storage services
/// </summary>
public static class AzureBlobStorageServiceExtensions
{
    /// <summary>
    /// Register Azure Blob Storage implementation for media storage
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAzureBlobStorageRepository(this IServiceCollection services)
    {
        // Register media storage repository
        services.AddScoped<IMediaStorageRepository, BlobStorageRepository>();

        return services;
    }
}
