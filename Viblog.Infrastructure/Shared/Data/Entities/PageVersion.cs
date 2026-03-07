using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities.Content;

namespace Viblog.Infrastructure.Shared.Data.Entities;

/// <summary>
/// Persisted version snapshot for Page content.
/// Stored separately from Page entity for scalability.
/// </summary>
public class PageVersion : BaseEntity
{
    /// <summary>
    /// Reference to the parent Page.
    /// </summary>
    public string ContentId { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot of the content at time of publishing.
    /// </summary>
    public PageContent Content { get; set; } = new();

    /// <summary>
    /// When this version was published.
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>
    /// User ID who published this version.
    /// </summary>
    public string PublishedBy { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the user who published this version.
    /// </summary>
    public string PublishedByName { get; set; } = string.Empty;

    /// <summary>
    /// Optional note describing what changed in this version.
    /// </summary>
    public string? ChangeNote { get; set; }

    /// <summary>
    /// Sequential version number (1, 2, 3, etc.)
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// GroupKey for CosmosDB (use content type for balanced distribution).
    /// </summary>
    public new string GroupKey { get; set; } = "PageVersion";
}
