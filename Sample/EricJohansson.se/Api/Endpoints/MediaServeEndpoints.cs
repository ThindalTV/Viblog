using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Viblog.Infrastructure.Data.Repositories;

namespace EricJohansson.se.Api.Endpoints;

/// <summary>
/// Endpoint that streams media files through the application at /media/{storagePath}
/// </summary>
public static class MediaServeEndpoints
{
    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    /// <summary>
    /// Maps the media serve endpoint at /media/{**storagePath}
    /// </summary>
    public static WebApplication MapMediaServeEndpoints(this WebApplication app)
    {
        app.MapGet("/media/{**storagePath}", ServeMediaAsync)
            .WithName("ServeMedia")
            .WithDescription("Stream a media file by its storage path")
            .WithTags("Media")
            .AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Proxy a media file from backing storage to the HTTP response
    /// </summary>
    private static async Task<IResult> ServeMediaAsync(
        string storagePath,
        [FromServices] IMediaStorageRepository storageRepository,
        [FromServices] ILoggerFactory loggerFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(MediaServeEndpoints));

        try
        {
            var stream = await storageRepository.DownloadAsync(storagePath, cancellationToken);

            var contentType = ResolveContentType(storagePath);

            // Uploaded media files are immutable — safe to cache aggressively
            httpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";

            return Results.Stream(stream, contentType, enableRangeProcessing: true);
        }
        catch (OperationCanceledException)
        {
            return Results.Empty;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not serve media file: {StoragePath}", storagePath);
            return Results.NotFound();
        }
    }

    private static string ResolveContentType(string path)
    {
        var extension = Path.GetExtension(path);
        return _contentTypeProvider.TryGetContentType(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}
