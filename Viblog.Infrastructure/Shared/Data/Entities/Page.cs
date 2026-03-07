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


    // Stored publish state — set via SetLiveContent, used directly in EF queries
    public bool IsPublished { get; set; }

    // Page-specific metadata
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
}
