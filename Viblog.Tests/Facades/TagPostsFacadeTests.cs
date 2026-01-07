using Viblog.Frontend.Facades;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for TagPostsFacade
/// </summary>
public class TagPostsFacadeTests
{
    private readonly Mock<IBlogPostRepository> _mockRepository;
    private readonly TagPostsFacade _facade;

    public TagPostsFacadeTests()
    {
        _mockRepository = new Mock<IBlogPostRepository>();
        _facade = new TagPostsFacade(_mockRepository.Object);
    }

    [Fact]
    public async Task GetPostsByTagAsync_WhenTagHasPosts_ReturnsPosts()
    {
        // Arrange
        var tag = "async";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Async Post 1", new[] { tag, "csharp" }),
            CreateTestPost("Async Post 2", new[] { tag })
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetPostsByTagAsync(
                tag,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByTagAsync(tag, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, post => Assert.Contains(tag, post.Tags));
    }

    [Fact]
    public async Task GetPostsByTagAsync_WhenTagNotFound_ReturnsEmptyResult()
    {
        // Arrange
        var tag = "nonexistent-tag";
        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = new List<BlogPost>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetPostsByTagAsync(
                tag,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByTagAsync(tag, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetPostsByTagAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var tag = "performance";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Post 11", new[] { tag }),
            CreateTestPost("Post 12", new[] { tag })
        };

        var pagingParams = new PagingParameters(3, 5);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 12,
            PageNumber = 3,
            PageSize = 5
        };

        _mockRepository.Setup(r => r.GetPostsByTagAsync(
                tag,
                It.Is<PagingParameters>(p => p.PageNumber == 3 && p.PageSize == 5),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByTagAsync(tag, pagingParams);

        // Assert
        Assert.Equal(3, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(12, result.TotalCount);
    }

    [Fact]
    public async Task GetPostsByTagAsync_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var tag = "blazor";
        var pagingParams = new PagingParameters(2, 20);

        _mockRepository.Setup(r => r.GetPostsByTagAsync(
                It.IsAny<string>(),
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(new PagedResult<BlogPost>
            {
                Items = new List<BlogPost>(),
                TotalCount = 0
            });

        // Act
        await _facade.GetPostsByTagAsync(tag, pagingParams);

        // Assert
        _mockRepository.Verify(r => r.GetPostsByTagAsync(
            tag,
            It.Is<PagingParameters>(p => p.PageNumber == 2 && p.PageSize == 20),
            true,
            default), Times.Once);
    }

    [Fact]
    public async Task GetPostsByTagAsync_ReturnsOnlyPublishedPosts()
    {
        // Arrange
        var tag = "testing";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Published Test Post", new[] { tag }, isPublished: true)
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 1
        };

        _mockRepository.Setup(r => r.GetPostsByTagAsync(
                tag,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByTagAsync(tag, pagingParams);

        // Assert
        Assert.All(result.Items, post => Assert.True(post.IsPublished));
    }

    [Theory]
    [InlineData("csharp")]
    [InlineData("async")]
    [InlineData("performance")]
    public async Task GetPostsByTagAsync_WithDifferentTags_FiltersCorrectly(string tag)
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateTestPost($"Post with {tag}", new[] { tag })
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 1
        };

        _mockRepository.Setup(r => r.GetPostsByTagAsync(
                tag,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByTagAsync(tag, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, post => Assert.Contains(tag, post.Tags));
    }

    [Fact]
    public async Task GetPostsByTagAsync_WithPostsHavingMultipleTags_ReturnsMatchingPosts()
    {
        // Arrange
        var targetTag = "dotnet";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Multi-tag Post 1", new[] { "dotnet", "azure", "csharp" }),
            CreateTestPost("Multi-tag Post 2", new[] { "dotnet", "blazor" })
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 2
        };

        _mockRepository.Setup(r => r.GetPostsByTagAsync(
                targetTag,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByTagAsync(targetTag, pagingParams);

        // Assert
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, post => Assert.Contains(targetTag, post.Tags));
    }

    private static BlogPost CreateTestPost(string title, string[] tags, bool isPublished = true)
    {
        return new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = "test",
            Title = title,
            Slug = title.ToLower().Replace(" ", "-"),
            Content = $"Content for {title}",
            IsPublished = isPublished,
            PublishedAt = isPublished ? DateTimeOffset.UtcNow : DateTimeOffset.MinValue,
            Tags = tags.ToList(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
