using Viblog.Frontend.Facades;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Shared.Extensions;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for CategoryPostsFacade
/// </summary>
public class CategoryPostsFacadeTests
{
    private readonly Mock<IBlogPostRepository> _mockRepository;
    private readonly CategoryPostsFacade _facade;

    public CategoryPostsFacadeTests()
    {
        _mockRepository = new Mock<IBlogPostRepository>();
        _facade = new CategoryPostsFacade(_mockRepository.Object);
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_WhenCategoryHasPosts_ReturnsPosts()
    {
        // Arrange
        var categoryId = "dotnet";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Post 1", categoryId),
            CreateTestPost("Post 2", categoryId)
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetPostsByCategoryAsync(
                categoryId,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByCategoryAsync(categoryId, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, post => Assert.Contains(categoryId, post.CategoryIds));
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_WhenCategoryEmpty_ReturnsEmptyResult()
    {
        // Arrange
        var categoryId = "empty-category";
        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = new List<BlogPost>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetPostsByCategoryAsync(
                categoryId,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByCategoryAsync(categoryId, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var categoryId = "blazor";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Post 6", categoryId),
            CreateTestPost("Post 7", categoryId),
            CreateTestPost("Post 8", categoryId)
        };

        var pagingParams = new PagingParameters(2, 5);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 13,
            PageNumber = 2,
            PageSize = 5
        };

        _mockRepository.Setup(r => r.GetPostsByCategoryAsync(
                categoryId,
                It.Is<PagingParameters>(p => p.PageNumber == 2 && p.PageSize == 5),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByCategoryAsync(categoryId, pagingParams);

        // Assert
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(13, result.TotalCount);
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var categoryId = "azure";
        var pagingParams = new PagingParameters(3, 15);

        _mockRepository.Setup(r => r.GetPostsByCategoryAsync(
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
        await _facade.GetPostsByCategoryAsync(categoryId, pagingParams);

        // Assert
        _mockRepository.Verify(r => r.GetPostsByCategoryAsync(
            categoryId,
            It.Is<PagingParameters>(p => p.PageNumber == 3 && p.PageSize == 15),
            true,
            default), Times.Once);
    }

    [Fact]
    public async Task GetPostsByCategoryAsync_ReturnsOnlyPublishedPosts()
    {
        // Arrange
        var categoryId = "csharp";
        var posts = new List<BlogPost>
        {
            CreateTestPost("Published Post", categoryId, isPublished: true)
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 1
        };

        _mockRepository.Setup(r => r.GetPostsByCategoryAsync(
                categoryId,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByCategoryAsync(categoryId, pagingParams);

        // Assert
        Assert.All(result.Items, post => Assert.True(post.IsPublished()));
    }

    [Theory]
    [InlineData("dotnet")]
    [InlineData("azure")]
    [InlineData("blazor")]
    public async Task GetPostsByCategoryAsync_WithDifferentCategories_FiltersCorrectly(string categoryId)
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateTestPost("Post in category", categoryId)
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 1
        };

        _mockRepository.Setup(r => r.GetPostsByCategoryAsync(
                categoryId,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByCategoryAsync(categoryId, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, post => Assert.Contains(categoryId, post.CategoryIds));
    }

    private static BlogPost CreateTestPost(string title, string categoryId, bool isPublished = true)
    {
        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = categoryId,
            Slug = title.ToLower().Replace(" ", "-"),
            Draft = new BlogPostContent
            {
                Title = title,
                Content = $"Content for {title}"
            },
            Live = isPublished ? new BlogPostContent
            {
                Title = title,
                Content = $"Content for {title}"
            } : null,
            PublishedAt = isPublished ? DateTimeOffset.UtcNow : null,
            CategoryIds = new List<string> { categoryId },
            CategoryNames = new List<string> { char.ToUpper(categoryId[0]) + categoryId[1..] },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        post.Draft.ComputeHash();
        if (post.Live != null)
        {
            post.Live.ComputeHash();
        }
        return post;
    }
}
