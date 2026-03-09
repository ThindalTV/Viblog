using System.Security.Cryptography;
using System.Text;

namespace Viblog.Infrastructure.Data.Entities.Content;

/// <summary>
/// Page-specific content structure.
/// Extends base ContentData with display title toggle.
/// </summary>
public class PageContent : BaseContent
{
    /// <summary>
    /// Whether to display the title on the page.
    /// </summary>
    public bool ShowTitle { get; set; } = true;

    /// <summary>
    /// Computes hash including Page-specific fields.
    /// </summary>
    protected override string ComputeContentHashCore()
    {
        var hashInput = $"{Title}|{Markdown}|{Content}|" +
                       $"{FeaturedImageUrl}|{FeaturedImageAlt}|" +
                       $"{MetaDescription}|{MetaKeywords}|" +
                       $"{SearchIndex}|{ShowTitle}";

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToBase64String(bytes);
    }
}
