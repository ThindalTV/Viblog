using Viblog.Frontend.Facades;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Shared.Extensions;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for PageDetailFacade
/// </summary>
public class PageDetailFacadeTests
{
    private readonly Mock<IPageRepository> _mockRepository;
    private readonly PageDetailFacade _facade;

    public PageDetailFacadeTests()
    {
        _mockRepository = new Mock<IPageRepository>();
        _facade = new PageDetailFacade(_mockRepository.Object);
    }

    [Fact]
    public async Task GetPageBySlugAsync_WhenPageExists_ReturnsPage()
    {
        // Arrange
        var slug = "about";
        var expectedPage = CreateTestPage(slug);

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, true, default))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _facade.GetPageBySlugAsync(slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(slug, result.Slug);
        Assert.NotNull(result.Live);
        Assert.Equal(expectedPage?.Live?.Title, result.Live.Title);
        Assert.True(result.IsPublished);
    }

    [Fact]
    public async Task GetPageBySlugAsync_WhenPageNotFound_ReturnsNull()
    {
        // Arrange
        var slug = "nonexistent-page";

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, true, default))
            .ReturnsAsync((Page?)null);

        // Act
        var result = await _facade.GetPageBySlugAsync(slug);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPageBySlugAsync_CallsRepositoryWithPublishedOnly()
    {
        // Arrange
        var slug = "contact";

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, true, default))
            .ReturnsAsync((Page?)null);

        // Act
        await _facade.GetPageBySlugAsync(slug);

        // Assert
        _mockRepository.Verify(r => r.GetBySlugAsync(slug, true, default), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task GetPageBySlugAsync_WithInvalidSlug_ReturnsNull(string? invalidSlug)
    {
        // Act
        var result = await _facade.GetPageBySlugAsync(invalidSlug!);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetBySlugAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IncrementViewCountAsync_CallsRepository()
    {
        // Arrange
        var id = "page-123";
        var partitionKey = "pages";

        _mockRepository.Setup(r => r.IncrementViewCountAsync(id, partitionKey, default))
            .Returns(Task.CompletedTask);

        // Act
        await _facade.IncrementViewCountAsync(id, partitionKey);

        // Assert
        _mockRepository.Verify(r => r.IncrementViewCountAsync(id, partitionKey, default), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IncrementViewCountAsync_WithInvalidId_ThrowsException(string? invalidId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _facade.IncrementViewCountAsync(invalidId!, "partition"));
    }

    [Fact]
    public async Task IncrementViewCountAsync_WithNullId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _facade.IncrementViewCountAsync(null!, "partition"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IncrementViewCountAsync_WithInvalidPartitionKey_ThrowsException(string? invalidPartitionKey)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _facade.IncrementViewCountAsync("id", invalidPartitionKey!));
    }

    [Fact]
    public async Task IncrementViewCountAsync_WithNullPartitionKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _facade.IncrementViewCountAsync("id", null!));
    }

    [Fact]
    public async Task GetPageBySlugAsync_WhenScheduledPageIsReady_ReturnsPromotedPage()
    {
        // Arrange
        var slug = "scheduled-page";
        var page = CreateTestPage(slug, isPublished: false, hasScheduledPublish: true);

        _mockRepository.Setup(r => r.GetBySlugAsync(slug, true, default))
            .ReturnsAsync(page);

        // Act
        var result = await _facade.GetPageBySlugAsync(slug);

        // Assert - Repository handles promotion, so we just verify the call
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetBySlugAsync(slug, true, default), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PageDetailFacade(null!));
    }

    private static Page CreateTestPage(string slug, bool isPublished = true, bool hasScheduledPublish = false)
    {
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "pages",
            Slug = slug,
            IsPublished = isPublished,
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

        if (hasScheduledPublish)
        {
            page.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddMinutes(-5); // Past date, ready to publish
        }

        return page;
    }
}
