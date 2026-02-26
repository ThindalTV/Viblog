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

    // Computed state properties (not stored - derived from Live/Schedule)
    /// <summary>True when Live content exists and is visible to the public.</summary>
    public bool IsPublished => Live != null;

    /// <summary>True when published Live content exists but Draft has a scheduled update pending.</summary>
    public bool HasPendingUpdate => IsPublished && Schedule.Status == ContentStatus.Scheduled;

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
