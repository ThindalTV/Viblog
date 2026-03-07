using Viblog.Api.Endpoints;

namespace Viblog.Api;

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
    public static WebApplication MapViblogApiEndpoints(this WebApplication app)
    {
        // Map feed endpoints (RSS, Atom)
        app.MapFeedEndpoints();

        // Map SEO endpoints (sitemap.xml, robots.txt)
        app.MapSitemapEndpoints();

        // Serve media files through the app at /media/{storagePath}
        app.MapMediaServeEndpoints();

        // Map media library management endpoints
        var mediaGroup = app.MapGroup("/api/media")
            .WithTags("Media");
        mediaGroup.MapMediaEndpoints();

        return app;
    }
}
