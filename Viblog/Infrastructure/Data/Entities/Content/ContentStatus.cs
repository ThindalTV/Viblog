namespace Viblog.Infrastructure.Data.Entities.Content;

/// <summary>
/// Scheduling status for content.
/// Only 2 states: Draft (default) or Scheduled (future publish).
/// Published is determined by Live != null, not by this enum.
/// </summary>
public enum ContentStatus
{
    /// <summary>
    /// Working version, not scheduled for publish.
    /// Default state.
    /// </summary>
    Draft,

    /// <summary>
    /// Scheduled to publish at a future date.
    /// Will auto-publish when ScheduledPublishDate is reached.
    /// </summary>
    Scheduled
}
