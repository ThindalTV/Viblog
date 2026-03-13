using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Viblog.Admin.Facades;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Shared.Services.Content;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for PagesAdminFacade publish, schedule, and adopt operations.
/// </summary>
public class PagesAdminFacadePublishTests
{
    private readonly Mock<IPageRepository> _mockRepository;
    private readonly Mock<ContentSchedulingService> _mockSchedulingService;
    private readonly PagesAdminFacade _facade;

    private const string TestUserId = "user-1";
    private const string TestUserName = "Test User";
    private const string TestUserEmail = "test@example.com";

    public PagesAdminFacadePublishTests()
    {
        _mockRepository = new Mock<IPageRepository>();

        var mockVersionService = new Mock<ContentVersionService>(
            Mock.Of<IBlogPostVersionRepository>(),
            Mock.Of<IPageVersionRepository>(),
            Mock.Of<ILogger<ContentVersionService>>());

        _mockSchedulingService = new Mock<ContentSchedulingService>(
            mockVersionService.Object,
            Mock.Of<ILogger<ContentSchedulingService>>());

        _facade = new PagesAdminFacade(
            _mockRepository.Object,
            _mockSchedulingService.Object,
            httpContextAccessor: CreateMockHttpContextAccessor());
    }

    #region PublishPageNowAsync

    [Fact]
    public async Task PublishPageNowAsync_CallsSchedulingServiceAndSaves()
    {
        var page = CreateDraftPage();
        SetupRepositoryGetById(page);

        await _facade.PublishPageNowAsync(page.Id);

        _mockSchedulingService.Verify(
            s => s.PublishNowAsync(page, TestUserId, TestUserName, null, default),
            Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(page, default), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task PublishPageNowAsync_WhenPageNotFound_ThrowsInvalidOperationException()
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Page?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _facade.PublishPageNowAsync("missing-id"));
    }

    [Fact]
    public async Task PublishPageNowAsync_WhenNullId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _facade.PublishPageNowAsync(string.Empty));
    }

    #endregion

    #region SchedulePagePublishingAsync

    [Fact]
    public async Task SchedulePagePublishingAsync_NewSchedule_CallsScheduleForPublish()
    {
        var page = CreateDraftPage();
        page.Schedule.Status = ContentStatus.Draft;
        SetupRepositoryGetById(page);

        var publishDate = DateTimeOffset.UtcNow.AddDays(1);
        await _facade.SchedulePagePublishingAsync(page.Id, publishDate);

        _mockSchedulingService.Verify(s => s.ScheduleForPublish(page, publishDate), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(page, default), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task SchedulePagePublishingAsync_UpdateExistingSchedule_CallsScheduleForPublish()
    {
        var page = CreateDraftPage();
        page.Schedule.Status = ContentStatus.Scheduled;
        page.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddDays(1);
        SetupRepositoryGetById(page);

        var newDate = DateTimeOffset.UtcNow.AddDays(3);
        await _facade.SchedulePagePublishingAsync(page.Id, newDate);

        _mockSchedulingService.Verify(s => s.ScheduleForPublish(page, newDate), Times.Once);
    }

    [Fact]
    public async Task SchedulePagePublishingAsync_WhenPageNotFound_ThrowsInvalidOperationException()
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Page?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _facade.SchedulePagePublishingAsync("missing", DateTimeOffset.UtcNow.AddDays(1)));
    }

    #endregion

    #region CancelPageScheduleAsync

    [Fact]
    public async Task CancelPageScheduleAsync_ResetsScheduleStatusToDraft()
    {
        var page = CreateDraftPage();
        page.Schedule.Status = ContentStatus.Scheduled;
        page.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddDays(1);
        SetupRepositoryGetById(page);

        await _facade.CancelPageScheduleAsync(page.Id);

        Assert.Equal(ContentStatus.Draft, page.Schedule.Status);
        Assert.Null(page.Schedule.ScheduledPublishDate);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CancelPageScheduleAsync_WhenPageNotFound_ThrowsInvalidOperationException()
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Page?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _facade.CancelPageScheduleAsync("missing"));
    }

    #endregion

    #region UnpublishPageAsync

    [Fact]
    public async Task UnpublishPageAsync_CallsSchedulingServiceUnpublish()
    {
        var page = CreatePublishedPage();
        SetupRepositoryGetById(page);

        await _facade.UnpublishPageAsync(page.Id);

        _mockSchedulingService.Verify(s => s.Unpublish(page), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(page, default), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UnpublishPageAsync_WhenPageNotFound_ThrowsInvalidOperationException()
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Page?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _facade.UnpublishPageAsync("missing"));
    }

    #endregion

    #region AdoptPageAsync

    [Fact]
    public async Task AdoptPageAsync_SetsAuthorIdAndName()
    {
        var page = CreateDraftPage();
        page.AuthorId = "original-author";
        page.AuthorName = "Original Author";
        SetupRepositoryGetById(page);

        await _facade.AdoptPageAsync(page.Id);

        Assert.Equal(TestUserId, page.AuthorId);
        Assert.Equal(TestUserName, page.AuthorName);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task AdoptPageAsync_WhenPageNotFound_ThrowsInvalidOperationException()
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Page?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _facade.AdoptPageAsync("missing"));
    }

    #endregion

    #region Helper Methods

    private void SetupRepositoryGetById(Page page)
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(page.Id, default))
            .ReturnsAsync(page);
        _mockRepository.Setup(r => r.UpdateAsync(page, default)).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);
    }

    private static Page CreateDraftPage() => new Page
    {
        Id = Guid.NewGuid().ToString(),
        GroupKey = "pages",
        Slug = "test-page",
        Draft = new PageContent { Title = "Test Page", Markdown = "Content" },
        Live = null,
        Schedule = new ContentSchedule()
    };

    private static Page CreatePublishedPage()
    {
        var page = CreateDraftPage();
        page.Live = new PageContent { Title = "Test Page", Markdown = "Content" };
        return page;
    }

    private static IHttpContextAccessor CreateMockHttpContextAccessor()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, TestUserId),
            new(ClaimTypes.Name, TestUserName),
            new(ClaimTypes.Email, TestUserEmail)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        var mock = new Mock<IHttpContextAccessor>();
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock.Object;
    }

    #endregion
}
