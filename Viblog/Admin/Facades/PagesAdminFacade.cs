using System.Linq.Expressions;
using Viblog.Infrastructure.Admin.Facades;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Admin.Facades;

/// <summary>
/// Facade implementation for admin page management operations
/// </summary>
public class PagesAdminFacade : IPagesAdminFacade
{
    private readonly IPageRepository _pageRepository;

    public PagesAdminFacade(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository ?? throw new ArgumentNullException(nameof(pageRepository));
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
            false => p => !p.IsPublished,
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

        await _pageRepository.AddAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task UpdatePageAsync(
        Page page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);

        await _pageRepository.UpdateAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task DeletePageAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        await _pageRepository.DeleteAsync(id, partitionKey, softDelete: true, cancellationToken: cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task PublishPageNowAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var page = await _pageRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken);
        if (page == null)
        {
            throw new InvalidOperationException($"Page with ID '{id}' not found.");
        }

        page.PublishDraftNow();
        await _pageRepository.UpdateAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task SchedulePagePublishingAsync(
        string id,
        DateTimeOffset publishDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var page = await _pageRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken);
        if (page == null)
        {
            throw new InvalidOperationException($"Page with ID '{id}' not found.");
        }

        page.PublishDate = publishDate;
        await _pageRepository.UpdateAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task UnpublishPageAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var page = await _pageRepository.GetByIdWithoutPartitionKeyAsync(id, cancellationToken);
        if (page == null)
        {
            throw new InvalidOperationException($"Page with ID '{id}' not found.");
        }

        page.IsPublished = false;
        page.PublishDate = null;
        await _pageRepository.UpdateAsync(page, cancellationToken);
        await _pageRepository.SaveChangesAsync(cancellationToken);
    }
}
