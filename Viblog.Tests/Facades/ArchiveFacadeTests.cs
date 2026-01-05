using Moq;
using Vilog.Frontend.Facades;
using Vilog.Shared.Data.Common;
using Vilog.Shared.Data.Entities;
using Vilog.Shared.Data.Repositories;

namespace Vilog.Tests.Facades;

/// <summary>
/// Unit tests for ArchiveFacade
/// </summary>
public class ArchiveFacadeTests
{
    private readonly Mock<IBlogPostRepository> _mockRepository;
    private readonly ArchiveFacade _facade;

    public ArchiveFacadeTests()
    {
        _mockRepository = new Mock<IBlogPostRepository>();
        _facade = new ArchiveFacade(_mockRepository.Object);
    }

    [Fact]
    public async Task GetPostsByMonthAsync_WhenMonthHasPosts_ReturnsPosts()
    {
        // Arrange
        var year = 2024;
        var month = 3;
        var posts = new List<BlogPost>
        {
            CreateTestPost("March Post 1", year, month, 5),
            CreateTestPost("March Post 2", year, month, 15),
            CreateTestPost("March Post 3", year, month, 25)
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 3,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetPostsByMonthAsync(
                year,
                month,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByMonthAsync(year, month, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count());
        Assert.All(result.Items, post =>
        {
            Assert.Equal(year, post.PublishedAt.Year);
            Assert.Equal(month, post.PublishedAt.Month);
        });
    }

    [Fact]
    public async Task GetPostsByMonthAsync_WhenMonthEmpty_ReturnsEmptyResult()
    {
        // Arrange
        var year = 2024;
        var month = 6;
        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = new List<BlogPost>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetPostsByMonthAsync(
                year,
                month,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByMonthAsync(year, month, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetPostsByMonthAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var year = 2024;
        var month = 1;
        var posts = new List<BlogPost>
        {
            CreateTestPost("Post 6", year, month, 16),
            CreateTestPost("Post 7", year, month, 17),
            CreateTestPost("Post 8", year, month, 18)
        };

        var pagingParams = new PagingParameters(2, 5);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 13,
            PageNumber = 2,
            PageSize = 5
        };

        _mockRepository.Setup(r => r.GetPostsByMonthAsync(
                year,
                month,
                It.Is<PagingParameters>(p => p.PageNumber == 2 && p.PageSize == 5),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByMonthAsync(year, month, pagingParams);

        // Assert
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(13, result.TotalCount);
    }

    [Fact]
    public async Task GetPostsByMonthAsync_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var year = 2023;
        var month = 12;
        var pagingParams = new PagingParameters(3, 15);

        _mockRepository.Setup(r => r.GetPostsByMonthAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(new PagedResult<BlogPost>
            {
                Items = new List<BlogPost>(),
                TotalCount = 0
            });

        // Act
        await _facade.GetPostsByMonthAsync(year, month, pagingParams);

        // Assert
        _mockRepository.Verify(r => r.GetPostsByMonthAsync(
            year,
            month,
            It.Is<PagingParameters>(p => p.PageNumber == 3 && p.PageSize == 15),
            true,
            default), Times.Once);
    }

    [Theory]
    [InlineData(2024, 1)]
    [InlineData(2024, 6)]
    [InlineData(2024, 12)]
    public async Task GetPostsByMonthAsync_WithDifferentMonths_FiltersCorrectly(int year, int month)
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateTestPost($"Post in {month}/{year}", year, month, 10)
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 1
        };

        _mockRepository.Setup(r => r.GetPostsByMonthAsync(
                year,
                month,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByMonthAsync(year, month, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.All(result.Items, post =>
        {
            Assert.Equal(year, post.PublishedAt.Year);
            Assert.Equal(month, post.PublishedAt.Month);
        });
    }

    [Fact]
    public async Task GetPostsByMonthAsync_ReturnsOnlyPublishedPosts()
    {
        // Arrange
        var year = 2024;
        var month = 2;
        var posts = new List<BlogPost>
        {
            CreateTestPost("Published February Post", year, month, 10, isPublished: true)
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 1
        };

        _mockRepository.Setup(r => r.GetPostsByMonthAsync(
                year,
                month,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByMonthAsync(year, month, pagingParams);

        // Assert
        Assert.All(result.Items, post => Assert.True(post.IsPublished));
    }

    [Theory]
    [InlineData(2020, 1)]
    [InlineData(2022, 7)]
    [InlineData(2025, 12)]
    public async Task GetPostsByMonthAsync_WithVariousYears_WorksCorrectly(int year, int month)
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateTestPost($"Post from {year}", year, month, 1)
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 1
        };

        _mockRepository.Setup(r => r.GetPostsByMonthAsync(
                year,
                month,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByMonthAsync(year, month, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, post => Assert.Equal(year, post.PublishedAt.Year));
    }

    [Fact]
    public async Task GetPostsByMonthAsync_PostsOrderedByDate_ReturnsInCorrectOrder()
    {
        // Arrange
        var year = 2024;
        var month = 4;
        var posts = new List<BlogPost>
        {
            CreateTestPost("Latest Post", year, month, 30),
            CreateTestPost("Middle Post", year, month, 15),
            CreateTestPost("Oldest Post", year, month, 1)
        };

        var pagingParams = new PagingParameters(1, 10);
        var expectedResult = new PagedResult<BlogPost>
        {
            Items = posts,
            TotalCount = 3
        };

        _mockRepository.Setup(r => r.GetPostsByMonthAsync(
                year,
                month,
                It.IsAny<PagingParameters>(),
                true,
                default))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetPostsByMonthAsync(year, month, pagingParams);
        var resultList = result.Items.ToList();

        // Assert
        Assert.Equal(3, resultList.Count);
        // Verify all posts are from the same month
        Assert.All(resultList, post =>
        {
            Assert.Equal(year, post.PublishedAt.Year);
            Assert.Equal(month, post.PublishedAt.Month);
        });
    }

    private static BlogPost CreateTestPost(string title, int year, int month, int day, bool isPublished = true)
    {
        var publishedAt = new DateTimeOffset(year, month, day, 10, 0, 0, TimeSpan.Zero);

        return new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = $"{year}-{month:D2}",
            Title = title,
            Slug = title.ToLower().Replace(" ", "-"),
            Content = $"Content for {title}",
            IsPublished = isPublished,
            PublishedAt = isPublished ? publishedAt : DateTimeOffset.MinValue,
            CreatedAt = publishedAt,
            UpdatedAt = publishedAt
        };
    }
}
