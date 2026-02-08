namespace Viblog.Infrastructure.Shared.Data.Entities;

/// <summary>
/// Represents a static page with both draft and live versions
/// </summary>
public class Page : BaseEntity
{
    /// <summary>
    /// URL-friendly slug for the page (e.g., "about", "contact")
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the page has a published live version
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Scheduled date and time when the draft version should become live
    /// </summary>
    public DateTimeOffset? PublishDate { get; set; }

    /// <summary>
    /// Live (currently published) version of the page content
    /// </summary>
    public PageContent Live { get; set; } = new();

    /// <summary>
    /// Draft (work in progress) version of the page content
    /// </summary>
    public PageContent Draft { get; set; } = new();

    // Common fields

    /// <summary>
    /// Author ID who created the page
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// Author name (denormalized for display)
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Number of views for this page
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Lowercase concatenated searchable content for efficient searching.
    /// Includes both live and draft content for admin searches.
    /// </summary>
    public string SearchIndex { get; set; } = string.Empty;

    /// <summary>
    /// Updates the search index with current page content
    /// </summary>
    public void UpdateSearchIndex()
    {
        var searchableContent = new[]
        {
            Live.Title,
            Live.Content,
            Draft.Title,
            Draft.Content,
            AuthorName,
            Slug
        };

        SearchIndex = string.Join(" ", searchableContent.Where(s => !string.IsNullOrWhiteSpace(s)))
            .ToLowerInvariant();
    }

    /// <summary>
    /// Promotes the draft version to live if the publish date has been reached
    /// </summary>
    /// <returns>True if the draft was promoted to live, false otherwise</returns>
    public bool PromoteDraftIfScheduled()
    {
        if (!PublishDate.HasValue || PublishDate.Value > DateTimeOffset.UtcNow)
        {
            return false;
        }

        // Copy draft to live
        Live.Title = Draft.Title;
        Live.Markdown = Draft.Markdown;
        Live.Content = Draft.Content;
        Live.FeaturedImageUrl = Draft.FeaturedImageUrl;
        Live.FeaturedImageAlt = Draft.FeaturedImageAlt;
        Live.MetaDescription = Draft.MetaDescription;
        Live.MetaKeywords = Draft.MetaKeywords;
        Live.ShowTitle = Draft.ShowTitle;

        IsPublished = true;
        PublishDate = null;
        UpdatedAt = DateTimeOffset.UtcNow;

        return true;
    }

    /// <summary>
    /// Publishes the draft version immediately
    /// </summary>
    public void PublishDraftNow()
    {
        PublishDate = DateTimeOffset.UtcNow;
        PromoteDraftIfScheduled();
    }
}
