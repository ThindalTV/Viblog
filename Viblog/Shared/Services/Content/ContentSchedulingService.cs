using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Infrastructure.Extensions;
using Viblog.Shared.Extensions;

namespace Viblog.Shared.Services.Content;

/// <summary>
/// Handles all scheduling logic for any content type.
/// Pure business logic, no data access.
/// </summary>
public class ContentSchedulingService
{
    private readonly ContentVersionService _versionService;
    private readonly ILogger<ContentSchedulingService> _logger;

    public ContentSchedulingService(
        ContentVersionService versionService,
        ILogger<ContentSchedulingService> logger)
    {
        _versionService = versionService;
        _logger = logger;
    }

    /// <summary>
    /// Publishes content immediately by promoting Draft to Live.
    /// Works for both new content and updates to published content.
    /// </summary>
    public virtual async Task PublishNowAsync(ISchedulableContent content, string publishedBy, string? publishedByName = null, string? changeNote = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing content {ContentId} by {User}", content.Id, publishedBy);

        // Set first publish date if this is the initial publish (for sorting)
        SetFirstPublishedDateIfNeeded(content);

        // Promote Draft to Live (creates version snapshot internally)
        await _versionService.PromoteDraftToLiveAsync(content, publishedBy, publishedByName, changeNote, cancellationToken);

        // Update scheduling metadata
        content.Schedule.Status = ContentStatus.Draft;  // Back to default
        content.Schedule.PublishedAt = DateTimeOffset.UtcNow;  // For "Last published" display
        content.Schedule.ScheduledPublishDate = null;  // Clear schedule

        _logger.LogInformation("Content {ContentId} published successfully", content.Id);
    }

    /// <summary>
    /// Sets PublishedAt field on first publish only (used for sorting).
    /// Only applies to content types that have a PublishedAt property.
    /// </summary>
    private void SetFirstPublishedDateIfNeeded(ISchedulableContent content)
    {
        // Use pattern matching to check for BlogPost (or other types with PublishedAt)
        if (content is BlogPost post)
        {
            if (post.PublishedAt == null)
            {
                post.PublishedAt = DateTimeOffset.UtcNow;
                _logger.LogDebug("Set first published date for content {ContentId}", content.Id);
            }
        }
        // Add other content types here as needed:
        // else if (content is Page page && page.PublishedAt == null)
        // {
        //     page.PublishedAt = DateTimeOffset.UtcNow;
        // }
    }

    /// <summary>
    /// Schedules content for future publication.
    /// Works for both new content and updates to published content.
    /// </summary>
    public virtual void ScheduleForPublish(ISchedulableContent content, DateTimeOffset publishDate)
    {
        if (publishDate <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("Scheduled publish date must be in the future", nameof(publishDate));
        }

        _logger.LogInformation("Scheduling content {ContentId} for {PublishDate}", content.Id, publishDate);

        content.Schedule.Status = ContentStatus.Scheduled;
        content.Schedule.ScheduledPublishDate = publishDate;

        _logger.LogInformation("Content {ContentId} scheduled successfully", content.Id);
    }

    /// <summary>
    /// Unpublishes content by removing Live version.
    /// Draft remains for continued editing.
    /// </summary>
    public virtual void Unpublish(ISchedulableContent content)
    {
        _logger.LogInformation("Unpublishing content {ContentId}", content.Id);

        // Clear Live field (this hides content from public)
        _versionService.ClearLive(content);

        content.Schedule.Status = ContentStatus.Draft;
        content.Schedule.ScheduledPublishDate = null;

        _logger.LogInformation("Content {ContentId} unpublished successfully", content.Id);
    }

    /// <summary>
    /// Checks if scheduled content is ready to be published.
    /// </summary>
    public virtual bool IsReadyToPublish(ISchedulableContent content)
    {
        return content.IsReadyToPublish();
    }

    /// <summary>
    /// Publishes content if it's ready (scheduled date has passed).
    /// Returns true if content was published.
    /// </summary>
    public virtual async Task<bool> PromoteIfReadyAsync(ISchedulableContent content, string publishedBy, string? publishedByName = null, CancellationToken cancellationToken = default)
    {
        if (!IsReadyToPublish(content))
        {
            return false;
        }

        await PublishNowAsync(content, publishedBy, publishedByName, changeNote: "Scheduled publish", cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>
    /// Checks if Draft differs from Live using hash comparison.
    /// Fast O(1) operation.
    /// </summary>
    public virtual bool DraftDiffersFromLive(ISchedulableContent content)
    {
        return _versionService.DraftDiffersFromLive(content);
    }

    /// <summary>
    /// Resets Draft to match Live, discarding any unpublished changes.
    /// </summary>
    public virtual void ResetDraftToLive(ISchedulableContent content)
    {
        _logger.LogInformation("Resetting Draft to Live for content {ContentId}", content.Id);
        _versionService.ResetDraftToLive(content);
    }

    /// <summary>
    /// Gets a human-readable status description.
    /// </summary>
    public virtual string GetStatusDescription(ISchedulableContent content)
    {
        // Check if published first (Live != null)
        var isPublished = _versionService.IsPublished(content);

        if (content.Schedule.Status == ContentStatus.Scheduled)
        {
            var date = content.Schedule.ScheduledPublishDate?.ToString("MMM dd, yyyy 'at' h:mm tt") ?? "unknown";
            return isPublished ? $"Update scheduled for {date}" : $"Scheduled for {date}";
        }

        if (isPublished)
        {
            if (DraftDiffersFromLive(content))
            {
                return "Published (with unpublished changes)";
            }
            return "Published";
        }

        return "Draft";
    }

    /// <summary>
    /// Validates that the schedule is consistent.
    /// </summary>
    public virtual bool IsValidSchedule(ContentSchedule schedule)
    {
        if (schedule.Status == ContentStatus.Scheduled)
        {
            return schedule.ScheduledPublishDate.HasValue &&
                   schedule.ScheduledPublishDate.Value > DateTimeOffset.UtcNow;
        }

        return true;
    }
}
