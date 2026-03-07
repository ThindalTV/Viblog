using Viblog.Infrastructure.Shared.Data.Entities.Content;

namespace Viblog.Infrastructure.Shared.Data.Entities;

/// <summary>
/// Represents a blog post with Draft/Live versioning and scheduling.
/// Version history stored separately in BlogPostVersion entities.
/// </summary>
public class BlogPost : BaseEntity, ISchedulableContent
{
    // ISchedulableContent implementation
    public ContentSchedule Schedule { get; set; } = new();

    // Identification
    public string Slug { get; set; } = string.Empty;

    // Versioned Content (Draft/Live model)
    public BlogPostContent Draft { get; set; } = new();
    public BlogPostContent? Live { get; set; }

    // Stored publish state — set via SetLiveContent, used directly in EF queries
    public bool IsPublished { get; set; }

    // Denormalized search index from Live content — set via SetLiveContent, used directly in EF queries
    public string LiveSearchIndex { get; set; } = string.Empty;

    // BlogPost-specific metadata
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }  // First publish date (for sorting)
    public bool IsFeatured { get; set; }
    public int ViewCount { get; set; }
    public int ReadingTimeMinutes { get; set; }

    // Tags and Categories
    public List<string> Tags { get; set; } = [];
    public List<string> CategoryIds { get; set; } = [];
    public List<string> CategoryNames { get; set; } = [];

    // Media
    public List<string> MediaUrls { get; set; } = [];
}
