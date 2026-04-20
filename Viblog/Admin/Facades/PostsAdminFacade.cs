using System.Linq.Expressions;
using System.Security.Claims;
using Viblog.Infrastructure.Auditing;
using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Infrastructure.Data.Repositories;
using Viblog.Infrastructure.Facades;
using Viblog.Shared.Services.Content;

namespace Viblog.Admin.Facades;

/// <summary>
/// Facade implementation for admin post management operations
/// </summary>
public class PostsAdminFacade : IPostsAdminFacade
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly ContentSchedulingService? _schedulingService;
    private readonly IAuditLogService? _auditLogService;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<PostsAdminFacade> _logger;

    public PostsAdminFacade(
        IBlogPostRepository blogPostRepository,
        ILogger<PostsAdminFacade> logger,
        ContentSchedulingService? schedulingService = null,
        IAuditLogService? auditLogService = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _blogPostRepository = blogPostRepository ?? throw new ArgumentNullException(nameof(blogPostRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schedulingService = schedulingService; // Optional
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
                predicate, pagingParameters, p => p.Draft.Title, ascending, false, cancellationToken),

            PostSortField.Slug => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.Slug, ascending, false, cancellationToken),

            PostSortField.PublishedAt => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.PublishedAt, ascending, false, cancellationToken),

            PostSortField.IsFeatured => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.IsFeatured, ascending, false, cancellationToken),

            PostSortField.IsPublished => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.IsPublished, ascending, false, cancellationToken),

            PostSortField.ViewCount => await _blogPostRepository.FindAsync(
                predicate, pagingParameters, p => p.ViewCount, ascending, false, cancellationToken),

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

        if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var user = _httpContextAccessor.HttpContext.User;
            post.AuthorId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            post.AuthorName = user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? string.Empty;
        }

        await _blogPostRepository.AddAsync(post, cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);

        // Log post creation
        await LogAuditAsync(
            AuditAction.ContentCreated,
            post.Id,
            post.Draft.Title,
            $"Created blog post '{post.Draft.Title}'",
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
            AuditAction.ContentUpdated,
            post.Id,
            post.Draft.Title,
            $"Updated blog post '{post.Draft.Title}'",
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
                AuditAction.ContentDeleted,
                post.Id,
                post.Draft.Title,
                $"Deleted blog post '{post.Draft.Title}'",
                cancellationToken);
        }
    }

    /// <inheritdoc/>
    public virtual async Task PublishPostNowAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(_schedulingService, nameof(_schedulingService));

        _logger.LogInformation("PublishPostNowAsync started for post {PostId}", id);

        try
        {
            var post = await _blogPostRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken)
                ?? throw new InvalidOperationException($"Post '{id}' not found.");

            _logger.LogDebug("Post {PostId} loaded. Title={Title}, IsPublished={IsPublished}, GroupKey={GroupKey}",
                post.Id, post.Draft.Title, post.IsPublished, post.GroupKey);

            var (userId, userName, _) = GetCurrentUser();

            _logger.LogDebug("Calling PublishNowAsync for post {PostId} by user {UserId}", id, userId);
            await _schedulingService.PublishNowAsync(post, userId, userName, cancellationToken: cancellationToken);

            _logger.LogDebug("Calling UpdateAsync for post {PostId}", id);
            await _blogPostRepository.UpdateAsync(post, cancellationToken);

            _logger.LogDebug("Calling SaveChangesAsync for post {PostId}", id);
            await _blogPostRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("PublishPostNowAsync completed successfully for post {PostId}", id);

            await LogAuditAsync(
                AuditAction.ContentPublished,
                post.Id,
                post.Draft.Title,
                $"Published BlogPost '{post.Draft.Title}'",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PublishPostNowAsync failed for post {PostId}. Exception: {ExceptionType}: {ExceptionMessage}",
                id, ex.GetType().Name, ex.Message);
            throw new InvalidOperationException($"Failed to publish post '{id}'. {ex.GetType().Name}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task SchedulePostAsync(
        string id,
        DateTimeOffset publishDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(_schedulingService, nameof(_schedulingService));

        var post = await _blogPostRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Post '{id}' not found.");

        var alreadyScheduled = post.Schedule.Status == ContentStatus.Scheduled;

        _schedulingService.ScheduleForPublish(post, publishDate);

        await _blogPostRepository.UpdateAsync(post, cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);

        var auditAction = alreadyScheduled ? AuditAction.ContentScheduleUpdated : AuditAction.ContentScheduled;
        var description = alreadyScheduled
            ? $"Updated schedule for BlogPost '{post.Draft.Title}' to {publishDate:u}"
            : $"Scheduled BlogPost '{post.Draft.Title}' for {publishDate:u}";

        await LogAuditAsync(auditAction, post.Id, post.Draft.Title, description, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task CancelPostScheduleAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var post = await _blogPostRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Post '{id}' not found.");

        post.Schedule.Status = ContentStatus.Draft;
        post.Schedule.ScheduledPublishDate = null;

        await _blogPostRepository.UpdateAsync(post, cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);

        await LogAuditAsync(
            AuditAction.ContentScheduleCancelled,
            post.Id,
            post.Draft.Title,
            $"Cancelled schedule for BlogPost '{post.Draft.Title}'",
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task UnpublishPostAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(_schedulingService, nameof(_schedulingService));

        var post = await _blogPostRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Post '{id}' not found.");

        _schedulingService.Unpublish(post);

        await _blogPostRepository.UpdateAsync(post, cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);

        await LogAuditAsync(
            AuditAction.ContentUnpublished,
            post.Id,
            post.Draft.Title,
            $"Unpublished BlogPost '{post.Draft.Title}'",
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task AdoptPostAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var post = await _blogPostRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Post '{id}' not found.");

        var (userId, userName, _) = GetCurrentUser();

        var previousAuthorName = post.AuthorName;
        post.AuthorId = userId;
        post.AuthorName = userName;

        await _blogPostRepository.UpdateAsync(post, cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);

        await LogAuditAsync(
            AuditAction.ContentOwnershipTransferred,
            post.Id,
            post.Draft.Title,
            $"Ownership of BlogPost '{post.Draft.Title}' transferred from {previousAuthorName} to {userName}.",
            cancellationToken);
    }

    /// <summary>
    /// Reads the current user's identity from the HTTP context.
    /// </summary>
    private (string userId, string userName, string userEmail) GetCurrentUser()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        return (
            user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
            user?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User",
            user?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@email.com"
        );
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

        var (userId, userName, userEmail) = GetCurrentUser();

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
