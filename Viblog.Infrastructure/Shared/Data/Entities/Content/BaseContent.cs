using System.Security.Cryptography;
using System.Text;

namespace Viblog.Infrastructure.Shared.Data.Entities.Content;

/// <summary>
/// Base content structure for both Draft and Live versions.
/// Contains fields common to all content types (BlogPost, Page).
/// </summary>
public class BaseContent
{
    // Core content fields
    public string Title { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    public string? FeaturedImageAlt { get; set; }

    // SEO fields
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }

    // Search index
    public string SearchIndex { get; set; } = string.Empty;

    // Content hash for change detection (ALL fields, computed on save)
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Computes a hash from ALL content fields for accurate change detection.
    /// Call this method whenever content is saved or cloned.
    /// </summary>
    public void ComputeHash()
    {
        ContentHash = ComputeContentHashCore();
    }

    /// <summary>
    /// Core hash computation for base fields.
    /// Override this in derived classes to include type-specific fields.
    /// </summary>
    protected virtual string ComputeContentHashCore()
    {
        var hashInput = $"{Title}|{Markdown}|{Content}|" +
                       $"{FeaturedImageUrl}|{FeaturedImageAlt}|" +
                       $"{MetaDescription}|{MetaKeywords}|" +
                       $"{SearchIndex}";

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToBase64String(bytes);
    }
}
