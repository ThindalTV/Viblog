using Viblog.Frontend.Facades;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Shared.Extensions;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for BlogPostDetailFacade
/// </summary>
public class BlogPostDetailFacadeTests
{
    private readonly Mock<IBlogPostRepository> _mockRepository;
    private readonly BlogPostDetailFacade _facade;

    public BlogPostDetailFacadeTests()
    {
        _mockRepository = new Mock<IBlogPostRepository>();
        _facade = new BlogPostDetailFacade(_mockRepository.Object);
    }

    [Fact]
    public async Task GetPostBySlugAsync_WhenPostExists_ReturnsPost()
    {
        // Arrange
        var slug = "test-post-slug";
        var expectedPost = CreateTestPost(slug);

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, true, default))
            .ReturnsAsync(expectedPost);

        // Act
        var result = await _facade.GetPostBySlugAsync(slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(slug, result.Slug);
        Assert.Equal(expectedPost.Live!.Title, result.Live!.Title);
    }

    [Fact]
    public async Task GetPostBySlugAsync_WhenPostNotFound_ReturnsNull()
    {
        // Arrange
        var slug = "nonexistent-slug";

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, true, default))
            .ReturnsAsync((BlogPost?)null);

        // Act
        var result = await _facade.GetPostBySlugAsync(slug);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPostBySlugAsync_CallsRepositoryWithPublishedOnly()
    {
        // Arrange
        var slug = "test-slug";

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, true, default))
            .ReturnsAsync((BlogPost?)null);

        // Act
        await _facade.GetPostBySlugAsync(slug);

        // Assert
        _mockRepository.Verify(r => r.GetBySlugAsync(slug, true, default), Times.Once);
    }

    [Fact]
    public async Task IncrementViewCountAsync_CallsRepository()
    {
        // Arrange
        var id = "post-123";
        var partitionKey = "partition-key";

        _mockRepository.Setup(r => r.IncrementViewCountAsync(id, partitionKey, default))
            .Returns(Task.CompletedTask);

        // Act
        await _facade.IncrementViewCountAsync(id, partitionKey);

        // Assert
        _mockRepository.Verify(r => r.IncrementViewCountAsync(id, partitionKey, default), Times.Once);
    }

    [Fact]
    public async Task GetRelatedPostsAsync_WhenPostExists_ReturnsRelatedPosts()
    {
        // Arrange
        var slug = "main-post";
        var mainPost = CreateTestPost(slug, tags: new[] { "tag1", "tag2" });
        var relatedPosts = new List<BlogPost>
        {
            CreateTestPost("related-1", tags: new[] { "tag1" }),
            CreateTestPost("related-2", tags: new[] { "tag2" })
        };

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, true, default))
            .ReturnsAsync(mainPost);
        _mockRepository.Setup(r => r.GetRelatedPostsAsync(mainPost, 5, default))
            .ReturnsAsync(relatedPosts);

        // Act
        var result = await _facade.GetRelatedPostsAsync(slug, maxPosts: 5);
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, p => p.Slug == "related-1");
        Assert.Contains(resultList, p => p.Slug == "related-2");
    }

    [Fact]
    public async Task GetRelatedPostsAsync_WhenPostNotFound_ReturnsEmpty()
    {
        // Arrange
        var slug = "nonexistent";

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, true, default))
            .ReturnsAsync((BlogPost?)null);

        // Act
        var result = await _facade.GetRelatedPostsAsync(slug);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRelatedPostsAsync_WithMaxPosts_LimitsResults()
    {
        // Arrange
        var slug = "main-post";
        var mainPost = CreateTestPost(slug, tags: new[] { "tag1" });
        var relatedPosts = new List<BlogPost>
        {
            CreateTestPost("related-1"),
            CreateTestPost("related-2"),
            CreateTestPost("related-3")
        };

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, true, default))
            .ReturnsAsync(mainPost);
        _mockRepository.Setup(r => r.GetRelatedPostsAsync(mainPost, 2, default))
            .ReturnsAsync(relatedPosts.Take(2));

        // Act
        var result = await _facade.GetRelatedPostsAsync(slug, maxPosts: 2);
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        _mockRepository.Verify(r => r.GetRelatedPostsAsync(mainPost, 2, default), Times.Once);
    }

    [Fact]
    public async Task GetRelatedPostsAsync_WhenNoRelatedPosts_ReturnsEmpty()
    {
        // Arrange
        var slug = "isolated-post";
        var mainPost = CreateTestPost(slug, tags: new[] { "unique-tag" });

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, true, default))
            .ReturnsAsync(mainPost);
        _mockRepository.Setup(r => r.GetRelatedPostsAsync(mainPost, 5, default))
            .ReturnsAsync(new List<BlogPost>());

        // Act
        var result = await _facade.GetRelatedPostsAsync(slug);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetPostBySlugAsync_WithInvalidSlug_ReturnsNull(string? invalidSlug)
    {
        // Arrange - No mock setup needed as method returns null immediately

        // Act
        var result = await _facade.GetPostBySlugAsync(invalidSlug!);

        // Assert
        Assert.Null(result);
    }

    private static BlogPost CreateTestPost(string slug, string[]? tags = null)
    {
        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "test",
            Slug = slug,
            PublishedAt = DateTimeOffset.UtcNow,
            Tags = tags?.ToList() ?? new List<string>(),
            Draft = new BlogPostContent
            {
                Title = $"Post for {slug}",
                Content = "Test content"
            },
            Live = new BlogPostContent
            {
                Title = $"Post for {slug}",
                Content = "Test content"
            },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        post.Draft.ComputeHash();
        post.Live.ComputeHash();
        return post;
    }
}
