using Viblog.Infrastructure.Shared.Data.Entities.Content;

namespace Viblog.Infrastructure.Shared.Data.Entities;

/// <summary>
/// Represents a static page with Draft/Live versioning and scheduling.
/// Version history stored separately in PageVersion entities.
/// </summary>
public class Page : BaseEntity, ISchedulableContent
{
    // ISchedulableContent implementation
    public ContentSchedule Schedule { get; set; } = new();

    // Identification
    public string Slug { get; set; } = string.Empty;

    // Versioned Content (Draft/Live model)
    public PageContent Draft { get; set; } = new();
    public PageContent? Live { get; set; }

    // Computed state properties (not stored - derived from Live/Schedule)
    /// <summary>True when Live content exists and is visible to the public.</summary>
    public bool IsPublished => Live != null;

    /// <summary>True when published Live content exists but Draft has a scheduled update pending.</summary>
    public bool HasPendingUpdate => IsPublished && Schedule.Status == ContentStatus.Scheduled;

    // Page-specific metadata
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
}
