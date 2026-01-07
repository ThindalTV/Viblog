using Viblog.Infrastructure.Frontend.Facades;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Frontend.Facades;

/// <summary>
/// Facade implementation for comment operations
/// </summary>
public class CommentFacade : ICommentFacade
{
    private readonly IBlogPostRepository _blogPostRepository;

    public CommentFacade(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CommentViewModel>> GetApprovedCommentsAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Enumerable.Empty<CommentViewModel>();
        }

        var post = await _blogPostRepository.GetBySlugAsync(slug, publishedOnly: true, cancellationToken);
        if (post == null)
        {
            return Enumerable.Empty<CommentViewModel>();
        }

        var approvedComments = await _blogPostRepository.GetApprovedCommentsAsync(
            post.Id,
            post.PartitionKey,
            cancellationToken);

        return BuildCommentHierarchy(approvedComments);
    }

    /// <inheritdoc/>
    public async Task<bool> AddCommentAsync(
        string slug,
        string authorName,
        string authorEmail,
        string? authorWebsite,
        string content,
        string? parentCommentId,
        string? ipAddress,
        string? userAgent,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug) ||
            string.IsNullOrWhiteSpace(authorName) ||
            string.IsNullOrWhiteSpace(authorEmail) ||
            string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var post = await _blogPostRepository.GetBySlugAsync(slug, publishedOnly: true, cancellationToken);
        if (post == null || !post.AllowComments)
        {
            return false;
        }

        var comment = new Comment
        {
            UserId = userId,
            AuthorName = authorName,
            AuthorEmail = authorEmail,
            AuthorWebsite = authorWebsite,
            Content = content,
            ParentCommentId = parentCommentId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsApproved = false, // Require moderation by default
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _blogPostRepository.AddCommentAsync(
            post.Id,
            post.PartitionKey,
            comment,
            cancellationToken);

        return result != null;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateCommentAsync(
        string slug,
        string commentId,
        string content,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug) ||
            string.IsNullOrWhiteSpace(commentId) ||
            string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var post = await _blogPostRepository.GetBySlugAsync(slug, publishedOnly: false, cancellationToken);
        if (post == null)
        {
            return false;
        }

        var existingComment = post.Comments.FirstOrDefault(c => c.Id == commentId);
        if (existingComment == null)
        {
            return false;
        }

        // Only allow update by the comment author or an admin
        if (!string.IsNullOrEmpty(userId) && existingComment.UserId != userId)
        {
            // This should check for admin role, but for now just check ownership
            return false;
        }

        var updatedComment = new Comment
        {
            Content = content
        };

        var result = await _blogPostRepository.UpdateCommentAsync(
            post.Id,
            post.PartitionKey,
            commentId,
            updatedComment,
            cancellationToken);

        return result != null;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteCommentAsync(
        string slug,
        string commentId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(commentId))
        {
            return false;
        }

        var post = await _blogPostRepository.GetBySlugAsync(slug, publishedOnly: false, cancellationToken);
        if (post == null)
        {
            return false;
        }

        var existingComment = post.Comments.FirstOrDefault(c => c.Id == commentId);
        if (existingComment == null)
        {
            return false;
        }

        // Only allow deletion by the comment author or an admin
        if (!string.IsNullOrEmpty(userId) && existingComment.UserId != userId)
        {
            // This should check for admin role, but for now just check ownership
            return false;
        }

        var result = await _blogPostRepository.DeleteCommentAsync(
            post.Id,
            post.PartitionKey,
            commentId,
            cancellationToken);

        return result != null;
    }

    /// <inheritdoc/>
    public async Task<bool> ApproveCommentAsync(
        string slug,
        string commentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(commentId))
        {
            return false;
        }

        var post = await _blogPostRepository.GetBySlugAsync(slug, publishedOnly: false, cancellationToken);
        if (post == null)
        {
            return false;
        }

        var result = await _blogPostRepository.ApproveCommentAsync(
            post.Id,
            post.PartitionKey,
            commentId,
            cancellationToken);

        return result != null;
    }

    /// <inheritdoc/>
    public async Task<bool> MarkCommentAsSpamAsync(
        string slug,
        string commentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(commentId))
        {
            return false;
        }

        var post = await _blogPostRepository.GetBySlugAsync(slug, publishedOnly: false, cancellationToken);
        if (post == null)
        {
            return false;
        }

        var result = await _blogPostRepository.MarkCommentAsSpamAsync(
            post.Id,
            post.PartitionKey,
            commentId,
            cancellationToken);

        return result != null;
    }

    /// <summary>
    /// Build hierarchical comment structure from flat list
    /// </summary>
    private static IEnumerable<CommentViewModel> BuildCommentHierarchy(IEnumerable<Comment> comments)
    {
        var commentList = comments.ToList();
        var commentDict = commentList.ToDictionary(c => c.Id);
        var rootComments = new List<CommentViewModel>();

        foreach (var comment in commentList)
        {
            var viewModel = MapToViewModel(comment);

            if (string.IsNullOrEmpty(comment.ParentCommentId))
            {
                // Top-level comment
                rootComments.Add(viewModel);
            }
            else if (commentDict.TryGetValue(comment.ParentCommentId, out var parentComment))
            {
                // Find parent in already-created view models and add as reply
                var parentViewModel = FindCommentInHierarchy(rootComments, comment.ParentCommentId);
                parentViewModel?.Replies.Add(viewModel);
            }
        }

        return rootComments;
    }

    /// <summary>
    /// Find a comment view model in the hierarchy by ID
    /// </summary>
    private static CommentViewModel? FindCommentInHierarchy(IEnumerable<CommentViewModel> comments, string commentId)
    {
        foreach (var comment in comments)
        {
            if (comment.Id == commentId)
            {
                return comment;
            }

            var found = FindCommentInHierarchy(comment.Replies, commentId);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Map Comment entity to CommentViewModel
    /// </summary>
    private static CommentViewModel MapToViewModel(Comment comment)
    {
        return new CommentViewModel
        {
            Id = comment.Id,
            AuthorName = comment.AuthorName,
            AuthorWebsite = comment.AuthorWebsite,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            ParentCommentId = comment.ParentCommentId,
            UserId = comment.UserId
        };
    }
}
