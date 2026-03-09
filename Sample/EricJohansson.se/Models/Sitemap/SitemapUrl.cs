using System.Xml.Serialization;

namespace EricJohansson.se.Models.Sitemap;

/// <summary>
/// Represents a single URL entry in a sitemap
/// </summary>
public class SitemapUrl
{
    /// <summary>
    /// The location (URL) of the page
    /// </summary>
    [XmlElement("loc")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// The date of last modification (YYYY-MM-DD)
    /// </summary>
    [XmlElement("lastmod")]
    public string LastModified { get; set; } = string.Empty;

    /// <summary>
    /// How frequently the page is likely to change
    /// </summary>
    [XmlElement("changefreq")]
    public string ChangeFrequency { get; set; } = string.Empty;

    /// <summary>
    /// The priority of this URL relative to other URLs on the site (0.0 to 1.0)
    /// </summary>
    [XmlElement("priority")]
    public string Priority { get; set; } = string.Empty;
}
