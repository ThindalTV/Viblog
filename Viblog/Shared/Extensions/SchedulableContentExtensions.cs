using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;

namespace Viblog.Shared.Extensions;

/// <summary>
/// Extension methods for ISchedulableContent to simplify common operations.
/// </summary>
public static class SchedulableContentExtensions
{
    /// <summary>
    /// Checks if content is published (Live != null).
    /// </summary>
    public static bool IsPublished(this ISchedulableContent content)
    {
        return content switch
        {
            BlogPost post => post.Live != null,
            Page page => page.Live != null,
            _ => false
        };
    }

    /// <summary>
    /// Gets the Live content for display.
    /// </summary>
    public static BaseContent? GetLiveContent(this ISchedulableContent content)
    {
        return content switch
        {
            BlogPost post => post.Live,
            Page page => page.Live,
            _ => null
        };
    }

    /// <summary>
    /// Gets the Draft content for editing.
    /// </summary>
    public static BaseContent? GetDraftContent(this ISchedulableContent content)
    {
        return content switch
        {
            BlogPost post => post.Draft,
            Page page => page.Draft,
            _ => null
        };
    }

    /// <summary>
    /// Checks if Draft differs from Live using hash comparison.
    /// Fast O(1) operation.
    /// </summary>
    public static bool DraftDiffersFromLive(this ISchedulableContent content)
    {
        var live = content.GetLiveContent();
        if (live == null) return true; // Never published

        var draft = content.GetDraftContent();
        if (draft == null) return false;

        // Compute hashes if not already computed
        if (string.IsNullOrEmpty(draft.ContentHash))
            draft.ComputeHash();
        if (string.IsNullOrEmpty(live.ContentHash))
            live.ComputeHash();

        return draft.ContentHash != live.ContentHash;
    }

    /// <summary>
    /// Checks if content is scheduled for future publish.
    /// </summary>
    public static bool IsScheduled(this ISchedulableContent content)
    {
        return content.Schedule.Status == ContentStatus.Scheduled &&
               content.Schedule.ScheduledPublishDate.HasValue;
    }

    /// <summary>
    /// Checks if scheduled content is ready to be published.
    /// </summary>
    public static bool IsReadyToPublish(this ISchedulableContent content)
    {
        return content.Schedule.Status == ContentStatus.Scheduled &&
               content.Schedule.ScheduledPublishDate.HasValue &&
               content.Schedule.ScheduledPublishDate.Value <= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Sets the Live content (unpublish if null).
    /// </summary>
    public static void SetLiveContent(this ISchedulableContent content, BaseContent? value)
    {
        switch (content)
        {
            case BlogPost post:
                post.Live = value as BlogPostContent;
                break;
            case Page page:
                page.Live = value as PageContent;
                break;
        }
    }

    /// <summary>
    /// Sets the Draft content.
    /// </summary>
    public static void SetDraftContent(this ISchedulableContent content, BaseContent value)
    {
        switch (content)
        {
            case BlogPost post:
                if (value is BlogPostContent blogContent)
                    post.Draft = blogContent;
                break;
            case Page page:
                if (value is PageContent pageContent)
                    page.Draft = pageContent;
                break;
        }
    }
}
