using System.ComponentModel.DataAnnotations;

namespace Viblog.Admin.Models;

/// <summary>
/// View model for page content (either draft or live version)
/// </summary>
public class PageContentModel
{
    public string Title { get; set; } = string.Empty;

    public string Markdown { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public string? FeaturedImageAlt { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public bool ShowTitle { get; set; } = true;
}
