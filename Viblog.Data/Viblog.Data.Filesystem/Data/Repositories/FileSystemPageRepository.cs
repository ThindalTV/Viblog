using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Data.Filesystem.Configuration;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.Filesystem.Data.Repositories;

/// <summary>
/// Filesystem-based repository implementation for page operations
/// </summary>
public class FileSystemPageRepository : FilesystemRepository<Page>, IPageRepository
{
    public FileSystemPageRepository(
        IOptions<FilesystemStorageOptions> options,
        ILogger<FilesystemRepository<Page>> logger)
        : base(options, logger)
    {
    }

    /// <inheritdoc/>
    public virtual async Task<Page?> GetBySlugAsync(
        string slug,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var page = await FirstOrDefaultAsync(
            p => p.Slug == slug &&
                 (!publishedOnly || p.IsPublished),
            includeDeleted: false,
            cancellationToken);

        // Promote scheduled draft if ready
        if (page != null && page.PromoteDraftIfScheduled())
        {
            await SaveEntityAsync(page, cancellationToken);
        }

        return page;
    }

    /// <inheritdoc/>
    public virtual async Task<Page?> GetByIdWithoutPartitionKeyAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        await _indexManager.LoadIndexAsync(cancellationToken);

        // Search through all entries to find matching ID
        var entry = _indexManager.GetAllEntries()
            .FirstOrDefault(e => e.Id == id && !e.IsDeleted);

        if (entry is null)
            return null;

        var filePath = Path.Combine(_entityDirectory, entry.FileName);
        var page = await ReadEntityFromFileAsync(filePath, cancellationToken);

        // Promote scheduled draft if ready
        if (page != null && page.PromoteDraftIfScheduled())
        {
            await SaveEntityAsync(page, cancellationToken);
        }

        return page;
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<Page>> GetPagesAsync(
        PagingParameters pagingParameters,
        bool publishedOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await FindAsync(
            p => !publishedOnly || p.IsPublished,
            pagingParameters,
            p => p.Slug,
            ascending: true,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task IncrementViewCountAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        var page = await GetByIdAsync(id, partitionKey, cancellationToken);
        if (page is not null)
        {
            page.ViewCount++;
            page.UpdatedAt = DateTimeOffset.UtcNow;
            await SaveEntityAsync(page, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<Page>> GetScheduledPagesReadyToPublishAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await FindAsync(
            p => p.PublishDate.HasValue && p.PublishDate.Value <= DateTimeOffset.UtcNow,
            new PagingParameters(1, int.MaxValue),
            p => p.PublishDate,
            ascending: true,
            includeDeleted: false,
            cancellationToken);

        return result.Items;
    }

    /// <inheritdoc/>
    public virtual async Task<int> PromoteScheduledPagesAsync(
        CancellationToken cancellationToken = default)
    {
        var pagesToPromote = await GetScheduledPagesReadyToPublishAsync(cancellationToken);
        var promotedCount = 0;

        foreach (var page in pagesToPromote)
        {
            if (page.PromoteDraftIfScheduled())
            {
                await SaveEntityAsync(page, cancellationToken);
                promotedCount++;
            }
        }

        return promotedCount;
    }
}
