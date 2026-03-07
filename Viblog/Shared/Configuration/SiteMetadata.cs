namespace Viblog.Shared.Configuration;

/// <summary>
/// Configuration for site-wide metadata used in SEO, structured data, and social sharing
/// </summary>
public class SiteMetadata
{
    /// <summary>
    /// The name of the blog/website
    /// </summary>
    public string SiteName { get; set; } = string.Empty;

    /// <summary>
    /// The base URL of the website (e.g., https://yourblog.com)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Default description used when page-specific description is not available
    /// </summary>
    public string DefaultDescription { get; set; } = string.Empty;

    /// <summary>
    /// Twitter handle (without @) for Twitter Cards
    /// </summary>
    public string? TwitterHandle { get; set; }

    /// <summary>
    /// Facebook App ID for Open Graph
    /// </summary>
    public string? FacebookAppId { get; set; }

    /// <summary>
    /// Default image URL for social sharing when post doesn't have featured image
    /// </summary>
    public string? DefaultImageUrl { get; set; }

    /// <summary>
    /// Locale for the site (e.g., en_US)
    /// </summary>
    public string Locale { get; set; } = "en_US";

    /// <summary>
    /// Site tagline or subtitle
    /// </summary>
    public string? Tagline { get; set; }

    /// <summary>
    /// Contact email for the site
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Organization logo URL for structured data
    /// </summary>
    public string? LogoUrl { get; set; }
}
