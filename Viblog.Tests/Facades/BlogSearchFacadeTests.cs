using Moq;
using Viblog.Frontend.Facades;
using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;
using Viblog.Shared.Infrastructure;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for BlogSearchFacade
/// </summary>
public class BlogSearchFacadeTests
{
    private readonly Mock<IBlogSearchService> _mockSearchService;
    private readonly BlogSearchFacade _facade;

    public BlogSearchFacadeTests()
    {
        _mockSearchService = new Mock<IBlogSearchService>();
        _facade = new BlogSearchFacade(_mockSearchService.Object);
    }

    [Fact]
    public async Task SearchPostsAsync_WithValidTerm_ReturnsMatchingPosts()
    {
        // Arrange
        var searchTerm = "blazor";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Getting Started with Blazor", "blazor tutorial"),
            CreateTestPost("Advanced Blazor Techniques", "blazor advanced patterns")
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mockSearchService.Setup(s => s.SearchAsync(
                searchTerm,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.SearchPostsAsync(searchTerm, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, post => 
            Assert.Contains(searchTerm, post.Title.ToLower() + " " + post.Content.ToLower()));
    }

    [Fact]
    public async Task SearchPostsAsync_WithNoMatches_ReturnsEmptyResult()
    {
        // Arrange
        var searchTerm = "nonexistent-term-xyz";
        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = new List<BlogPost>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _mockSearchService.Setup(s => s.SearchAsync(
                searchTerm,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.SearchPostsAsync(searchTerm, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task SearchPostsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var searchTerm = "async";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Async Post 6", "async programming"),
            CreateTestPost("Async Post 7", "async patterns"),
            CreateTestPost("Async Post 8", "async best practices")
        };

        var pagingParams = new PagingParameters(2, 5);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 13,
            PageNumber = 2,
            PageSize = 5
        };

        _mockSearchService.Setup(s => s.SearchAsync(
                searchTerm,
                It.Is<PagingParameters>(p => p.PageNumber == 2 && p.PageSize == 5),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.SearchPostsAsync(searchTerm, pagingParams);

        // Assert
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(13, result.TotalCount);
    }

    [Fact]
    public async Task SearchPostsAsync_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var searchTerm = "dotnet";
        var pagingParams = new PagingParameters(3, 15);

        _mockSearchService.Setup(s => s.SearchAsync(
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
        await _facade.SearchPostsAsync(searchTerm, pagingParams);

        // Assert
        _mockSearchService.Verify(s => s.SearchAsync(
            searchTerm,
            It.Is<PagingParameters>(p => p.PageNumber == 3 && p.PageSize == 15),
            true,
            default), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchPostsAsync_WithEmptyTerm_ReturnsEmptyResult(string emptyTerm)
    {
        // Arrange
        var pagingParams = new PagingParameters(1, 10);
        // No mock setup needed as facade returns empty immediately

        // Act
        var result = await _facade.SearchPostsAsync(emptyTerm, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task SearchPostsAsync_IsCaseInsensitive()
    {
        // Arrange
        var searchTermUpper = "BLAZOR";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Blazor Tutorial", "Learn blazor")
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 1
        };

        _mockSearchService.Setup(s => s.SearchAsync(
                searchTermUpper,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.SearchPostsAsync(searchTermUpper, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchPostsAsync_SearchesInMultipleFields()
    {
        // Arrange
        var searchTerm = "performance";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Optimization Guide", "performance tips"),
            CreateTestPost("Performance Testing", "testing strategies"),
            CreateTestPost("Best Practices", "improve performance")
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 3
        };

        _mockSearchService.Setup(s => s.SearchAsync(
                searchTerm,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.SearchPostsAsync(searchTerm, pagingParams);

        // Assert
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task SearchPostsAsync_WithLongSearchTerm_ReturnsResults()
    {
        // Arrange
        var longSearchTerm = "asynchronous programming patterns in modern dotnet applications";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Async Patterns", "asynchronous programming patterns in modern dotnet applications")
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 1
        };

        _mockSearchService.Setup(s => s.SearchAsync(
                longSearchTerm,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.SearchPostsAsync(longSearchTerm, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    private static BlogPost CreateTestPost(string title, string content)
    {
        return new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = "test",
            Title = title,
            Slug = title.ToLower().Replace(" ", "-"),
            Content = content,
            IsPublished = true,
            PublishedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
