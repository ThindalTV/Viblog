using System.ComponentModel.DataAnnotations;

namespace Viblog.Admin.Models;

/// <summary>
/// View model for blog post editing
/// </summary>
public class BlogPostModel
{
    public string? Id { get; set; }
    public string? PartitionKey { get; set; }

    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug is required")]
    public string Slug { get; set; } = string.Empty;

    public string Short { get; set; } = string.Empty;

    /// <summary>
    /// Raw markdown content (optional - for "short" posts without content)
    /// </summary>
    public string Markdown { get; set; } = string.Empty;

    /// <summary>
    /// Rendered HTML content (generated from Markdown, null for "short" posts)
    /// </summary>
    public string? Content { get; set; }

    public string? FeaturedImageUrl { get; set; }
    public string? FeaturedImageAlt { get; set; }

    [Required(ErrorMessage = "Author name is required")]
    public string AuthorName { get; set; } = string.Empty;

    public string AuthorId { get; set; } = string.Empty;

    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.Now;
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }

    public List<string> Tags { get; set; } = [];
    public List<string> CategoryIds { get; set; } = [];
}
