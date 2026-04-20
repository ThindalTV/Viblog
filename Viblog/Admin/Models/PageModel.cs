using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

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

    /// <summary>
    /// Draft version content (what editors work with)
    /// </summary>
    public PageContentModel Draft { get; set; } = new();

    /// <summary>
    /// Live version content (currently published - null if not yet published, read-only in UI)
    /// </summary>
    public PageContentModel? Live { get; set; }

    // Common fields

    [Required(ErrorMessage = "Author name is required")]
    public string AuthorName { get; set; } = string.Empty;

    public string AuthorId { get; set; } = string.Empty;
    public int ViewCount { get; set; }

    /// <summary>
    /// Computes a deterministic hash of all editable fields for dirty-tracking.
    /// </summary>
    public string GetFormHash()
    {
        var sb = new StringBuilder();
        sb.Append(Slug);
        sb.Append('|');
        sb.Append(Draft.Title);
        sb.Append('|');
        sb.Append(Draft.Markdown);
        sb.Append('|');
        sb.Append(Draft.FeaturedImageUrl);
        sb.Append('|');
        sb.Append(Draft.FeaturedImageAlt);
        sb.Append('|');
        sb.Append(Draft.MetaDescription);
        sb.Append('|');
        sb.Append(Draft.MetaKeywords);
        sb.Append('|');
        sb.Append(Draft.ShowTitle);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexStringLower(bytes);
    }
}
