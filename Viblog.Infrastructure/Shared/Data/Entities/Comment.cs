namespace Viblog.Infrastructure.Shared.Data.Entities;

/// <summary>
/// Represents a comment on a blog post
/// </summary>
public class Comment
{
    /// <summary>
    /// Unique identifier for the comment
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// ID of the user who posted the comment
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Name of the commenter (for display)
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Email of the commenter (not displayed publicly)
    /// </summary>
    public string? AuthorEmail { get; set; }

    /// <summary>
    /// Website of the commenter (optional)
    /// </summary>
    public string? AuthorWebsite { get; set; }

    /// <summary>
    /// The comment content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the comment was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the comment was last updated
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Whether the comment has been approved for display
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// Whether the comment is flagged as spam
    /// </summary>
    public bool IsSpam { get; set; } = false;

    /// <summary>
    /// Whether the comment has been deleted (soft delete)
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// ID of the parent comment (for threaded replies)
    /// </summary>
    public string? ParentCommentId { get; set; }

    /// <summary>
    /// IP address of the commenter (for moderation)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the commenter (for moderation)
    /// </summary>
    public string? UserAgent { get; set; }
}
