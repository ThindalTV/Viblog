namespace Viblog.Infrastructure.Frontend.Facades;

/// <summary>
/// Facade interface for comment operations with presentation-ready data
/// </summary>
public interface ICommentFacade
{
    /// <summary>
    /// Get approved comments for a blog post by slug
    /// </summary>
    /// <param name="slug">The blog post slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of approved comments organized hierarchically</returns>
    Task<IEnumerable<CommentViewModel>> GetApprovedCommentsAsync(
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a comment to a blog post
    /// </summary>
    /// <param name="slug">The blog post slug</param>
    /// <param name="authorName">The comment author's name</param>
    /// <param name="authorEmail">The comment author's email</param>
    /// <param name="authorWebsite">The comment author's website (optional)</param>
    /// <param name="content">The comment content</param>
    /// <param name="parentCommentId">The parent comment ID for threaded replies (optional)</param>
    /// <param name="ipAddress">The commenter's IP address</param>
    /// <param name="userAgent">The commenter's user agent</param>
    /// <param name="userId">The user ID if authenticated (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if comment was added successfully</returns>
    Task<bool> AddCommentAsync(
        string slug,
        string authorName,
        string authorEmail,
        string? authorWebsite,
        string content,
        string? parentCommentId,
        string? ipAddress,
        string? userAgent,
        string? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a comment (by author or admin)
    /// </summary>
    /// <param name="slug">The blog post slug</param>
    /// <param name="commentId">The comment ID</param>
    /// <param name="content">The updated content</param>
    /// <param name="userId">The user ID making the update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if comment was updated successfully</returns>
    Task<bool> UpdateCommentAsync(
        string slug,
        string commentId,
        string content,
        string? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a comment (soft delete)
    /// </summary>
    /// <param name="slug">The blog post slug</param>
    /// <param name="commentId">The comment ID</param>
    /// <param name="userId">The user ID making the deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if comment was deleted successfully</returns>
    Task<bool> DeleteCommentAsync(
        string slug,
        string commentId,
        string? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve a comment (admin only)
    /// </summary>
    /// <param name="slug">The blog post slug</param>
    /// <param name="commentId">The comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if comment was approved successfully</returns>
    Task<bool> ApproveCommentAsync(
        string slug,
        string commentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a comment as spam (admin only)
    /// </summary>
    /// <param name="slug">The blog post slug</param>
    /// <param name="commentId">The comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if comment was marked as spam successfully</returns>
    Task<bool> MarkCommentAsSpamAsync(
        string slug,
        string commentId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// View model for displaying comments with hierarchical structure
/// </summary>
public class CommentViewModel
{
    /// <summary>
    /// Comment ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Author name
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Author website URL (optional)
    /// </summary>
    public string? AuthorWebsite { get; set; }

    /// <summary>
    /// Comment content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the comment was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the comment was updated (if edited)
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Whether the comment was edited
    /// </summary>
    public bool IsEdited => UpdatedAt.HasValue;

    /// <summary>
    /// Parent comment ID (for threaded replies)
    /// </summary>
    public string? ParentCommentId { get; set; }

    /// <summary>
    /// Nested replies to this comment
    /// </summary>
    public List<CommentViewModel> Replies { get; set; } = new();

    /// <summary>
    /// User ID if the comment was by a registered user
    /// </summary>
    public string? UserId { get; set; }
}
