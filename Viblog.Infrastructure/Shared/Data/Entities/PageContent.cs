namespace Viblog.Infrastructure.Shared.Data.Entities;

/// <summary>
/// Represents the content of a page version (either draft or live)
/// </summary>
public class PageContent
{
    /// <summary>
    /// The title of the page
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Raw markdown content of the page
    /// </summary>
    public string Markdown { get; set; } = string.Empty;

    /// <summary>
    /// Rendered HTML content of the page (generated from Markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Featured image URL for the page
    /// </summary>
    public string? FeaturedImageUrl { get; set; }

    /// <summary>
    /// Alt text for the featured image
    /// </summary>
    public string? FeaturedImageAlt { get; set; }

    /// <summary>
    /// Meta description for SEO purposes
    /// </summary>
    public string? MetaDescription { get; set; }

    /// <summary>
    /// Meta keywords for SEO purposes
    /// </summary>
    public string? MetaKeywords { get; set; }

    /// <summary>
    /// Whether to display the page title as a heading
    /// </summary>
    public bool ShowTitle { get; set; } = true;
}
