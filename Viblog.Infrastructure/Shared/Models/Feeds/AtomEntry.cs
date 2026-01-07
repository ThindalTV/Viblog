using System.Xml;
using System.Xml.Serialization;

namespace Viblog.Infrastructure.Shared.Models.Feeds;

/// <summary>
/// Represents a single entry (blog post) in an Atom feed
/// </summary>
public class AtomEntry
{
    /// <summary>
    /// The title of the entry
    /// </summary>
    [XmlElement("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A permanent, universally unique identifier for the entry
    /// </summary>
    [XmlElement("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Links associated with the entry
    /// </summary>
    [XmlElement("link")]
    public List<AtomLink> Links { get; set; } = [];

    /// <summary>
    /// When the entry was first published
    /// </summary>
    [XmlElement("published")]
    public string? Published { get; set; }

    /// <summary>
    /// The most recent instant in time when the entry was modified
    /// </summary>
    [XmlElement("updated")]
    public string? Updated { get; set; }

    /// <summary>
    /// The author of the entry
    /// </summary>
    [XmlElement("author")]
    public AtomPerson? Author { get; set; }

    /// <summary>
    /// A summary of the entry
    /// </summary>
    [XmlElement("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// The full content of the entry
    /// </summary>
    [XmlElement("content")]
    public AtomContent? Content { get; set; }

    /// <summary>
    /// Categories associated with the entry
    /// </summary>
    [XmlElement("category")]
    public List<AtomCategory> Categories { get; set; } = [];
}
