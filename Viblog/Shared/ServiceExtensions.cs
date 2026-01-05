using Microsoft.Extensions.DependencyInjection;
using Viblog.Shared.Infrastructure;
using Viblog.Shared.Services;

namespace Viblog.Shared;

/// <summary>
/// Extension methods for registering service layer dependencies
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Register blog services with the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBlogServices(this IServiceCollection services)
    {
        // Register search service
        services.AddScoped<IBlogSearchService, BlogSearchService>();

        // Register SEO services
        services.AddScoped<StructuredDataHelper>();
        services.AddScoped<ISitemapService, SitemapService>();

        // Register text utilities
        services.AddScoped<ITextUtilities, TextUtilities>();

        // Additional blog services can be registered here as they are created
        // Example:
        // services.AddScoped<IBlogPostService, BlogPostService>();
        // services.AddScoped<ICategoryService, CategoryService>();

        return services;
    }
}
