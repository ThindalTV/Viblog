using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;

namespace Viblog.Infrastructure.Shared.Extensions;

/// <summary>
/// Extension methods for ISchedulableContent to simplify common operations.
/// </summary>
public static class SchedulableContentExtensions
{
    extension(ISchedulableContent content)
    {
        /// <summary>
        /// True when content is published and has a scheduled update pending.
        /// Equivalent to Live != null &amp;&amp; Schedule.Status == Scheduled.
        /// </summary>
        public bool HasPendingUpdate =>
            content.IsPublished && content.Schedule.Status == ContentStatus.Scheduled;

        /// <summary>
        /// Gets the Live content for display.
        /// </summary>
        public BaseContent? GetLiveContent()
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
        public BaseContent? GetDraftContent()
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
        public bool DraftDiffersFromLive()
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
        public bool IsScheduled()
        {
            return content.Schedule.Status == ContentStatus.Scheduled &&
                   content.Schedule.ScheduledPublishDate.HasValue;
        }

        /// <summary>
        /// Checks if scheduled content is ready to be published.
        /// </summary>
        public bool IsReadyToPublish()
        {
            return content.Schedule.Status == ContentStatus.Scheduled &&
                   content.Schedule.ScheduledPublishDate.HasValue &&
                   content.Schedule.ScheduledPublishDate.Value <= DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Sets the Live content (unpublish if null) and keeps <see cref="ISchedulableContent.IsPublished"/> in sync.
        /// </summary>
        public void SetLiveContent(BaseContent? value)
        {
            switch (content)
            {
                case BlogPost post:
                    post.Live = value as BlogPostContent;
                    post.IsPublished = post.Live is not null;
                    post.LiveSearchIndex = post.Live?.SearchIndex ?? string.Empty;
                    break;
                case Page page:
                    page.Live = value as PageContent;
                    page.IsPublished = page.Live is not null;
                    break;
            }
        }

        /// <summary>
        /// Sets the Draft content.
        /// </summary>
        public void SetDraftContent(BaseContent value)
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
}

