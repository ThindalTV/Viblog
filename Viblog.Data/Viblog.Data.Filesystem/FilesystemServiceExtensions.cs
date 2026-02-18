using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Viblog.Data.Filesystem.Configuration;
using Viblog.Data.Filesystem.Data.Repositories;
using Viblog.Data.Filesystem.Indexing;
using Viblog.Data.Filesystem.Storage;
using Viblog.Infrastructure.Shared.Data.Indexing;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.Filesystem;

/// <summary>
/// Extension methods for registering filesystem-based data access services
/// </summary>
public static class FilesystemServiceExtensions
{
    /// <summary>
    /// Add filesystem storage configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFilesystemStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration - Bind the section to the options
        var section = configuration.GetSection(FilesystemStorageOptions.SectionName);
        services.Configure<FilesystemStorageOptions>(section);

        // Register file storage service
        services.AddSingleton<IFilesystemFileStorage, FilesystemFileStorage>();

        return services;
    }

    /// <summary>
    /// Register filesystem-based repository implementations
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFilesystemRepositories(this IServiceCollection services)
    {
        // Register the generic repository
        services.AddScoped(typeof(IRepository<>), typeof(FilesystemRepository<>));

        // Register specific repositories
        services.AddScoped<IBlogPostRepository, FileSystemBlogPostRepository>();
        services.AddScoped<IMediaMetadataRepository, FileSystemMediaMetadataRepository>();
        services.AddScoped<IPageRepository, FileSystemPageRepository>();
        services.AddScoped<IUserRepository, FileSystemUserRepository>();
        services.AddScoped<IAuditLogRepository, FileSystemAuditLogRepository>();

        // Register media storage repository
        services.AddScoped<IMediaStorageRepository, FileSystemMediaRepository>();

        return services;
    }

    /// <summary>
    /// Add all filesystem services (Storage + Repositories)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFilesystemDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFilesystemStorage(configuration);
        services.AddFilesystemRepositories();

        return services;
    }
}
