using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Viblog.Infrastructure.Admin.Facades;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Shared.Services.Content;

namespace Viblog.Admin.Facades;

/// <summary>
/// Facade implementation for admin page management operations
/// </summary>
public class PagesAdminFacade : IPagesAdminFacade
{
    private readonly IPageRepository _pageRepository;
    private readonly ContentSchedulingService? _schedulingService;
    private readonly IAuditLogService? _auditLogService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public PagesAdminFacade(
        IPageRepository pageRepository,
        ContentSchedulingService? schedulingService = null,
        IAuditLogService? auditLogService = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _pageRepository = pageRepository ?? throw new ArgumentNullException(nameof(pageRepository));
        _schedulingService = schedulingService; // Optional
        _auditLogService = auditLogService; // Optional
        _httpContextAccessor = httpContextAccessor; // Optional
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<Page>> GetPagesAsync(
        PagingParameters pagingParameters,
        bool? publishedOnly = null,
        PageSortField sortField = PageSortField.Slug,
        bool ascending = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        // Build the predicate based on the filter
        Expression<Func<Page, bool>> predicate = publishedOnly switch
        {
            true => p => p.IsPublished,
            false => p => true, // Not restricting to published only — include all
            null => p => true // All pages
        };

        // Use the appropriate sort expression based on the sort field
        return sortField switch
        {
            PageSortField.CreatedAt => await _pageRepository.FindAsync(
                predicate, pagingParameters, p => p.CreatedAt, ascending, false, cancellationToken),

            PageSortField.UpdatedAt => await _pageRepository.FindAsync(
                predicate, pagingParameters, p => p.UpdatedAt, ascending, false, cancellationToken),

            PageSortField.IsPublished => await _pageRepository.FindAsync(
                predicate, pagingParameters, p => p.IsPublished, ascending, false, cancellationToken),

            PageSortField.Slug or _ => await _pageRepository.FindAsync(
                predicate, pagingParameters, p => p.Slug, ascending, false, cancellationToken)
        };
    }

    /// <inheritdoc/>
    public virtual async Task<Page?> GetPageByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return await _pageRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task CreatePageAsync(
        Page page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var user = _httpContextAccessor.HttpContext.User;
            page.AuthorId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            page.AuthorName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }

        // Check if slug already exists
        await ValidateUniqueSlugAsync(page.Slug, null, cancellationToken);

        await _pageRepository.AddAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);

        // Log page creation
        await LogAuditAsync(
            AuditAction.ContentCreated,
            page.Id,
            page.Slug,
            $"Created page '{page.Slug}'",
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task UpdatePageAsync(
        Page page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);

        // Check if slug already exists (excluding current page)
        await ValidateUniqueSlugAsync(page.Slug, page.Id, cancellationToken);

        await _pageRepository.UpdateAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);

        // Log page update
        await LogAuditAsync(
            AuditAction.ContentUpdated,
            page.Id,
            page.Slug,
            $"Updated page '{page.Slug}'",
            cancellationToken);
    }

    /// <summary>
    /// Validates that a slug is unique across all pages
    /// </summary>
    /// <param name="slug">The slug to validate</param>
    /// <param name="excludePageId">Optional page ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if slug already exists</exception>
    private async Task ValidateUniqueSlugAsync(
        string slug,
        string? excludePageId,
        CancellationToken cancellationToken)
    {
        // Check all pages (published or not) to ensure slug is unique
        var existingPage = await _pageRepository.GetBySlugAsync(slug, publishedOnly: false, cancellationToken);
        
        if (existingPage != null && existingPage.Id != excludePageId)
        {
            throw new InvalidOperationException($"A page with the slug '{slug}' already exists. Please choose a different slug.");
        }
    }

    /// <inheritdoc/>
    public virtual async Task DeletePageAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        // Get page info before deletion for audit log
        var page = await _pageRepository.GetByIdAsync(id, partitionKey, cancellationToken);

        await _pageRepository.DeleteAsync(id, partitionKey, softDelete: true, cancellationToken: cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);

        // Log page deletion
        if (page != null)
        {
            await LogAuditAsync(
                AuditAction.ContentDeleted,
                page.Id,
                page.Slug,
                $"Deleted page '{page.Slug}'",
                cancellationToken);
        }
    }

    /// <inheritdoc/>
    public virtual async Task PublishPageNowAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(_schedulingService, nameof(_schedulingService));

        var page = await _pageRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Page '{id}' not found.");

        var (userId, userName, _) = GetCurrentUser();

        await _schedulingService.PublishNowAsync(page, userId, userName, cancellationToken: cancellationToken);

        await _pageRepository.UpdateAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);

        await LogAuditAsync(
            AuditAction.ContentPublished,
            page.Id,
            page.Slug,
            $"Published Page '{page.Slug}'",
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task SchedulePagePublishingAsync(
        string id,
        DateTimeOffset publishDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(_schedulingService, nameof(_schedulingService));

        var page = await _pageRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Page '{id}' not found.");

        var alreadyScheduled = page.Schedule.Status == ContentStatus.Scheduled;

        _schedulingService.ScheduleForPublish(page, publishDate);

        await _pageRepository.UpdateAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);

        var auditAction = alreadyScheduled ? AuditAction.ContentScheduleUpdated : AuditAction.ContentScheduled;
        var description = alreadyScheduled
            ? $"Updated schedule for Page '{page.Slug}' to {publishDate:u}"
            : $"Scheduled Page '{page.Slug}' for {publishDate:u}";

        await LogAuditAsync(auditAction, page.Id, page.Slug, description, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task UnpublishPageAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(_schedulingService, nameof(_schedulingService));

        var page = await _pageRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Page '{id}' not found.");

        _schedulingService.Unpublish(page);

        await _pageRepository.UpdateAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);

        await LogAuditAsync(
            AuditAction.ContentUnpublished,
            page.Id,
            page.Slug,
            $"Unpublished Page '{page.Slug}'",
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task CancelPageScheduleAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var page = await _pageRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Page '{id}' not found.");

        page.Schedule.Status = ContentStatus.Draft;
        page.Schedule.ScheduledPublishDate = null;

        await _pageRepository.UpdateAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);

        await LogAuditAsync(
            AuditAction.ContentScheduleCancelled,
            page.Id,
            page.Slug,
            $"Cancelled schedule for Page '{page.Slug}'",
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task AdoptPageAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var page = await _pageRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Page '{id}' not found.");

        var (userId, userName, _) = GetCurrentUser();

        var previousAuthorName = page.AuthorName;
        page.AuthorId = userId;
        page.AuthorName = userName;

        await _pageRepository.UpdateAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);

        await LogAuditAsync(
            AuditAction.ContentOwnershipTransferred,
            page.Id,
            page.Slug,
            $"Ownership of Page '{page.Slug}' transferred from {previousAuthorName} to {userName}.",
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
            entityType: EntityType.Page,
            entityId: entityId,
            entityName: entityName,
            description: description,
            result: ActionResult.Success,
            cancellationToken: cancellationToken);
    }
}
