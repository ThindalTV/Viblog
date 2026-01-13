using System.ComponentModel.DataAnnotations;

namespace Viblog.Admin.Models;

/// <summary>
/// View model for page editing
/// </summary>
public class PageModel
{
    public string? Id { get; set; }
    public string? PartitionKey { get; set; }

    [Required(ErrorMessage = "Slug is required")]
    public string Slug { get; set; } = string.Empty;

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishDate { get; set; }

    // Draft version fields (what editors work with)
    
    [Required(ErrorMessage = "Title is required")]
    public string DraftTitle { get; set; } = string.Empty;

    /// <summary>
    /// Raw markdown content for the draft version
    /// </summary>
    public string DraftMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// Rendered HTML content for the draft version (generated from Markdown)
    /// </summary>
    public string? DraftContent { get; set; }

    public string? DraftFeaturedImageUrl { get; set; }
    public string? DraftFeaturedImageAlt { get; set; }
    public string? DraftMetaDescription { get; set; }
    public string? DraftMetaKeywords { get; set; }

    // Live version fields (currently published - read-only in UI)
    
    public string LiveTitle { get; set; } = string.Empty;
    public string LiveMarkdown { get; set; } = string.Empty;
    public string LiveContent { get; set; } = string.Empty;
    public string? LiveFeaturedImageUrl { get; set; }
    public string? LiveFeaturedImageAlt { get; set; }
    public string? LiveMetaDescription { get; set; }
    public string? LiveMetaKeywords { get; set; }

    // Common fields
    
    [Required(ErrorMessage = "Author name is required")]
    public string AuthorName { get; set; } = string.Empty;

    public string AuthorId { get; set; } = string.Empty;
    public int ViewCount { get; set; }
}
