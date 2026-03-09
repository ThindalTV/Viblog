using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace EricJohansson.se.Models.Feed;

/// <summary>
/// Represents a single item (blog post) in an RSS feed
/// </summary>
public class RssItem
{
    /// <summary>
    /// The title of the item
    /// </summary>
    [XmlElement("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the item
    /// </summary>
    [XmlElement("link")]
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// A unique identifier for the item
    /// </summary>
    [XmlElement("guid")]
    public string Guid { get; set; } = string.Empty;

    /// <summary>
    /// The item synopsis
    /// </summary>
    [XmlElement("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the item was published (RFC 822 format)
    /// </summary>
    [XmlElement("pubDate")]
    public string? PubDate { get; set; }

    /// <summary>
    /// Email address of the author
    /// </summary>
    [XmlElement("author")]
    public string? Author { get; set; }

    /// <summary>
    /// Categories associated with the item
    /// </summary>
    [XmlElement("category")]
    public List<string> Categories { get; set; } = [];

    /// <summary>
    /// The full content of the item (CDATA)
    /// </summary>
    [XmlElement("encoded", Namespace = "http://purl.org/rss/1.0/modules/content/")]
    public XmlCDataSection? Content { get; set; }
}
