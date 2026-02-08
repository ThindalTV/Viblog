using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Options;
using Viblog.Infrastructure.Shared.Models.Sitemap;
using Viblog.Infrastructure.Shared.Services;
using Viblog.Shared.Configuration;

namespace Viblog.Api.Endpoints;

/// <summary>
/// Sitemap API endpoints
/// </summary>
public static class SitemapEndpoints
{
    /// <summary>
    /// Map sitemap endpoints to the application
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication MapSitemapEndpoints(this WebApplication app)
    {
        var sitemapGroup = app.MapGroup("/")
            .WithTags("SEO")
            .WithDescription("SEO endpoints including sitemap.xml and robots.txt for search engine indexing");

        // Sitemap.xml
        sitemapGroup.MapGet("/sitemap.xml", GetSitemap)
            .WithName("GetSitemap")
            .WithSummary("Get XML sitemap of all blog pages")
            .WithDescription("Returns an XML sitemap containing all public blog pages including posts, categories, tags, and archive pages. Helps search engines discover and index content.")
            .Produces<string>(StatusCodes.Status200OK, "application/xml")
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Robots.txt
        sitemapGroup.MapGet("/robots.txt", GetRobotsTxt)
            .WithName("GetRobotsTxt")
            .WithSummary("Get robots.txt file")
            .WithDescription("Returns the robots.txt file that instructs search engine crawlers which pages to crawl and which to exclude.")
            .Produces<string>(StatusCodes.Status200OK, "text/plain");

        return app;
    }

    /// <summary>
    /// Get XML sitemap of all blog pages
    /// </summary>
    /// <param name="sitemapService">The sitemap generation service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sitemap XML</returns>
    private static async Task<IResult> GetSitemap(
        ISitemapService sitemapService,
        CancellationToken cancellationToken)
    {
        try
        {
            var sitemap = await sitemapService.GenerateSitemapAsync(cancellationToken);
            var xml = SerializeToXml(sitemap);
            return Results.Content(xml, "application/xml", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            // In production, log the exception
            return Results.Problem(
                title: "Failed to generate sitemap",
                detail: "An error occurred while generating the sitemap. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get robots.txt file
    /// </summary>
    /// <param name="siteMetadata">Site metadata configuration</param>
    /// <returns>Robots.txt content</returns>
    private static IResult GetRobotsTxt(IOptions<SiteMetadata> siteMetadata)
    {
        var baseUrl = siteMetadata.Value.BaseUrl;
        
        var robotsTxt = $"""
            # robots.txt for {siteMetadata.Value.SiteName}
            User-agent: *
            Allow: /
            
            # Disallow admin and account areas
            Disallow: /Account/
            Disallow: /admin/
            
            # Disallow search results to avoid duplicate content
            Disallow: /search
            
            # Sitemap location
            Sitemap: {baseUrl}/sitemap.xml
            """;

        return Results.Content(robotsTxt, "text/plain", Encoding.UTF8);
    }

    /// <summary>
    /// Serialize a sitemap to XML string
    /// </summary>
    private static string SerializeToXml(SitemapUrlSet sitemap)
    {
        var serializer = new XmlSerializer(typeof(SitemapUrlSet));
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        serializer.Serialize(xmlWriter, sitemap);
        return stringWriter.ToString();
    }
}
