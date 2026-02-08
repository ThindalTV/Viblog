using System.Xml.Serialization;

namespace Viblog.Infrastructure.Shared.Models.Feeds;

/// <summary>
/// Represents an Atom 1.0 feed
/// </summary>
[XmlRoot("feed", Namespace = "http://www.w3.org/2005/Atom")]
public class AtomFeed
{
    /// <summary>
    /// The title of the feed
    /// </summary>
    [XmlElement("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A subtitle for the feed
    /// </summary>
    [XmlElement("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>
    /// Links associated with the feed
    /// </summary>
    [XmlElement("link")]
    public List<AtomLink> Links { get; set; } = [];

    /// <summary>
    /// A permanent, universally unique identifier for the feed
    /// </summary>
    [XmlElement("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The most recent instant in time when the feed was modified
    /// </summary>
    [XmlElement("updated")]
    public string Updated { get; set; } = string.Empty;

    /// <summary>
    /// Collection of entries (blog posts) in the feed
    /// </summary>
    [XmlElement("entry")]
    public List<AtomEntry> Entries { get; set; } = [];
}
