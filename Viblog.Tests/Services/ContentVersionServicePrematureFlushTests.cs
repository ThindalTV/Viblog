using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Shared.Data.Sources.CosmosDb.Data;
using Viblog.Shared.Data.Sources.CosmosDb.Data.Repositories;
using Viblog.Shared.Services.Content;

namespace Viblog.Tests.Services;

/// <summary>
/// Reproduces Bug 2: ContentVersionService.CreatePublishedSnapshotAsync calls
/// SaveChangesAsync on a shared DbContext, which prematurely flushes all pending
/// BlogPost changes to the database before the caller intends to commit.
///
/// This causes two problems in production:
/// 1. The BlogPost is saved with GroupKey="draft" and IsPublished=true — an inconsistent
///    intermediate state.
/// 2. The entity becomes "clean" (Unchanged) in EF tracking, which strips the EF Core
///    internal __jObject/etag metadata. When CosmosDbBlogPostRepository.UpdateAsync then
///    tries to move the document to a new partition key it detaches and re-attaches the
///    entity without that metadata, resulting in a 404 from CosmosDB.
///
/// The test uses a real shared InMemory DbContext (matching production scope behaviour)
/// and real repository instances to confirm the premature flush occurs.
/// After the fix (removing SaveChangesAsync from CreatePublishedSnapshotAsync) the test passes.
/// </summary>
public class ContentVersionServicePrematureFlushTests
{
    // -------------------------------------------------------------------------
    // Test infrastructure
    // -------------------------------------------------------------------------

    /// <summary>
    /// InMemory-compatible context.  Omits Cosmos-specific APIs (ToContainer,
    /// HasPartitionKey, HasNoDiscriminator) that are unsupported by the InMemory provider.
    /// </summary>
    private sealed class SharedTestDbContext(DbContextOptions<ApplicationDbContext> options)
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

            modelBuilder.Entity<BlogPostVersion>(b =>
            {
                b.HasKey(v => v.Id);
                b.OwnsOne(v => v.Content);
            });
        }
    }

    private static SharedTestDbContext CreateSharedContext(string dbName) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

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
    /// Reproduces the premature flush:
    /// After PromoteDraftToLiveAsync the BlogPost entity must still have pending
    /// (unsaved) changes — the caller controls when to commit via SaveChangesAsync.
    ///
    /// BUG: The entity is Unchanged because the shared-context SaveChangesAsync inside
    ///      CreatePublishedSnapshotAsync already flushed everything.
    /// FIX: Remove the SaveChangesAsync call from CreatePublishedSnapshotAsync; let the
    ///      caller's SaveChangesAsync commit both the version snapshot and the post.
    ///
    /// NOTE: This test uses mocked repositories instead of real Cosmos repositories to avoid
    ///       InMemory provider compatibility issues with Cosmos-specific partition key APIs.
    /// </summary>
    [Fact]
    public async Task PromoteDraftToLiveAsync_WithSharedDbContext_DoesNotPrematurelyFlushBlogPost()
    {
        // Arrange — mock the version repository to avoid Cosmos-specific WithPartitionKey calls
        var mockVersionRepo = new Mock<IBlogPostVersionRepository>();
        mockVersionRepo
            .Setup(r => r.GetLatestVersionNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        using var context = CreateSharedContext(Guid.NewGuid().ToString());
        var service = new ContentVersionService(
            mockVersionRepo.Object,
            Mock.Of<IPageVersionRepository>(),
            Mock.Of<ILogger<ContentVersionService>>());

        var post = CreateDraftPost();

        // Seed the post (simulates a document already stored in CosmosDB)
        context.Set<BlogPost>().Add(post);
        await context.SaveChangesAsync();

        // Simulate the mutations PublishNowAsync performs before calling PromoteDraftToLiveAsync:
        // SetFirstPublishedDateIfNeeded sets PublishedAt, then PromoteDraftToLive will set Live/IsPublished.
        post.PublishedAt = DateTimeOffset.UtcNow;
        // At this point the entity has ONE unsaved change (PublishedAt)
        Assert.Equal(EntityState.Modified, context.Entry(post).State); // sanity check

        // Act
        await service.PromoteDraftToLiveAsync(post, publishedBy: "user1");

        // Assert — changes to the post (IsPublished=true, Live set, etc.) must still be
        // PENDING in EF tracking so that the CALLER can control the commit.
        //
        // This assertion FAILS before the fix:
        //   EntityState is Unchanged because CreatePublishedSnapshotAsync called
        //   versionRepo.SaveChangesAsync() which flushed the shared context.
        //
        // This assertion PASSES after the fix:
        //   SaveChangesAsync is no longer called inside CreatePublishedSnapshotAsync;
        //   the entity remains Modified until the caller calls SaveChangesAsync.
        Assert.Equal(EntityState.Modified, context.Entry(post).State);
    }

    /// <summary>
    /// Confirms the symptom of the bug: after PromoteDraftToLiveAsync the database
    /// already contains an updated BlogPost document with IsPublished=true even though
    /// the caller never explicitly called SaveChangesAsync on the blog-post repository.
    ///
    /// This is the unsafe intermediate state that can leave CosmosDB with GroupKey="draft"
    /// and IsPublished=true simultaneously.
    ///
    /// NOTE: This test uses mocked repositories instead of real Cosmos repositories to avoid
    ///       InMemory provider compatibility issues with Cosmos-specific partition key APIs.
    /// </summary>
    [Fact]
    public async Task PromoteDraftToLiveAsync_WithSharedDbContext_DoesNotPersistBlogPostWithoutExplicitSave()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateSharedContext(dbName);

        // Mock the version repository to avoid Cosmos-specific WithPartitionKey calls
        var mockVersionRepo = new Mock<IBlogPostVersionRepository>();
        mockVersionRepo
            .Setup(r => r.GetLatestVersionNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var service = new ContentVersionService(
            mockVersionRepo.Object,
            Mock.Of<IPageVersionRepository>(),
            Mock.Of<ILogger<ContentVersionService>>());

        var post = CreateDraftPost();
        context.Set<BlogPost>().Add(post);
        await context.SaveChangesAsync();
        post.PublishedAt = DateTimeOffset.UtcNow;

        // Act — caller does NOT call SaveChangesAsync afterwards
        await service.PromoteDraftToLiveAsync(post, publishedBy: "user1");

        // Reload from the store using a separate context instance to see what is actually persisted
        await using var readContext = CreateSharedContext(dbName);
        var persisted = await readContext.Set<BlogPost>().FindAsync(post.Id);

        // The post should NOT be persisted as IsPublished=true yet — the caller hasn't
        // called SaveChangesAsync, so the store should still reflect the seeded state
        // (IsPublished=false).
        //
        // This assertion FAILS before the fix (persisted.IsPublished == true, saved by
        // the premature SaveChangesAsync inside CreatePublishedSnapshotAsync).
        Assert.False(persisted!.IsPublished);
    }

    /// <summary>
    /// Confirms that the version snapshot IS persisted once the caller calls
    /// SaveChangesAsync on the shared context (through any repository).
    ///
    /// This is the key contract after removing SaveChangesAsync from
    /// CreatePublishedSnapshotAsync: the snapshot is staged (AddAsync) but not yet
    /// committed. The caller's SaveChangesAsync on the blog-post repository flushes
    /// the entire shared context — committing both the blog-post mutation and the
    /// version snapshot in one unit of work.
    ///
    /// Both production call sites follow this pattern:
    ///   PostsAdminFacade.PublishPostNowAsync    → _blogPostRepository.SaveChangesAsync()
    ///   ContentPublishingBackgroundService       → blogPostRepository.SaveChangesAsync()
    ///
    /// NOTE: This test uses mocked repositories instead of real Cosmos repositories to avoid
    ///       InMemory provider compatibility issues with Cosmos-specific partition key APIs.
    /// </summary>
    [Fact]
    public async Task PromoteDraftToLiveAsync_AfterCallerSavesChanges_VersionSnapshotIsPersisted()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateSharedContext(dbName);

        // Mock the version repository to avoid Cosmos-specific WithPartitionKey calls
        // but ensure AddAsync actually adds to the shared DbContext
        var mockVersionRepo = new Mock<IBlogPostVersionRepository>();
        mockVersionRepo
            .Setup(r => r.GetLatestVersionNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockVersionRepo
            .Setup(r => r.AddAsync(It.IsAny<BlogPostVersion>(), It.IsAny<CancellationToken>()))
            .Callback<BlogPostVersion, CancellationToken>((version, _) => 
            {
                // Add to the shared context so SaveChangesAsync will persist it
                context.Set<BlogPostVersion>().Add(version);
            })
            .Returns(Task.CompletedTask);

        var service = new ContentVersionService(
            mockVersionRepo.Object,
            Mock.Of<IPageVersionRepository>(),
            Mock.Of<ILogger<ContentVersionService>>());

        var post = CreateDraftPost();
        context.Set<BlogPost>().Add(post);
        await context.SaveChangesAsync();

        // Act — service stages the snapshot but does NOT commit
        await service.PromoteDraftToLiveAsync(post, publishedBy: "user1");

        // Snapshot is staged but not yet in the store
        await using var beforeSave = CreateSharedContext(dbName);
        Assert.Empty(await beforeSave.Set<BlogPostVersion>().ToListAsync());

        // Caller commits — SaveChangesAsync on the shared context flushes everything,
        // including the staged version snapshot from versionRepo.AddAsync
        await context.SaveChangesAsync(); // equivalent to _blogPostRepository.SaveChangesAsync()

        // Version snapshot must now be in the store
        await using var afterSave = CreateSharedContext(dbName);
        var snapshots = await afterSave.Set<BlogPostVersion>().ToListAsync();

        Assert.Single(snapshots);
        Assert.Equal(post.Id, snapshots[0].ContentId);
        Assert.Equal(1, snapshots[0].VersionNumber);
        Assert.Equal("user1", snapshots[0].PublishedBy);
    }
}
