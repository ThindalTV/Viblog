using System.Security.Cryptography;
using System.Text;

namespace Viblog.Infrastructure.Shared.Data.Entities.Content;

/// <summary>
/// BlogPost-specific content structure.
/// Extends base ContentData with excerpt field.
/// </summary>
public class BlogPostContent : BaseContent
{
    /// <summary>
    /// Short excerpt or summary of the blog post.
    /// </summary>
    public string? Short { get; set; }

    /// <summary>
    /// Computes hash including BlogPost-specific fields.
    /// </summary>
    protected override string ComputeContentHashCore()
    {
        var hashInput = $"{Title}|{Markdown}|{Content}|" +
                       $"{FeaturedImageUrl}|{FeaturedImageAlt}|" +
                       $"{MetaDescription}|{MetaKeywords}|" +
                       $"{SearchIndex}|{Short}";

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToBase64String(bytes);
    }
}
