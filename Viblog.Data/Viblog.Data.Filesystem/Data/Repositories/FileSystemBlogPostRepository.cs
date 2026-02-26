using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Data.Filesystem.Configuration;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.Filesystem.Data.Repositories;

/// <summary>
/// Filesystem-based repository implementation for blog post operations
/// </summary>
public class FileSystemBlogPostRepository : FilesystemRepository<BlogPost>, IBlogPostRepository
{
    public FileSystemBlogPostRepository(
        IOptions<FilesystemStorageOptions> options,
        ILogger<FilesystemRepository<BlogPost>> logger)
        : base(options, logger)
    {
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetPublishedPostsAsync(
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await FindAsync(
            p => p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow,
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
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

        return await FindAsync(
            p => p.CategoryIds.Contains(categoryId) &&
                 (!publishedOnly || (p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow)),
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
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

        return await FindAsync(
            p => p.Tags.Contains(tag) &&
                 (!publishedOnly || (p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow)),
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<BlogPost>> GetFeaturedPostsAsync(
        PagingParameters pagingParameters,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await FindAsync(
            p => p.IsFeatured &&
                 (!publishedOnly || (p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow)),
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> GetBySlugAsync(
        string slug,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return await FirstOrDefaultAsync(
            p => p.Slug == slug &&
                 (!publishedOnly || (p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow)),
            includeDeleted: false,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> GetByIdWithoutPartitionKeyAsync(
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
        return await ReadEntityFromFileAsync(filePath, cancellationToken);
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

        return await FindAsync(
            p => p.AuthorId == authorId &&
                 (!publishedOnly || (p.IsPublished && p.PublishedAt <= DateTimeOffset.UtcNow)),
            pagingParameters,
            p => p.PublishedAt,
            ascending: false,
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

        var post = await GetByIdAsync(id, partitionKey, cancellationToken);
        if (post is not null)
        {
            post.ViewCount++;
            post.UpdatedAt = DateTimeOffset.UtcNow;
            await SaveEntityAsync(post, cancellationToken);
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
        if (post?.Tags is null || post.Tags.Count == 0)
        {
            return [];
        }

        var relatedPosts = await FindAsync(
            p => p.IsPublished &&
                 p.Id != post.Id &&
                 p.Tags.Any(tag => post.Tags.Contains(tag)),
            new PagingParameters(1, maxPosts),
            p => p.PublishedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);

        return relatedPosts.Items;
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

        var post = await GetByIdAsync(postId, currentPartitionKey, cancellationToken);
        if (post is null)
        {
            return null;
        }

        var oldPartitionKey = post.GroupKey;
        post.PublishedAt = newPublishedAt;
        
        // Update partition key based on new date
        var newYear = newPublishedAt.Year.ToString();
        post.GroupKey = newYear;

        // If partition key changed, we need to delete old file and create new one
        if (oldPartitionKey != post.GroupKey)
        {
            var oldFilePath = GetEntityFilePath(post.Id, oldPartitionKey);
            
            // Save to new location
            await SaveEntityAsync(post, cancellationToken);
            
            // Delete old file
            if (File.Exists(oldFilePath))
            {
                File.Delete(oldFilePath);
                await _indexManager.RemoveAsync(post.Id, oldPartitionKey, cancellationToken);
            }
        }
        else
        {
            await UpdateAsync(post, cancellationToken);
        }

        return post;
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<BlogPost>> GetScheduledPostsReadyToPublishAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var result = await FindAsync(
            p => p.Schedule.Status == ContentStatus.Scheduled &&
                 p.Schedule.ScheduledPublishDate <= now,
            new PagingParameters { PageSize = 1000 },
            p => p.Schedule.ScheduledPublishDate,
            ascending: true,
            includeDeleted: false,
            cancellationToken);

        return result.Items;
    }
}
