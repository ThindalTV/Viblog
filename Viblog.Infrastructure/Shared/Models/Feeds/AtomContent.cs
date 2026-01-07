using System.Xml;
using System.Xml.Serialization;

namespace Viblog.Infrastructure.Shared.Models.Feeds;

/// <summary>
/// Represents content in an Atom entry
/// </summary>
public class AtomContent
{
    /// <summary>
    /// The media type of the content
    /// </summary>
    [XmlAttribute("type")]
    public string Type { get; set; } = "html";

    /// <summary>
    /// The content value (CDATA for HTML)
    /// </summary>
    [XmlText]
    public XmlNode[]? Value { get; set; }
}
