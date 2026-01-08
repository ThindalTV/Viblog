using Viblog.Frontend.Facades;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for BlogPostListFacade
/// </summary>
public class BlogPostListFacadeTests
{
    private readonly Mock<IBlogPostRepository> _mockRepository;
    private readonly BlogPostListFacade _facade;

    public BlogPostListFacadeTests()
    {
        _mockRepository = new Mock<IBlogPostRepository>();
        _facade = new BlogPostListFacade(_mockRepository.Object);
    }

    [Fact]
    public async Task GetPaginatedPostsAsync_ReturnsPagedResult()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateTestPost("Post 1", 1),
            CreateTestPost("Post 2", 2),
            CreateTestPost("Post 3", 3)
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 3,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
                It.Is<PagingParameters>(p => p.PageNumber == 1 && p.PageSize == 10),
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPaginatedPostsAsync(pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetPaginatedPostsAsync_WithPageNumber_ReturnsCorrectPage()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateTestPost("Post 4", 4),
            CreateTestPost("Post 5", 5)
        };

        var pagingParams = new PagingParameters(2, 3);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 10,
            PageNumber = 2,
            PageSize = 3
        };

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
                It.Is<PagingParameters>(p => p.PageNumber == 2 && p.PageSize == 3),
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPaginatedPostsAsync(pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(4, result.TotalPages);
    }

    [Fact]
    public async Task GetPaginatedPostsAsync_WhenNoPosts_ReturnsEmptyResult()
    {
        // Arrange
        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = new List<BlogPost>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
                It.IsAny<PagingParameters>(),
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPaginatedPostsAsync(pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetPaginatedPostsAsync_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var pagingParams = new PagingParameters(3, 15);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = new List<BlogPost>(),
            TotalCount = 0,
            PageNumber = 3,
            PageSize = 15
        };

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
                It.IsAny<PagingParameters>(),
                default))
            .ReturnsAsync(expectedResult);

        // Act
        await _facade.GetPaginatedPostsAsync(pagingParams);

        // Assert
        _mockRepository.Verify(r => r.GetPublishedPostsAsync(
            It.Is<PagingParameters>(p => p.PageNumber == 3 && p.PageSize == 15),
            default), Times.Once);
    }

    [Fact]
    public async Task GetPaginatedPostsAsync_ReturnsOnlyPublishedPosts()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateTestPost("Published 1", 1, isPublished: true),
            CreateTestPost("Published 2", 2, isPublished: true)
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
                It.IsAny<PagingParameters>(),
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPaginatedPostsAsync(pagingParams);

        // Assert
        Assert.All(result.Items, post => Assert.True(post.IsPublished));
    }

    private static BlogPost CreateTestPost(string title, int id, bool isPublished = true)
    {
        return new BlogPost
        {
            Id = id.ToString(),
            GroupKey = "test",
            Title = title,
            Slug = title.ToLower().Replace(" ", "-"),
            Content = $"Content for {title}",
            IsPublished = isPublished,
            PublishedAt = isPublished ? DateTimeOffset.UtcNow.AddDays(-id) : DateTimeOffset.MinValue,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-id),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-id)
        };
    }
}
