using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Infrastructure.Data.Repositories;
using Viblog.Shared.Data.Sources.CosmosDb.Data.Entities;

namespace Viblog.Shared.Data.Sources.CosmosDb.Data.Repositories;

/// <summary>
/// CosmosDB-specific repository implementation for blog post operations
/// </summary>
public class CosmosDbBlogPostRepository : CosmosDbRepository<BlogPost>, IBlogPostRepository
{
    private readonly ILogger<CosmosDbBlogPostRepository> _logger;

    public CosmosDbBlogPostRepository(ApplicationDbContext context, ILogger<CosmosDbBlogPostRepository> logger) : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public override async Task AddAsync(BlogPost entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            _logger.LogDebug("Adding BlogPost {PostId}. State: IsPublished={IsPublished}, PublishedAt={PublishedAt}, Slug={Slug}",
                entity.Id, entity.IsPublished, entity.PublishedAt, entity.Slug);
            
            entity.SetPartitionKey(); // Ensure partition key is set based on publication date
            _logger.LogDebug("Set partition key for BlogPost {PostId}: {PartitionKey}", entity.Id, entity.GroupKey);
            
            await base.AddAsync(entity, cancellationToken);
            _logger.LogDebug("Successfully added BlogPost {PostId} to {PartitionKey}", entity.Id, entity.GroupKey);
        }
        catch (Exception ex)
        {
            var diagnosticMsg = $"Error adding BlogPost {entity.Id}. " +
                               $"State: IsPublished={entity.IsPublished}, PublishedAt={entity.PublishedAt}, " +
                               $"Slug={entity.Slug}, PartitionKey={entity.GroupKey}. " +
                               $"Exception: {ex.GetType().Name}: {ex.Message}";
            _logger.LogError(ex, diagnosticMsg);
            throw new InvalidOperationException(diagnosticMsg, ex);
        }
    }

    /// <inheritdoc/>
    public override async Task AddRangeAsync(IEnumerable<BlogPost> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        try
        {
            var entityList = entities.ToList();
            _logger.LogDebug("Adding {Count} BlogPosts. IDs: {PostIds}", 
                entityList.Count, string.Join(", ", entityList.Select(e => e.Id)));
            
            foreach (var entity in entityList)
            {
                try
                {
                    entity.SetPartitionKey(); // Ensure partition key is set based on publication date
                    _logger.LogDebug("Set partition key for BlogPost {PostId}: {PartitionKey}", entity.Id, entity.GroupKey);
                }
                catch (Exception ex)
                {
                    var diagnosticMsg = $"Error setting partition key for BlogPost {entity.Id}. " +
                                       $"State: IsPublished={entity.IsPublished}, PublishedAt={entity.PublishedAt}, " +
                                       $"Slug={entity.Slug}. " +
                                       $"Exception: {ex.GetType().Name}: {ex.Message}";
                    _logger.LogError(ex, diagnosticMsg);
                    throw new InvalidOperationException(diagnosticMsg, ex);
                }
            }

            await base.AddRangeAsync(entityList, cancellationToken);
            _logger.LogInformation("Successfully added {Count} BlogPosts to CosmosDB", entityList.Count);
        }
        catch (Exception ex)
        {
            var diagnosticMsg = $"Error adding batch of BlogPosts. " +
                               $"Exception: {ex.GetType().Name}: {ex.Message}";
            _logger.LogError(ex, diagnosticMsg);
            throw new InvalidOperationException(diagnosticMsg, ex);
        }
    }

    /// <inheritdoc/>
    public override async Task UpdateAsync(BlogPost entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var originalGroupKey = entity.GroupKey;

        // If GroupKey is empty or null, the entity was likely loaded without partition key context.
        // Recalculate the current partition key based on the entity's state BEFORE any changes.
        // This ensures we have the correct original partition key for comparison.
        if (string.IsNullOrEmpty(originalGroupKey))
        {
            var diagnosticMsg = $"BlogPost {entity.Id} has empty GroupKey. Recalculating based on entity state. " +
                               $"IsPublished={entity.IsPublished}, PublishedAt={entity.PublishedAt}";
            _logger.LogWarning(diagnosticMsg);
            
            // Temporarily determine what the current partition key should be
            if (entity.IsPublished && entity.PublishedAt.HasValue)
            {
                originalGroupKey = entity.PublishedAt.Value.Year.ToString();
            }
            else
            {
                originalGroupKey = "draft";
            }
            _logger.LogInformation("Recalculated partition key for BlogPost {PostId}: {PartitionKey}", entity.Id, originalGroupKey);
        }

        // Detach via Entry() BEFORE SetPartitionKey() mutates GroupKey.
        // Entry() does not trigger DetectChanges; ChangeTracker.Entries<T>() does.
        // If the entity is not tracked this is a no-op.
        _context.Entry(entity).State = EntityState.Detached;

        entity.SetPartitionKey(); // Safe to mutate now that entity is detached

        if (entity.GroupKey == originalGroupKey)
        {
            // Partition key unchanged — base.UpdateAsync re-attaches and updates normally.
            _logger.LogDebug("Partition key unchanged for BlogPost {PostId}: {PartitionKey}. Using standard update.", entity.Id, entity.GroupKey);
            await base.UpdateAsync(entity, cancellationToken);
            return;
        }

        // Partition key changed (e.g. publish/unpublish moves between "draft" and year partition).
        // CosmosDB does not allow updating partition keys in place — delete old document then reinsert.
        _logger.LogInformation("Partition key changed for BlogPost {PostId}: {OldKey} → {NewKey}. Performing delete and reinsert.",
            entity.Id, originalGroupKey, entity.GroupKey);
        
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        // Phase 1: delete using the original partition key.
        // Load the document fresh so EF Core has its full tracking metadata (__jObject / _etag).
        // Reusing the already-detached entity loses that metadata and causes a 404 in CosmosDB.
        var newGroupKey = entity.GroupKey;
        
        // Try to load from the original partition. If not found, try searching all partitions as fallback.
        BlogPost? toDelete = null;
        string? fallbackAttemptInfo = null;
        
        try
        {
            toDelete = await LoadByPartitionKeyForDeleteAsync(entity.Id, originalGroupKey, cancellationToken);
        }
        catch (ArgumentException)
        {
            // Partition key was invalid, rethrow
            throw;
        }
        catch (Exception ex)
        {
            // Any other error loading from the original partition, try a cross-partition query
            // as a fallback to find the document in case it's in an unexpected partition
            fallbackAttemptInfo = $"Primary partition query failed: {ex.GetType().Name}: {ex.Message}. ";
            _logger.LogWarning(ex, "Failed to load BlogPost {PostId} from partition {PartitionKey}. Attempting cross-partition fallback.",
                entity.Id, originalGroupKey);
            
            try
            {
                toDelete = await _dbSet
                    .FirstOrDefaultAsync(e => e.Id == entity.Id && !e.IsDeleted, cancellationToken);
                
                if (toDelete != null)
                {
                    var fallbackSuccess = $"Cross-partition fallback succeeded for BlogPost {entity.Id}. Found in partition {toDelete.GroupKey}";
                    _logger.LogInformation(fallbackSuccess);
                    fallbackAttemptInfo += $"Fallback successful: {fallbackSuccess}";
                }
            }
            catch (Exception fallbackEx)
            {
                // If fallback also fails, log and rethrow the original exception with diagnostic info
                fallbackAttemptInfo += $"Fallback failed: {fallbackEx.GetType().Name}: {fallbackEx.Message}";
                _logger.LogError(fallbackEx, "Cross-partition fallback also failed for BlogPost {PostId}. " +
                                           "Primary failure: {PrimaryException}. Fallback failure: {FallbackException}",
                    entity.Id, ex.Message, fallbackEx.Message);
                
                var diagnosticError = $"Failed to load BlogPost {entity.Id} for partition migration. " +
                                    $"Attempted partition: {originalGroupKey}. " +
                                    $"{fallbackAttemptInfo}";
                throw new InvalidOperationException(diagnosticError, ex);
            }
        }

        if (toDelete == null)
        {
            var errorMsg = $"BlogPost '{entity.Id}' not found at partition key '{originalGroupKey}'. " +
                          $"It may have already been moved or deleted. Post state: " +
                          $"IsPublished={entity.IsPublished}, PublishedAt={entity.PublishedAt}, GroupKey={originalGroupKey}. " +
                          $"{fallbackAttemptInfo}";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        _logger.LogDebug("Deleting BlogPost {PostId} from partition {OldPartitionKey}", entity.Id, originalGroupKey);
        _dbSet.Remove(toDelete);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully deleted BlogPost {PostId} from partition {OldPartitionKey}", entity.Id, originalGroupKey);

        // Phase 2: stage the reinsert with the new partition key.
        // The caller's SaveChangesAsync will commit the insert.
        // Use _dbSet.AddAsync directly to preserve the original CreatedAt.
        entity.GroupKey = newGroupKey;
        _logger.LogDebug("Re-inserting BlogPost {PostId} into partition {NewPartitionKey}", entity.Id, newGroupKey);
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Loads a blog post by id within a specific partition key, used by the
    /// partition-key-change delete path in <see cref="UpdateAsync"/>.
    /// Virtual so tests can override with an InMemory-compatible query
    /// (the InMemory provider does not support <c>WithPartitionKey</c>).
    /// </summary>
    protected virtual async Task<BlogPost?> LoadByPartitionKeyForDeleteAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken)
    {
        // Validate partition key to prevent invalid CosmosDB queries
        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            var error = $"Partition key cannot be null or empty. Entity ID: {id}. " +
                       "This usually indicates a failure to properly calculate the partition key.";
            _logger.LogError(error);
            throw new ArgumentException(error, nameof(partitionKey));
        }

        try
        {
            _logger.LogDebug("Loading BlogPost {PostId} from partition {PartitionKey} for deletion", id, partitionKey);
            var result = await _dbSet
                .WithPartitionKey(partitionKey)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            
            if (result != null)
            {
                _logger.LogDebug("Successfully loaded BlogPost {PostId} from partition {PartitionKey}", id, partitionKey);
            }
            else
            {
                _logger.LogWarning("BlogPost {PostId} not found in partition {PartitionKey}", id, partitionKey);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            var diagnosticMessage = $"Error loading BlogPost {id} from partition {partitionKey}. " +
                                   $"Exception: {ex.GetType().Name}: {ex.Message}";
            _logger.LogError(ex, diagnosticMessage);
            throw new InvalidOperationException(diagnosticMessage, ex);
        }
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
}
