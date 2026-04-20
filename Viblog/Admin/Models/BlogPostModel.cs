using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

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

    public bool IsFeatured { get; set; }

    public List<string> Tags { get; set; } = [];
    public List<string> CategoryIds { get; set; } = [];

    /// <summary>
    /// Computes a deterministic hash of all editable fields for dirty-tracking.
    /// </summary>
    public string GetFormHash()
    {
        var sb = new StringBuilder();
        sb.Append(Title);
        sb.Append('|');
        sb.Append(Slug);
        sb.Append('|');
        sb.Append(Short);
        sb.Append('|');
        sb.Append(Markdown);
        sb.Append('|');
        sb.Append(FeaturedImageUrl);
        sb.Append('|');
        sb.Append(FeaturedImageAlt);
        sb.Append('|');
        sb.Append(IsFeatured);
        sb.Append('|');
        sb.Append(string.Join(',', Tags.Order()));
        sb.Append('|');
        sb.Append(string.Join(',', CategoryIds.Order()));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexStringLower(bytes);
    }
}
