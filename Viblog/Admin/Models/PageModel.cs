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
}
