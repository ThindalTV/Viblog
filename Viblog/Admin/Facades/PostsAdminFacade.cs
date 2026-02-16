using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Viblog.Infrastructure.Admin.Facades;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Admin.Facades;

/// <summary>
/// Facade implementation for admin post management operations
/// </summary>
public class PostsAdminFacade : IPostsAdminFacade
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly IAuditLogService? _auditLogService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public PostsAdminFacade(
        IBlogPostRepository blogPostRepository,
        IAuditLogService? auditLogService = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _blogPostRepository = blogPostRepository ?? throw new ArgumentNullException(nameof(blogPostRepository));
        _auditLogService = auditLogService; // Optional
        _httpContextAccessor = httpContextAccessor; // Optional
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPostsAsync(
        PagingParameters pagingParameters,
        bool? publishedOnly = null,
        PostSortField sortField = PostSortField.CreatedAt,
        bool ascending = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        // Build the predicate based on the filter
        Expression<Func<BlogPost, bool>> predicate = publishedOnly switch
        {
            true => p => p.IsPublished,
            false => p => !p.IsPublished,
            null => p => true // All posts
        };

        // Use the appropriate sort expression based on the sort field
        return sortField switch
        {
            PostSortField.Title => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.Title, ascending, false, cancellationToken),
            
            PostSortField.Slug => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.Slug, ascending, false, cancellationToken),
            
            PostSortField.PublishedAt => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.PublishedAt, ascending, false, cancellationToken),
            
            PostSortField.IsFeatured => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.IsFeatured, ascending, false, cancellationToken),
            
            PostSortField.IsPublished => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.IsPublished, ascending, false, cancellationToken),
            
            PostSortField.CreatedAt or _ => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.CreatedAt, ascending, false, cancellationToken)
        };
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> GetPostByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return await _blogPostRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task CreatePostAsync(
        BlogPost post,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        await _blogPostRepository.AddAsync(post, cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);

        // Log post creation
        await LogAuditAsync(
            AuditAction.PostCreated,
            post.Id,
            post.Title,
            $"Created blog post '{post.Title}'",
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task UpdatePostAsync(
        BlogPost post,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        await _blogPostRepository.UpdateAsync(post, cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);

        // Log post update
        await LogAuditAsync(
            AuditAction.PostUpdated,
            post.Id,
            post.Title,
            $"Updated blog post '{post.Title}'",
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task DeletePostAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        // Get post info before deletion for audit log
        var post = await _blogPostRepository.GetByIdAsync(id, partitionKey, cancellationToken);

        await _blogPostRepository.DeleteAsync(id, partitionKey, softDelete: true, cancellationToken: cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);

        // Log post deletion
        if (post != null)
        {
            await LogAuditAsync(
                AuditAction.PostDeleted,
                post.Id,
                post.Title,
                $"Deleted blog post '{post.Title}'",
                cancellationToken);
        }
    }

    /// <summary>
    /// Helper method to log audit entries
    /// </summary>
    private async Task LogAuditAsync(
        AuditAction action,
        string entityId,
        string entityName,
        string description,
        CancellationToken cancellationToken)
    {
        if (_auditLogService == null || _httpContextAccessor?.HttpContext == null)
        {
            return;
        }

        var user = _httpContextAccessor.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
        var userEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@email.com";

        await _auditLogService.LogActionAsync(
            userId: userId,
            userName: userName,
            userEmail: userEmail,
            action: action,
            entityType: EntityType.BlogPost,
            entityId: entityId,
            entityName: entityName,
            description: description,
            result: ActionResult.Success,
            cancellationToken: cancellationToken);
    }
}
