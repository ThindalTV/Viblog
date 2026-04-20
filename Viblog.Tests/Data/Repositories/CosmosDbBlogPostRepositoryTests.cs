using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Shared.Data.Sources.CosmosDb.Data;
using Viblog.Shared.Data.Sources.CosmosDb.Data.Repositories;

namespace Viblog.Tests.Data.Repositories;

/// <summary>
/// Tests for <see cref="CosmosDbBlogPostRepository"/> verifying that all blog posts
/// use a constant "blogpost" partition key regardless of publication state.
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

    private static BlogPost CreateDraftPost() => new()
    {
        Id = Guid.NewGuid().ToString(),
        GroupKey = string.Empty,
        Draft = new BlogPostContent { Title = "Test Post", Markdown = "# Hello" }
    };

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddAsync_SetsConstantPartitionKey()
    {
        using var context = CreateInMemoryContext();
        var mockLogger = new Mock<ILogger<CosmosDbBlogPostRepository>>();
        var repo = new CosmosDbBlogPostRepository(context, mockLogger.Object);

        var post = CreateDraftPost();
        await repo.AddAsync(post, default);
        await repo.SaveChangesAsync(default);

        Assert.Equal("blogpost", post.GroupKey);
    }

    [Fact]
    public async Task UpdateAsync_WhenPublished_KeepsSamePartitionKey()
    {
        using var context = CreateInMemoryContext();
        var mockLogger = new Mock<ILogger<CosmosDbBlogPostRepository>>();
        var repo = new CosmosDbBlogPostRepository(context, mockLogger.Object);

        var post = CreateDraftPost();
        await repo.AddAsync(post, default);
        await repo.SaveChangesAsync(default);

        // Publish the post
        post.IsPublished = true;
        post.PublishedAt = DateTimeOffset.UtcNow;

        await repo.UpdateAsync(post, default);
        await repo.SaveChangesAsync(default);

        var saved = await context.Set<BlogPost>().FirstOrDefaultAsync(p => p.Id == post.Id);
        Assert.NotNull(saved);
        Assert.Equal("blogpost", saved.GroupKey);
    }

    [Fact]
    public async Task UpdateAsync_WhenUnpublished_KeepsSamePartitionKey()
    {
        using var context = CreateInMemoryContext();
        var mockLogger = new Mock<ILogger<CosmosDbBlogPostRepository>>();
        var repo = new CosmosDbBlogPostRepository(context, mockLogger.Object);

        var post = CreateDraftPost();
        post.IsPublished = true;
        post.PublishedAt = DateTimeOffset.UtcNow;
        await repo.AddAsync(post, default);
        await repo.SaveChangesAsync(default);

        // Unpublish
        post.IsPublished = false;
        post.PublishedAt = null;

        await repo.UpdateAsync(post, default);
        await repo.SaveChangesAsync(default);

        var saved = await context.Set<BlogPost>().FirstOrDefaultAsync(p => p.Id == post.Id);
        Assert.NotNull(saved);
        Assert.Equal("blogpost", saved.GroupKey);
    }
}
