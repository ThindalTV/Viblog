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

    // Live version fields (currently published)
    
    /// <summary>
    /// The title of the live page version
    /// </summary>
    public string LiveTitle { get; set; } = string.Empty;

    /// <summary>
    /// Raw markdown content of the live page version
    /// </summary>
    public string LiveMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// Rendered HTML content of the live page version (generated from Markdown)
    /// </summary>
    public string LiveContent { get; set; } = string.Empty;

    /// <summary>
    /// Featured image URL for the live page version
    /// </summary>
    public string? LiveFeaturedImageUrl { get; set; }

    /// <summary>
    /// Alt text for the featured image on the live page version
    /// </summary>
    public string? LiveFeaturedImageAlt { get; set; }

    /// <summary>
    /// Meta description for SEO purposes on the live page version
    /// </summary>
    public string? LiveMetaDescription { get; set; }

    /// <summary>
    /// Meta keywords for SEO purposes on the live page version
    /// </summary>
    public string? LiveMetaKeywords { get; set; }

    // Draft version fields (work in progress)

    /// <summary>
    /// The title of the draft page version
    /// </summary>
    public string DraftTitle { get; set; } = string.Empty;

    /// <summary>
    /// Raw markdown content of the draft page version
    /// </summary>
    public string DraftMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// Rendered HTML content of the draft page version (generated from Markdown)
    /// </summary>
    public string DraftContent { get; set; } = string.Empty;

    /// <summary>
    /// Featured image URL for the draft page version
    /// </summary>
    public string? DraftFeaturedImageUrl { get; set; }

    /// <summary>
    /// Alt text for the featured image on the draft page version
    /// </summary>
    public string? DraftFeaturedImageAlt { get; set; }

    /// <summary>
    /// Meta description for SEO purposes on the draft page version
    /// </summary>
    public string? DraftMetaDescription { get; set; }

    /// <summary>
    /// Meta keywords for SEO purposes on the draft page version
    /// </summary>
    public string? DraftMetaKeywords { get; set; }

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
            LiveTitle,
            LiveContent,
            DraftTitle,
            DraftContent,
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
        LiveTitle = DraftTitle;
        LiveMarkdown = DraftMarkdown;
        LiveContent = DraftContent;
        LiveFeaturedImageUrl = DraftFeaturedImageUrl;
        LiveFeaturedImageAlt = DraftFeaturedImageAlt;
        LiveMetaDescription = DraftMetaDescription;
        LiveMetaKeywords = DraftMetaKeywords;

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
