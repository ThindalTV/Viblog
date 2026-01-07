using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Viblog.Infrastructure.Frontend.Facades;

namespace Viblog.Api.Endpoints;

/// <summary>
/// Feed API endpoints (RSS, Atom)
/// </summary>
public static class FeedEndpoints
{
    /// <summary>
    /// Map feed endpoints to the application
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication MapFeedEndpoints(this WebApplication app)
    {
        var feedGroup = app.MapGroup("/")
            .WithTags("Feeds")
            .WithDescription("RSS and Atom feed endpoints for blog content syndication");

        // RSS 2.0 Feed
        feedGroup.MapGet("/feed.xml", GetRssFeed)
            .WithName("GetRssFeed")
            .WithSummary("Get RSS 2.0 feed of recent blog posts")
            .WithDescription("Returns an RSS 2.0 formatted XML feed containing the most recent published blog posts. The feed includes post titles, descriptions, content, categories, and publication dates.")
            .Produces<string>(StatusCodes.Status200OK, "application/rss+xml")
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Atom 1.0 Feed
        feedGroup.MapGet("/atom.xml", GetAtomFeed)
            .WithName("GetAtomFeed")
            .WithSummary("Get Atom 1.0 feed of recent blog posts")
            .WithDescription("Returns an Atom 1.0 formatted XML feed containing the most recent published blog posts. The feed includes post titles, summaries, content, categories, and timestamps.")
            .Produces<string>(StatusCodes.Status200OK, "application/atom+xml")
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    /// <summary>
    /// Get RSS 2.0 feed of recent blog posts
    /// </summary>
    /// <param name="feedFacade">The feed facade</param>
    /// <param name="maxPosts">Maximum number of posts to include (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>RSS 2.0 XML feed</returns>
    private static async Task<IResult> GetRssFeed(
        IFeedFacade feedFacade,
        int maxPosts = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var feed = await feedFacade.GenerateRssFeedAsync(maxPosts, cancellationToken);
            var xml = SerializeToXml(feed);
            return Results.Content(xml, "application/rss+xml", Encoding.UTF8);
        }
        catch (Exception)
        {
            // Log error in production
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Feed Generation Error",
                detail: "Unable to generate RSS feed. Please try again later.");
        }
    }

    /// <summary>
    /// Get Atom 1.0 feed of recent blog posts
    /// </summary>
    /// <param name="feedFacade">The feed facade</param>
    /// <param name="maxPosts">Maximum number of posts to include (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Atom 1.0 XML feed</returns>
    private static async Task<IResult> GetAtomFeed(
        IFeedFacade feedFacade,
        int maxPosts = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var feed = await feedFacade.GenerateAtomFeedAsync(maxPosts, cancellationToken);
            var xml = SerializeToXml(feed);
            return Results.Content(xml, "application/atom+xml", Encoding.UTF8);
        }
        catch (Exception)
        {
            // Log error in production
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Feed Generation Error",
                detail: "Unable to generate Atom feed. Please try again later.");
        }
    }

    /// <summary>
    /// Serialize a feed object to XML string
    /// </summary>
    private static string SerializeToXml<T>(T feed)
    {
        var serializer = new XmlSerializer(typeof(T));
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        serializer.Serialize(xmlWriter, feed);
        return stringWriter.ToString();
    }
}
