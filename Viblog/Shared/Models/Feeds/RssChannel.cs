using System.Xml.Serialization;

namespace Vilog.Shared.Models.Feeds;

/// <summary>
/// Represents the channel element of an RSS feed
/// </summary>
public class RssChannel
{
    /// <summary>
    /// The name of the channel
    /// </summary>
    [XmlElement("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The URL to the website corresponding to the channel
    /// </summary>
    [XmlElement("link")]
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// Phrase or sentence describing the channel
    /// </summary>
    [XmlElement("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The language the channel is written in
    /// </summary>
    [XmlElement("language")]
    public string Language { get; set; } = "en-us";

    /// <summary>
    /// The last time the content of the channel changed
    /// </summary>
    [XmlElement("lastBuildDate")]
    public string LastBuildDate { get; set; } = string.Empty;

    /// <summary>
    /// Self-referencing Atom link
    /// </summary>
    [XmlElement("link", Namespace = "http://www.w3.org/2005/Atom")]
    public AtomLink? AtomLink { get; set; }

    /// <summary>
    /// Collection of items (blog posts) in the feed
    /// </summary>
    [XmlElement("item")]
    public List<RssItem> Items { get; set; } = [];
}
