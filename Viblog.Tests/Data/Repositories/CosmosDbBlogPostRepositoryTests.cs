using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Shared.Data.Sources.CosmosDb.Data;
using Viblog.Shared.Data.Sources.CosmosDb.Data.Repositories;

namespace Viblog.Tests.Data.Repositories;

/// <summary>
/// Reproduces Bug 1: CosmosDbBlogPostRepository.UpdateAsync reused a detached entity
/// for the Phase-1 delete when the partition key changes (e.g. publish moves a post
/// from GroupKey="draft" to GroupKey="2026").
///
/// When an EF entity is detached its internal CosmosDB metadata (__jObject, _etag,
/// _self link) is lost. Re-attaching it via _dbSet.Remove() leaves EF Core without
/// the information it needs to form a valid DELETE request, resulting in a 404 from
/// the CosmosDB service.
///
/// The fix is to load the document fresh — with proper EF tracking — before deleting.
/// The production implementation uses WithPartitionKey() which is CosmosDB-specific;
/// a protected virtual method (LoadByPartitionKeyForDeleteAsync) is the seam that lets
/// these unit tests stay on the InMemory provider while still verifying the behaviour.
/// </summary>
public class CosmosDbBlogPostRepositoryTests
{
    // -------------------------------------------------------------------------
    // Test infrastructure
    // -------------------------------------------------------------------------

    private sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlogPost>(b =>
            {
                b.HasKey(p => p.Id);
                b.OwnsOne(p => p.Draft);
                b.OwnsOne(p => p.Live);
                b.OwnsOne(p => p.Schedule);
            });
        }
    }

    private static TestDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ILogger<CosmosDbBlogPostRepository> CreateMockLogger()
    {
        var mockLogger = new Mock<ILogger<CosmosDbBlogPostRepository>>();
        return mockLogger.Object;
    }

    /// <summary>
    /// Spy subclass that:
    /// - Records whether LoadByPartitionKeyForDeleteAsync was called and with which partition key
    /// - Overrides the method with an InMemory-compatible Where query (no WithPartitionKey)
    /// Without the virtual method (i.e. before the fix), the spy can never be called, so
    /// FreshLoadAttempted stays false and the assertion fails — reproducing the bug.
    /// </summary>
    private sealed class SpyBlogPostRepository(ApplicationDbContext context, ILogger<CosmosDbBlogPostRepository> logger)
        : CosmosDbBlogPostRepository(context, logger)
    {
        public bool FreshLoadAttempted { get; private set; }
        public string? PartitionKeyUsedForDelete { get; private set; }

        protected override Task<BlogPost?> LoadByPartitionKeyForDeleteAsync(
            string id,
            string partitionKey,
            CancellationToken cancellationToken)
        {
            FreshLoadAttempted = true;
            PartitionKeyUsedForDelete = partitionKey;

            // InMemory-compatible: filter by both Id and GroupKey instead of WithPartitionKey
            return _dbSet
                .Where(e => e.Id == id && e.GroupKey == partitionKey)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    private static BlogPost CreateDraftPost() => new()
    {
        Id = Guid.NewGuid().ToString(),
        GroupKey = "draft",
        Draft = new BlogPostContent { Title = "Test Post", Markdown = "# Hello" }
    };

    // -------------------------------------------------------------------------
    // Bug-reproducing test
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reproduces the root cause: when a partition key change is needed, UpdateAsync must
    /// perform a FRESH LOAD of the document before deleting it — not reuse the detached entity.
    ///
    /// BUG (before the virtual method / fix):
    ///   UpdateAsync detaches the entity and calls _dbSet.Remove(detachedEntity).
    ///   The virtual method LoadByPartitionKeyForDeleteAsync does not exist, so it is
    ///   never called → FreshLoadAttempted stays false → Assert.True fails.
    ///
    /// FIX:
    ///   UpdateAsync calls LoadByPartitionKeyForDeleteAsync(id, originalGroupKey) before
    ///   deleting → the spy records the call → FreshLoadAttempted is true → test passes.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenPartitionKeyChanges_LoadsEntityFreshBeforeDeleting()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var repo = new SpyBlogPostRepository(context, logger);

        var post = CreateDraftPost();
        await repo.AddAsync(post, default);
        await repo.SaveChangesAsync(default);

        // Mark as published — SetPartitionKey will now return the year instead of "draft"
        post.IsPublished = true;
        post.PublishedAt = DateTimeOffset.UtcNow;

        // Act
        await repo.UpdateAsync(post, default);

        // Assert — the fresh load must have been called with the ORIGINAL partition key
        Assert.True(repo.FreshLoadAttempted,
            "UpdateAsync must load the entity fresh for the delete step; " +
            "reusing a detached entity loses EF Core tracking metadata and causes a 404 in CosmosDB.");
        Assert.Equal("draft", repo.PartitionKeyUsedForDelete);
    }

    /// <summary>
    /// Verifies the end-state contract: after a partition-key-changing update the
    /// document must exist under the NEW key and must no longer exist under the old one.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenPartitionKeyChanges_MovesDocumentToNewPartition()
    {
        using var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var repo = new SpyBlogPostRepository(context, logger);

        var post = CreateDraftPost();
        await repo.AddAsync(post, default);
        await repo.SaveChangesAsync(default);

        post.IsPublished = true;
        post.PublishedAt = DateTimeOffset.UtcNow;
        var expectedNewKey = post.PublishedAt.Value.Year.ToString();

        await repo.UpdateAsync(post, default);
        await repo.SaveChangesAsync(default);

        var allPosts = await context.Set<BlogPost>().ToListAsync();

        // Old partition must be gone
        Assert.DoesNotContain(allPosts, p => p.Id == post.Id && p.GroupKey == "draft");

        // New partition must have the document
        Assert.Contains(allPosts, p => p.Id == post.Id && p.GroupKey == expectedNewKey && p.IsPublished);
    }

    /// <summary>
    /// Verifies that when the partition key does NOT change (e.g. saving a draft edit)
    /// the fresh-load path is NOT taken — only a normal update is performed.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenPartitionKeyUnchanged_DoesNotAttemptFreshLoad()
    {
        using var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var repo = new SpyBlogPostRepository(context, logger);

        var post = CreateDraftPost();
        await repo.AddAsync(post, default);
        await repo.SaveChangesAsync(default);

        // Modify the post without changing publish state (partition key stays "draft")
        post.Draft.Title = "Updated Title";

        await repo.UpdateAsync(post, default);
        await repo.SaveChangesAsync(default);

        // Fresh load should NOT have been attempted for a same-partition update
        Assert.False(repo.FreshLoadAttempted);
    }
}
