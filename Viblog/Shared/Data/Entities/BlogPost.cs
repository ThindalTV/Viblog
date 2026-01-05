namespace Vilog.Shared.Data.Entities;

/// <summary>
/// Represents a blog post with full content and metadata
/// </summary>
public class BlogPost : BaseEntity
{
    /// <summary>
    /// The title of the blog post
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug for the blog post
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Short summary or excerpt of the post for listing pages
    /// </summary>
    public string Short { get; set; } = string.Empty;

    /// <summary>
    /// Raw markdown content of the blog post
    /// </summary>
    public string Markdown { get; set; } = string.Empty;

    /// <summary>
    /// Rendered HTML content (generated from Markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Featured image URL for the post
    /// </summary>
    public string? FeaturedImageUrl { get; set; }

    /// <summary>
    /// Alt text for the featured image
    /// </summary>
    public string? FeaturedImageAlt { get; set; }

    /// <summary>
    /// Author ID of the post
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// Author name (denormalized for display)
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Publication date and time (defaults to creation time for drafts)
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Indicates whether the post is published or in draft state
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Indicates whether the post is featured
    /// </summary>
    public bool IsFeatured { get; set; }

    /// <summary>
    /// Indicates whether comments are allowed on this post
    /// </summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// Number of views for this post
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// List of tags associated with the post
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// List of category IDs this post belongs to
    /// </summary>
    public List<string> CategoryIds { get; set; } = new();

    /// <summary>
    /// List of category names (denormalized for display)
    /// </summary>
    public List<string> CategoryNames { get; set; } = new();

    /// <summary>
    /// URLs to connected media (images, videos, etc.)
    /// </summary>
    public List<string> MediaUrls { get; set; } = new();

    /// <summary>
    /// Comments associated with this blog post
    /// </summary>
    public List<Comment> Comments { get; set; } = new();

    /// <summary>
    /// Meta description for SEO purposes
    /// </summary>
    public string? MetaDescription { get; set; }

    /// <summary>
    /// Meta keywords for SEO purposes
    /// </summary>
    public string? MetaKeywords { get; set; }

    /// <summary>
    /// Estimated reading time in minutes
    /// </summary>
    public int ReadingTimeMinutes { get; set; }

    /// <summary>
    /// Number of comments on this post
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Last comment date and time
    /// </summary>
    public DateTimeOffset? LastCommentAt { get; set; }

    /// <summary>
    /// Lowercase concatenated searchable content for efficient searching.
    /// Includes title, short description, content, tags, and category names.
    /// </summary>
    public string SearchIndex { get; set; } = string.Empty;

    /// <summary>
    /// Gets the publication year for partitioning purposes
    /// </summary>
    /// <returns>Year as string, or "draft" if unpublished</returns>
    public string GetPublicationYear()
    {
        return IsPublished ? PublishedAt.Year.ToString() : "draft";
    }

    /// <summary>
    /// Sets the partition key based on the publication date.
    /// Should be called before saving a new post or when the publication date changes.
    /// </summary>
    public void UpdatePartitionKey()
    {
        PartitionKey = GetPublicationYear();
    }

    /// <summary>
    /// Updates the search index with current post content
    /// </summary>
    public void UpdateSearchIndex()
    {
        var searchableContent = new[]
        {
            Title,
            Short,
            Content,
            string.Join(" ", Tags),
            string.Join(" ", CategoryNames),
            AuthorName
        };

        SearchIndex = string.Join(" ", searchableContent.Where(s => !string.IsNullOrWhiteSpace(s)))
            .ToLowerInvariant();
    }
}
