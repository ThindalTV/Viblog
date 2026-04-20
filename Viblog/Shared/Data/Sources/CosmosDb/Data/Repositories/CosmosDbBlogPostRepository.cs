using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Infrastructure.Data.Repositories;

namespace Viblog.Shared.Data.Sources.CosmosDb.Data.Repositories;

/// <summary>
/// CosmosDB-specific repository implementation for blog post operations
/// </summary>
public class CosmosDbBlogPostRepository : CosmosDbRepository<BlogPost>, IBlogPostRepository
{
    private readonly ILogger<CosmosDbBlogPostRepository> _logger;

    public CosmosDbBlogPostRepository(ApplicationDbContext context, ILogger<CosmosDbBlogPostRepository> logger) : base(context)
    {
        _logger = logger;
    }


    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPublishedPostsAsync(
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(p => !p.IsDeleted && p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow);

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPostsByCategoryAsync(
        string categoryId,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(p => !p.IsDeleted && p.CategoryIds.Contains(categoryId));

        if (publishedOnly)
        {
            query = query.Where(p => p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow);
        }

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPostsByTagAsync(
        string tag,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(p => !p.IsDeleted && p.Tags.Contains(tag));

        if (publishedOnly)
        {
            query = query.Where(p => p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow);
        }

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetFeaturedPostsAsync(
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(p => !p.IsDeleted && p.IsFeatured);

        if (publishedOnly)
        {
            query = query.Where(p => p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow);
        }

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> GetBySlugAsync(
        string slug,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var query = _dbSet
            .Where(p => !p.IsDeleted && p.Slug == slug);

        if (publishedOnly)
        {
            query = query.Where(p => p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> GetByIdWithoutPartitionKeyAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return await _dbSet
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPostsByAuthorAsync(
        string authorId,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorId);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        var query = _dbSet
            .Where(p => !p.IsDeleted && p.AuthorId == authorId);

        if (publishedOnly)
        {
            query = query.Where(p => p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow);
        }

        return await ApplyPagingAndSortingAsync(
            query,
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
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

        try
        {
            _logger.LogDebug("Incrementing view count for BlogPost {PostId} in partition {PartitionKey}", id, partitionKey);
            
            var post = await _dbSet
                .WithPartitionKey(partitionKey)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            if (post != null)
            {
                post.ViewCount++;
                post.UpdatedAt = DateTimeOffset.UtcNow;
                _dbSet.Update(post);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Successfully incremented view count for BlogPost {PostId}. New count: {ViewCount}", id, post.ViewCount);
            }
            else
            {
                _logger.LogWarning("BlogPost {PostId} not found in partition {PartitionKey} for view count increment. " +
                                  "Attempting cross-partition fallback.", id, partitionKey);
                
                // Try fallback cross-partition query
                var postFallback = await _dbSet
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
                
                if (postFallback != null)
                {
                    postFallback.ViewCount++;
                    postFallback.UpdatedAt = DateTimeOffset.UtcNow;
                    _dbSet.Update(postFallback);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Cross-partition fallback succeeded for BlogPost {PostId}. Found in partition {ActualPartitionKey}. New count: {ViewCount}",
                        id, postFallback.GroupKey, postFallback.ViewCount);
                }
                else
                {
                    var warnMsg = $"BlogPost {id} not found in any partition. View count increment skipped. " +
                                 $"Attempted partition: {partitionKey}";
                    _logger.LogWarning(warnMsg);
                }
            }
        }
        catch (Exception ex)
        {
            var diagnosticMsg = $"Error incrementing view count for BlogPost {id} in partition {partitionKey}. " +
                               $"Exception: {ex.GetType().Name}: {ex.Message}";
            _logger.LogError(ex, diagnosticMsg);
            
            // Log but don't throw - view count increment is non-critical
            // This prevents a non-critical operation from breaking the request
            _logger.LogWarning("View count increment failed but continuing operation. " +
                              "BlogPost {PostId} may have incorrect view count. Details: {DiagnosticMessage}",
                id, diagnosticMsg);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPostsByMonthAsync(
        int year,
        int month,
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = startDate.AddMonths(1);

        return await FindAsync(
            p => (!publishedOnly || p.IsPublished) &&
                 p.PublishedAt >= startDate &&
                 p.PublishedAt < endDate,
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<BlogPost>> GetRelatedPostsAsync(
        BlogPost post,
        int maxPosts = 5,
        CancellationToken cancellationToken = default)
    {
        if (post?.Tags == null || !post.Tags.Any())
        {
            return [];
        }

        var postId = post.Id;
        var postTags = post.Tags;

        // Fetch published posts (excluding this one) server-side, then filter tag overlap
        // client-side — CosmosDB EF cannot translate nested Any/Contains over owned entities.
        var candidates = await _dbSet
            .Where(p => !p.IsDeleted && p.IsPublished && p.Id != postId)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(p => p.Tags.Any(tag => postTags.Contains(tag)))
            .OrderByDescending(p => p.PublishedAt)
            .Take(maxPosts);
    }

    /// <inheritdoc/>
    public virtual async Task<(BlogPost? previous, BlogPost? next)> GetAdjacentPostsAsync(
        DateTimeOffset publishedAt,
        CancellationToken cancellationToken = default)
    {
        // Fetch a small batch before and after the reference date (date filter is server-side).
        // Content check is done client-side — CosmosDB EF cannot translate string operations on nested objects.
        var beforeBatch = await _dbSet
            .Where(p => !p.IsDeleted && p.IsPublished && p.PublishedAt < publishedAt && p.PublishedAt <= DateTimeOffset.UtcNow)
            .OrderByDescending(p => p.PublishedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var afterBatch = await _dbSet
            .Where(p => !p.IsDeleted && p.IsPublished && p.PublishedAt > publishedAt && p.PublishedAt <= DateTimeOffset.UtcNow)
            .OrderBy(p => p.PublishedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var previous = beforeBatch.FirstOrDefault(p => p.Live != null && !string.IsNullOrWhiteSpace(p.Live.Markdown));
        var next = afterBatch.FirstOrDefault(p => p.Live != null && !string.IsNullOrWhiteSpace(p.Live.Markdown));

        return (previous, next);
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> UpdatePublicationDateAsync(
        string postId,
        string currentPartitionKey,
        DateTimeOffset newPublishedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPartitionKey);

        var post = await _dbSet.FirstOrDefaultAsync(
            p => p.Id == postId && p.GroupKey == currentPartitionKey,
            cancellationToken);

        if (post == null)
            return null;

        post.PublishedAt = newPublishedAt;
        await UpdateAsync(post, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return post;
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<BlogPost>> GetScheduledPostsReadyToPublishAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _dbSet
            .Where(p => !p.IsDeleted &&
                        p.Schedule.Status == ContentStatus.Scheduled &&
                        p.Schedule.ScheduledPublishDate <= now)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("SaveChangesAsync called for BlogPost context. Pending changes: {ChangeCount}",
            _context.ChangeTracker.Entries().Count(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged && e.State != Microsoft.EntityFrameworkCore.EntityState.Detached));

        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("SaveChangesAsync completed successfully. Rows affected: {RowsAffected}", result);
            return result;
        }
        catch (Exception ex)
        {
            var diagnosticMsg = $"SaveChangesAsync failed. Exception: {ex.GetType().Name}: {ex.Message}";
            _logger.LogError(ex, "SaveChangesAsync failed for BlogPost context. Exception: {ExceptionType}: {ExceptionMessage}",
                ex.GetType().Name, ex.Message);

            // Log tracked entity state to help diagnose partition key / CosmosDB issues
            foreach (var entry in _context.ChangeTracker.Entries<BlogPost>())
            {
                _logger.LogError("Tracked BlogPost entry during failed save: Id={PostId}, State={State}, GroupKey={GroupKey}, IsPublished={IsPublished}, PublishedAt={PublishedAt}",
                    entry.Entity.Id, entry.State, entry.Entity.GroupKey, entry.Entity.IsPublished, entry.Entity.PublishedAt);
            }

            throw new InvalidOperationException(diagnosticMsg, ex);
        }
    }
}
