using System.Xml.Serialization;

namespace EricJohansson.se.Models.Feed;

/// <summary>
/// Represents a category in an Atom feed
/// </summary>
public class AtomCategory
{
    /// <summary>
    /// Identifies the category
    /// </summary>
    [XmlAttribute("term")]
    public string Term { get; set; } = string.Empty;

    /// <summary>
    /// An optional categorization scheme (URI)
    /// </summary>
    [XmlAttribute("scheme")]
    public string? Scheme { get; set; }

    /// <summary>
    /// A human-readable label for display
    /// </summary>
    [XmlAttribute("label")]
    public string? Label { get; set; }
}
