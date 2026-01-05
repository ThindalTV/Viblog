using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vilog.Api.Endpoints;

namespace Vilog.Api;

/// <summary>
/// Extension methods for registering API endpoints
/// </summary>
public static class ApiServiceExtensions
{
    /// <summary>
    /// Register all blog API endpoints with the application
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication MapVilogApiEndpoints(this WebApplication app)
    {
        // Map feed endpoints (RSS, Atom)
        app.MapFeedEndpoints();

        // Map SEO endpoints (sitemap.xml, robots.txt)
        app.MapSitemapEndpoints();

        // Add future API groups here:
        // app.MapWebhookEndpoints();
        // app.MapHealthCheckEndpoints();

        return app;
    }
}
