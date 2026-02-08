using System.Xml.Serialization;

namespace Viblog.Infrastructure.Shared.Models.Feeds;

/// <summary>
/// Represents an Atom link element
/// </summary>
public class AtomLink
{
    /// <summary>
    /// The URI of the referenced resource
    /// </summary>
    [XmlAttribute("href")]
    public string Href { get; set; } = string.Empty;

    /// <summary>
    /// The relationship type
    /// </summary>
    [XmlAttribute("rel")]
    public string? Rel { get; set; }

    /// <summary>
    /// The media type of the resource
    /// </summary>
    [XmlAttribute("type")]
    public string? Type { get; set; }
}
