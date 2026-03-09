using Microsoft.Extensions.DependencyInjection;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Shared.Data.Sources.AzureStorage.Storage;

namespace Viblog.Shared.Data.Sources.AzureStorage;

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
        services.AddScoped<IMediaStorageRepository, AzureBlobStorageMediaRepository>();

        return services;
    }
}
