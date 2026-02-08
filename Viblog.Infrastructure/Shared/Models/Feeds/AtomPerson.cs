using System.Xml.Serialization;

namespace Viblog.Infrastructure.Shared.Models.Feeds;

/// <summary>
/// Represents a person (author or contributor) in an Atom feed
/// </summary>
public class AtomPerson
{
    /// <summary>
    /// The name of the person
    /// </summary>
    [XmlElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the person (optional)
    /// </summary>
    [XmlElement("email")]
    public string? Email { get; set; }

    /// <summary>
    /// The URI of the person (optional)
    /// </summary>
    [XmlElement("uri")]
    public string? Uri { get; set; }
}
