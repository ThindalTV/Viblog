using Microsoft.EntityFrameworkCore;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Shared.Data.Repositories;

/// <summary>
/// Repository implementation for blog post operations
/// </summary>
public class BlogPostRepository : Repository<BlogPost>, IBlogPostRepository
{
    public BlogPostRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public override async Task AddAsync(BlogPost entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.UpdatePartitionKey(); // Ensure partition key is set based on publication date
        entity.UpdateSearchIndex();
        await base.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task AddRangeAsync(IEnumerable<BlogPost> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();
        foreach (var entity in entityList)
        {
            entity.UpdatePartitionKey(); // Ensure partition key is set based on publication date
            entity.UpdateSearchIndex();
        }

        await base.AddRangeAsync(entityList, cancellationToken);
    }

    /// <inheritdoc/>
    public override Task UpdateAsync(BlogPost entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.UpdatePartitionKey(); // Update partition key based on published state
        entity.UpdateSearchIndex();
        return base.UpdateAsync(entity, cancellationToken);
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

        var post = await _dbSet
            .WithPartitionKey(partitionKey)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

        if (post != null)
        {
            post.ViewCount++;
            post.UpdatedAt = DateTimeOffset.UtcNow;
            _dbSet.Update(post);
            await _context.SaveChangesAsync(cancellationToken);
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
            return Enumerable.Empty<BlogPost>();
        }

        // Find posts that share at least one tag, excluding the current post
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
    public virtual async Task<BlogPost?> AddCommentAsync(
        string postId,
        string partitionKey,
        Comment comment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comment);

        var post = await _dbSet.FirstOrDefaultAsync(
            p => p.Id == postId && p.PartitionKey == partitionKey,
            cancellationToken);

        if (post == null || !post.AllowComments)
        {
            return null;
        }

        comment.CreatedAt = DateTimeOffset.UtcNow;
        post.Comments.Add(comment);
        post.CommentCount = post.Comments.Count(c => !c.IsDeleted);
        post.LastCommentAt = comment.CreatedAt;
        post.UpdatedAt = DateTimeOffset.UtcNow;

        _dbSet.Update(post);
        await _context.SaveChangesAsync(cancellationToken);

        return post;
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> UpdateCommentAsync(
        string postId,
        string partitionKey,
        string commentId,
        Comment updatedComment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updatedComment);

        var post = await _dbSet.FirstOrDefaultAsync(
            p => p.Id == postId && p.PartitionKey == partitionKey,
            cancellationToken);

        if (post == null)
        {
            return null;
        }

        var comment = post.Comments.FirstOrDefault(c => c.Id == commentId);
        if (comment == null)
        {
            return null;
        }

        comment.Content = updatedComment.Content;
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        post.UpdatedAt = DateTimeOffset.UtcNow;

        _dbSet.Update(post);
        await _context.SaveChangesAsync(cancellationToken);

        return post;
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> DeleteCommentAsync(
        string postId,
        string partitionKey,
        string commentId,
        CancellationToken cancellationToken = default)
    {
        var post = await _dbSet.FirstOrDefaultAsync(
            p => p.Id == postId && p.PartitionKey == partitionKey,
            cancellationToken);

        if (post == null)
        {
            return null;
        }

        var comment = post.Comments.FirstOrDefault(c => c.Id == commentId);
        if (comment == null)
        {
            return null;
        }

        comment.IsDeleted = true;
        post.CommentCount = post.Comments.Count(c => !c.IsDeleted);
        post.UpdatedAt = DateTimeOffset.UtcNow;

        _dbSet.Update(post);
        await _context.SaveChangesAsync(cancellationToken);

        return post;
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> ApproveCommentAsync(
        string postId,
        string partitionKey,
        string commentId,
        CancellationToken cancellationToken = default)
    {
        var post = await _dbSet.FirstOrDefaultAsync(
            p => p.Id == postId && p.PartitionKey == partitionKey,
            cancellationToken);

        if (post == null)
        {
            return null;
        }

        var comment = post.Comments.FirstOrDefault(c => c.Id == commentId);
        if (comment == null)
        {
            return null;
        }

        comment.IsApproved = true;
        comment.IsSpam = false;
        post.UpdatedAt = DateTimeOffset.UtcNow;

        _dbSet.Update(post);
        await _context.SaveChangesAsync(cancellationToken);

        return post;
    }

    /// <inheritdoc/>
    public virtual async Task<BlogPost?> MarkCommentAsSpamAsync(
        string postId,
        string partitionKey,
        string commentId,
        CancellationToken cancellationToken = default)
    {
        var post = await _dbSet.FirstOrDefaultAsync(
            p => p.Id == postId && p.PartitionKey == partitionKey,
            cancellationToken);

        if (post == null)
        {
            return null;
        }

        var comment = post.Comments.FirstOrDefault(c => c.Id == commentId);
        if (comment == null)
        {
            return null;
        }

        comment.IsSpam = true;
        comment.IsApproved = false;
        post.UpdatedAt = DateTimeOffset.UtcNow;

        _dbSet.Update(post);
        await _context.SaveChangesAsync(cancellationToken);

        return post;
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<Comment>> GetApprovedCommentsAsync(
        string postId,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        var post = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == postId && p.PartitionKey == partitionKey,
                cancellationToken);

        if (post == null)
        {
            return Enumerable.Empty<Comment>();
        }

        return post.Comments
            .Where(c => c.IsApproved && !c.IsDeleted && !c.IsSpam)
            .OrderBy(c => c.CreatedAt)
            .ToList();
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
            p => p.Id == postId && p.PartitionKey == currentPartitionKey,
            cancellationToken);

        if (post == null)
        {
            return null;
        }

        var oldPublishedAt = post.PublishedAt;
        post.PublishedAt = newPublishedAt;
        
        var oldPartitionKey = post.PartitionKey;
        post.UpdatePartitionKey();
        var newPartitionKey = post.PartitionKey;

        // If partition key hasn't changed, just update normally
        if (oldPartitionKey == newPartitionKey)
        {
            post.UpdatedAt = DateTimeOffset.UtcNow;
            _dbSet.Update(post);
            await _context.SaveChangesAsync(cancellationToken);
            return post;
        }

        // Partition key changed (year changed or published/draft status changed)
        // We need to delete the old document and create a new one
        // CosmosDB doesn't allow updating partition keys directly
        
        // Remove from old partition
        _dbSet.Remove(post);
        await _context.SaveChangesAsync(cancellationToken);

        // Create new document with same ID but different partition key
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbSet.AddAsync(post, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return post;
    }
}
