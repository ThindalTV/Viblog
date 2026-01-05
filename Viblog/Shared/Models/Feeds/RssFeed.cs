using System.Xml.Serialization;

namespace Vilog.Shared.Models.Feeds;

/// <summary>
/// Represents an RSS 2.0 feed
/// </summary>
[XmlRoot("rss")]
public class RssFeed
{
    /// <summary>
    /// RSS version (always 2.0)
    /// </summary>
    [XmlAttribute("version")]
    public string Version { get; set; } = "2.0";

    /// <summary>
    /// Atom namespace for self-referencing link
    /// </summary>
    [XmlAttribute("atom", Namespace = "http://www.w3.org/2000/xmlns/")]
    public string AtomNamespace { get; set; } = "http://www.w3.org/2005/Atom";

    /// <summary>
    /// The RSS channel containing feed metadata and items
    /// </summary>
    [XmlElement("channel")]
    public RssChannel Channel { get; set; } = new();
}
