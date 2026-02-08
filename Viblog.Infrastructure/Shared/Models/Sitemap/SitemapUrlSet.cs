using System.Xml.Serialization;

namespace Viblog.Infrastructure.Shared.Models.Sitemap;

/// <summary>
/// Represents a sitemap URL set
/// </summary>
[XmlRoot("urlset", Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9")]
public class SitemapUrlSet
{
    /// <summary>
    /// Collection of URLs in the sitemap
    /// </summary>
    [XmlElement("url")]
    public List<SitemapUrl> Urls { get; set; } = [];
}
