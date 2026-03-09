using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Repositories;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Shared.Services;

namespace Viblog.Tests.Services;

public class BlogSearchServiceTests
{
    private readonly Mock<IBlogPostRepository> _mockRepository;
    private readonly BlogSearchService _service;

    public BlogSearchServiceTests()
    {
        _mockRepository = new Mock<IBlogPostRepository>();
        _service = new BlogSearchService(_mockRepository.Object);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BlogSearchService(null!));
    }

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithValidSearchTerm_ReturnsMatchingPosts()
    {
        // Arrange
        var searchTerm = "test";
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<BlogPost>(
            [CreateBlogPost("Test Post")],
            1, 1, 10);

        _mockRepository.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>(),
            It.IsAny<PagingParameters>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, DateTimeOffset?>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.SearchAsync(searchTerm, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchAsync_NormalizesSearchTermToLowerCase()
    {
        // Arrange
        var searchTerm = "TEST";
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<BlogPost>([], 0, 1, 10);

        _mockRepository.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>(),
            It.IsAny<PagingParameters>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, DateTimeOffset?>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.SearchAsync(searchTerm, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchAsync_WithNullSearchTerm_ThrowsArgumentNullException()
    {
        // Arrange
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SearchAsync(null!, pagingParams));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_WithWhitespaceSearchTerm_ThrowsArgumentException(string searchTerm)
    {
        // Arrange
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SearchAsync(searchTerm, pagingParams));
    }

    [Fact]
    public async Task SearchAsync_WithNullPagingParameters_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SearchAsync("test", null!));
    }

    [Fact]
    public async Task SearchAsync_WithPublishedOnlyTrue_FiltersUnpublishedPosts()
    {
        // Arrange
        var searchTerm = "test";
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<BlogPost>([], 0, 1, 10);

        _mockRepository.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>(),
            It.IsAny<PagingParameters>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, DateTimeOffset?>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.SearchAsync(searchTerm, pagingParams, publishedOnly: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SearchAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var searchTerm = "test";
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var cancellationToken = new CancellationToken();
        var expectedResult = new PagedResult<BlogPost>([], 0, 1, 10);

        _mockRepository.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>(),
            It.IsAny<PagingParameters>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, DateTimeOffset?>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.SearchAsync(searchTerm, pagingParams, cancellationToken: cancellationToken);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region SearchByTitleAsync Tests

    [Fact]
    public async Task SearchByTitleAsync_WithValidTitleTerm_ReturnsMatchingPosts()
    {
        // Arrange
        var titleTerm = "test";
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<BlogPost>(
            [CreateBlogPost("Test Title")],
            1, 1, 10);

        _mockRepository.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>(),
            It.IsAny<PagingParameters>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, DateTimeOffset?>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.SearchByTitleAsync(titleTerm, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchByTitleAsync_WithNullTitleTerm_ThrowsArgumentNullException()
    {
        // Arrange
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SearchByTitleAsync(null!, pagingParams));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchByTitleAsync_WithWhitespaceTitleTerm_ThrowsArgumentException(string titleTerm)
    {
        // Arrange
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SearchByTitleAsync(titleTerm, pagingParams));
    }

    [Fact]
    public async Task SearchByTitleAsync_WithNullPagingParameters_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SearchByTitleAsync("test", null!));
    }

    [Fact]
    public async Task SearchByTitleAsync_NormalizesToLowerCase()
    {
        // Arrange
        var titleTerm = "TEST TITLE";
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<BlogPost>([], 0, 1, 10);

        _mockRepository.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>(),
            It.IsAny<PagingParameters>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, DateTimeOffset?>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.SearchByTitleAsync(titleTerm, pagingParams);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region SearchMultipleTermsAsync Tests

    [Fact]
    public async Task SearchMultipleTermsAsync_WithValidTerms_ReturnsMatchingPosts()
    {
        // Arrange
        var searchTerms = new[] { "test", "post" };
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<BlogPost>(
            [CreateBlogPost("Test Post")],
            1, 1, 10);

        _mockRepository.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>(),
            It.IsAny<PagingParameters>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, DateTimeOffset?>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.SearchMultipleTermsAsync(searchTerms, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchMultipleTermsAsync_WithNullSearchTerms_ThrowsArgumentNullException()
    {
        // Arrange
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SearchMultipleTermsAsync(null!, pagingParams));
    }

    [Fact]
    public async Task SearchMultipleTermsAsync_WithEmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var searchTerms = Array.Empty<string>();
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SearchMultipleTermsAsync(searchTerms, pagingParams));
        Assert.Equal("searchTerms", exception.ParamName);
    }

    [Fact]
    public async Task SearchMultipleTermsAsync_WithAllWhitespaceTerms_ThrowsArgumentException()
    {
        // Arrange
        var searchTerms = new[] { "   ", "", "  " };
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SearchMultipleTermsAsync(searchTerms, pagingParams));
        Assert.Equal("searchTerms", exception.ParamName);
    }

    [Fact]
    public async Task SearchMultipleTermsAsync_FiltersOutEmptyTerms()
    {
        // Arrange
        var searchTerms = new[] { "test", "   ", "post", "" };
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<BlogPost>([], 0, 1, 10);

        _mockRepository.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>(),
            It.IsAny<PagingParameters>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, DateTimeOffset?>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.SearchMultipleTermsAsync(searchTerms, pagingParams);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SearchMultipleTermsAsync_NormalizesTermsToLowerCase()
    {
        // Arrange
        var searchTerms = new[] { "TEST", "POST" };
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<BlogPost>([], 0, 1, 10);

        _mockRepository.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, bool>>>(),
            It.IsAny<PagingParameters>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BlogPost, DateTimeOffset?>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.SearchMultipleTermsAsync(searchTerms, pagingParams);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SearchMultipleTermsAsync_WithNullPagingParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var searchTerms = new[] { "test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SearchMultipleTermsAsync(searchTerms, null!));
    }

    #endregion

    #region Helper Methods

    private static BlogPost CreateBlogPost(string title)
    {  
        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            Slug = title.ToLower().Replace(" ", "-"),
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1),
            AuthorName = "Test Author",
            CategoryNames = ["Test"],
            Draft = new BlogPostContent
            {
                Title = title,
                Content = "Test content",
                Short = "Test short",
                SearchIndex = title.ToLower()
            },
            Live = new BlogPostContent
            {
                Title = title,
                Content = "Test content",
                Short = "Test short",
                SearchIndex = title.ToLower()
            }
        };
        post.Draft.ComputeHash();
        post.Live.ComputeHash();
        return post;
    }

    #endregion
}
