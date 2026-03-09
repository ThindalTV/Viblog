namespace Viblog.Infrastructure.Data.Entities.Content;

/// <summary>
/// Publishing schedule information for content.
/// Pure data container with no business logic.
/// </summary>
public class ContentSchedule
{
    /// <summary>
    /// Current scheduling status (Draft or Scheduled).
    /// Note: Published is determined by Live != null, not by this status.
    /// </summary>
    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    /// <summary>
    /// Future date when content should be auto-published.
    /// Only used when Status is Scheduled.
    /// </summary>
    public DateTimeOffset? ScheduledPublishDate { get; set; }

    /// <summary>
    /// When the content was last published (Live was updated).
    /// Used for "Published X days ago" display.
    /// </summary>
    public DateTimeOffset? PublishedAt { get; set; }
}
