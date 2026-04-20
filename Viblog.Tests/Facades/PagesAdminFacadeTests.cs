using System.Linq.Expressions;
using Viblog.Admin.Facades;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Infrastructure.Facades;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for PagesAdminFacade
/// </summary>
public class PagesAdminFacadeTests
{
    private readonly Mock<IPageRepository> _mockRepository;
    private readonly PagesAdminFacade _facade;

    public PagesAdminFacadeTests()
    {
        _mockRepository = new Mock<IPageRepository>();
        _facade = new PagesAdminFacade(_mockRepository.Object);
    }

    [Fact]
    public async Task GetPagesAsync_WithNullFilter_ReturnsAllPages()
    {
        // Arrange
        var pagingParams = new PagingParameters(1, 10);
        var expectedPages = new PagedResult<Page>
        {
            Items = new List<Page>
            {
                CreateTestPage("about", isPublished: true),
                CreateTestPage("contact", isPublished: false)
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        // Default sort field is now PublishedAt which uses DateTimeOffset?
        _mockRepository.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Page, bool>>>(),
                pagingParams,
                It.IsAny<Expression<Func<Page, DateTimeOffset?>>>(),
                false,
                false,
                default))
            .ReturnsAsync(expectedPages);

        // Act
        var result = await _facade.GetPagesAsync(pagingParams, publishedOnly: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        _mockRepository.Verify(r => r.FindAsync(
            It.IsAny<Expression<Func<Page, bool>>>(),
            pagingParams,
            It.IsAny<Expression<Func<Page, DateTimeOffset?>>>(),
            false,
            false,
            default), Times.Once);
    }

    [Fact]
    public async Task GetPagesAsync_WithPublishedOnlyTrue_ReturnsPublishedPages()
    {
        // Arrange
        var pagingParams = new PagingParameters(1, 10);
        var expectedPages = new PagedResult<Page>
        {
            Items = new List<Page> { CreateTestPage("about", isPublished: true) },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        // Default sort field is now PublishedAt which uses DateTimeOffset?
        _mockRepository.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Page, bool>>>(),
                pagingParams,
                It.IsAny<Expression<Func<Page, DateTimeOffset?>>>(),
                false,
                false,
                default))
            .ReturnsAsync(expectedPages);

        // Act
        var result = await _facade.GetPagesAsync(pagingParams, publishedOnly: true);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.NotNull(result.Items.First().Live);
    }

    [Fact]
    public async Task GetPagesAsync_WithPublishedOnlyFalse_ReturnsAllPages()
    {
        // Arrange
        var pagingParams = new PagingParameters(1, 10);
        var expectedPages = new PagedResult<Page>
        {
            Items = new List<Page>
            {
                CreateTestPage("published-page", isPublished: true),
                CreateTestPage("draft-page", isPublished: false)
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        // Default sort field is now PublishedAt which uses DateTimeOffset?
        _mockRepository.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Page, bool>>>(),
                pagingParams,
                It.IsAny<Expression<Func<Page, DateTimeOffset?>>>(),
                false,
                false,
                default))
            .ReturnsAsync(expectedPages);

        // Act
        var result = await _facade.GetPagesAsync(pagingParams, publishedOnly: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Contains(result.Items, p => p.IsPublished);
        Assert.Contains(result.Items, p => !p.IsPublished);
    }

    [Theory]
    [InlineData(PageSortField.Slug)]
    [InlineData(PageSortField.CreatedAt)]
    [InlineData(PageSortField.UpdatedAt)]
    [InlineData(PageSortField.IsPublished)]
    [InlineData(PageSortField.PublishedAt)]
    public async Task GetPagesAsync_WithDifferentSortFields_CallsRepositoryCorrectly(PageSortField sortField)
    {
        // Arrange
        var pagingParams = new PagingParameters(1, 10);
        var expectedPages = new PagedResult<Page>
        {
            Items = new List<Page> { CreateTestPage("test") },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        // Setup for string-based sorts (Slug)
        _mockRepository.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Page, bool>>>(),
                pagingParams,
                It.IsAny<Expression<Func<Page, string>>>(),
                It.IsAny<bool>(),
                false,
                default))
            .ReturnsAsync(expectedPages);

        // Setup for DateTimeOffset-based sorts (CreatedAt, UpdatedAt)
        _mockRepository.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Page, bool>>>(),
                pagingParams,
                It.IsAny<Expression<Func<Page, DateTimeOffset>>>(),
                It.IsAny<bool>(),
                false,
                default))
            .ReturnsAsync(expectedPages);

        // Setup for DateTimeOffset?-based sorts (PublishedAt)
        _mockRepository.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Page, bool>>>(),
                pagingParams,
                It.IsAny<Expression<Func<Page, DateTimeOffset?>>>(),
                It.IsAny<bool>(),
                false,
                default))
            .ReturnsAsync(expectedPages);

        // Setup for bool-based sorts (IsPublished)
        _mockRepository.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Page, bool>>>(),
                pagingParams,
                It.IsAny<Expression<Func<Page, bool>>>(),
                It.IsAny<bool>(),
                false,
                default))
            .ReturnsAsync(expectedPages);

        // Act
        var result = await _facade.GetPagesAsync(pagingParams, sortField: sortField);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetPagesAsync_WithAscendingSort_PassesCorrectDirection()
    {
        // Arrange
        var pagingParams = new PagingParameters(1, 10);
        var expectedPages = new PagedResult<Page>
        {
            Items = new List<Page> { CreateTestPage("test") },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        // Default sort field is now PublishedAt which uses DateTimeOffset?
        _mockRepository.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Page, bool>>>(),
                pagingParams,
                It.IsAny<Expression<Func<Page, DateTimeOffset?>>>(),
                true,
                false,
                default))
            .ReturnsAsync(expectedPages);

        // Act
        await _facade.GetPagesAsync(pagingParams, ascending: true);

        // Assert
        _mockRepository.Verify(r => r.FindAsync(
            It.IsAny<Expression<Func<Page, bool>>>(),
            pagingParams,
            It.IsAny<Expression<Func<Page, DateTimeOffset?>>>(),
            true,
            false,
            default), Times.Once);
    }

    [Fact]
    public async Task GetPageByIdAsync_WhenPageExists_ReturnsPage()
    {
        // Arrange
        var pageId = "page-123";
        var expectedPage = CreateTestPage("about");
        expectedPage.Id = pageId;

        _mockRepository.Setup(r => r.GetByIdWithoutPartitionKeyAsync(pageId, default))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _facade.GetPageByIdAsync(pageId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(pageId, result.Id);
    }

    [Fact]
    public async Task GetPageByIdAsync_WhenPageNotFound_ReturnsNull()
    {
        // Arrange
        var pageId = "nonexistent-page";

        _mockRepository.Setup(r => r.GetByIdWithoutPartitionKeyAsync(pageId, default))
            .ReturnsAsync((Page?)null);

        // Act
        var result = await _facade.GetPageByIdAsync(pageId);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPageByIdAsync_WithInvalidId_ThrowsException(string? invalidId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _facade.GetPageByIdAsync(invalidId!));
    }

    [Fact]
    public async Task GetPageByIdAsync_WithNullId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _facade.GetPageByIdAsync(null!));
    }

    [Fact]
    public async Task CreatePageAsync_WithValidPage_CallsRepositoryAndSaves()
    {
        // Arrange
        var page = CreateTestPage("new-page");

        _mockRepository.Setup(r => r.AddAsync(page, default))
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _facade.CreatePageAsync(page);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(page, default), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreatePageAsync_WithNullPage_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _facade.CreatePageAsync(null!));
    }

    [Fact]
    public async Task UpdatePageAsync_WithValidPage_CallsRepositoryAndSaves()
    {
        // Arrange
        var page = CreateTestPage("existing-page");
        page.Id = "page-123";

        _mockRepository.Setup(r => r.UpdateAsync(page, default))
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _facade.UpdatePageAsync(page);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(page, default), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdatePageAsync_WithNullPage_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _facade.UpdatePageAsync(null!));
    }

    [Fact]
    public async Task DeletePageAsync_WithValidIds_CallsRepositoryAndSaves()
    {
        // Arrange
        var pageId = "page-123";
        var partitionKey = "pages";

        _mockRepository.Setup(r => r.DeleteAsync(pageId, partitionKey, true, default))
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _facade.DeletePageAsync(pageId, partitionKey);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(pageId, partitionKey, true, default), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeletePageAsync_WithInvalidId_ThrowsException(string? invalidId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _facade.DeletePageAsync(invalidId!, "partition"));
    }

    [Fact]
    public async Task DeletePageAsync_WithNullId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _facade.DeletePageAsync(null!, "partition"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeletePageAsync_WithInvalidPartitionKey_ThrowsException(string? invalidPartitionKey)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _facade.DeletePageAsync("id", invalidPartitionKey!));
    }

    [Fact]
    public async Task DeletePageAsync_WithNullPartitionKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _facade.DeletePageAsync("id", null!));
    }


    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PagesAdminFacade(null!));
    }

    private static Page CreateTestPage(string slug, bool isPublished = true)
    {
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "pages",
            Slug = slug,
            IsPublished = isPublished,
            Schedule = new ContentSchedule
            {
                PublishedAt = isPublished ? DateTimeOffset.UtcNow : null
            },
            Live = isPublished ? new PageContent
            {
                Title = $"Live Title for {slug}",
                Markdown = "# Live Content",
                Content = "<h1>Live Content</h1>",
                MetaDescription = "Live meta description",
                ShowTitle = true
            } : null,
            Draft = new PageContent
            {
                Title = $"Draft Title for {slug}",
                Markdown = "# Draft Content",
                Content = "<h1>Draft Content</h1>",
                MetaDescription = "Draft meta description",
                ShowTitle = true
            },
            AuthorId = "author-123",
            AuthorName = "Test Author",
            ViewCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        page.Draft.ComputeHash();
        if (page.Live != null)
        {
            page.Live.ComputeHash();
        }
        return page;
    }
}
