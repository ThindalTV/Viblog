using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Viblog.Admin.Facades;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Shared.Services.Content;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for PostsAdminFacade publish, schedule, and adopt operations.
/// </summary>
public class PostsAdminFacadePublishTests
{
    private readonly Mock<IBlogPostRepository> _mockRepository;
    private readonly Mock<ContentSchedulingService> _mockSchedulingService;
    private readonly PostsAdminFacade _facade;

    private const string TestUserId = "user-1";
    private const string TestUserName = "Test User";
    private const string TestUserEmail = "test@example.com";

    public PostsAdminFacadePublishTests()
    {
        _mockRepository = new Mock<IBlogPostRepository>();

        var mockVersionService = new Mock<ContentVersionService>(
            Mock.Of<IBlogPostVersionRepository>(),
            Mock.Of<IPageVersionRepository>(),
            Mock.Of<ILogger<ContentVersionService>>());

        _mockSchedulingService = new Mock<ContentSchedulingService>(
            mockVersionService.Object,
            Mock.Of<ILogger<ContentSchedulingService>>());

        _facade = new PostsAdminFacade(
            _mockRepository.Object,
            _mockSchedulingService.Object,
            httpContextAccessor: CreateMockHttpContextAccessor());
    }

    #region PublishPostNowAsync

    [Fact]
    public async Task PublishPostNowAsync_CallsSchedulingServiceAndSaves()
    {
        var post = CreateDraftPost();
        SetupRepositoryGetById(post);

        await _facade.PublishPostNowAsync(post.Id);

        _mockSchedulingService.Verify(
            s => s.PublishNowAsync(post, TestUserId, TestUserName, null, default),
            Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(post, default), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task PublishPostNowAsync_WhenPostNotFound_ThrowsInvalidOperationException()
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync((BlogPost?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _facade.PublishPostNowAsync("missing-id"));
    }

    [Fact]
    public async Task PublishPostNowAsync_WhenNullId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _facade.PublishPostNowAsync(string.Empty));
    }

    #endregion

    #region SchedulePostAsync

    [Fact]
    public async Task SchedulePostAsync_NewSchedule_CallsScheduleForPublish()
    {
        var post = CreateDraftPost();
        post.Schedule.Status = ContentStatus.Draft;
        SetupRepositoryGetById(post);

        var publishDate = DateTimeOffset.UtcNow.AddDays(1);
        await _facade.SchedulePostAsync(post.Id, publishDate);

        _mockSchedulingService.Verify(s => s.ScheduleForPublish(post, publishDate), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(post, default), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task SchedulePostAsync_UpdateExistingSchedule_CallsScheduleForPublish()
    {
        var post = CreateDraftPost();
        post.Schedule.Status = ContentStatus.Scheduled;
        post.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddDays(1);
        SetupRepositoryGetById(post);

        var newDate = DateTimeOffset.UtcNow.AddDays(3);
        await _facade.SchedulePostAsync(post.Id, newDate);

        _mockSchedulingService.Verify(s => s.ScheduleForPublish(post, newDate), Times.Once);
    }

    [Fact]
    public async Task SchedulePostAsync_WhenPostNotFound_ThrowsInvalidOperationException()
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync((BlogPost?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _facade.SchedulePostAsync("missing", DateTimeOffset.UtcNow.AddDays(1)));
    }

    #endregion

    #region CancelPostScheduleAsync

    [Fact]
    public async Task CancelPostScheduleAsync_ResetsScheduleStatusToDraft()
    {
        var post = CreateDraftPost();
        post.Schedule.Status = ContentStatus.Scheduled;
        post.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddDays(1);
        SetupRepositoryGetById(post);

        await _facade.CancelPostScheduleAsync(post.Id);

        Assert.Equal(ContentStatus.Draft, post.Schedule.Status);
        Assert.Null(post.Schedule.ScheduledPublishDate);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CancelPostScheduleAsync_WhenPostNotFound_ThrowsInvalidOperationException()
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync((BlogPost?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _facade.CancelPostScheduleAsync("missing"));
    }

    #endregion

    #region UnpublishPostAsync

    [Fact]
    public async Task UnpublishPostAsync_CallsSchedulingServiceUnpublish()
    {
        var post = CreatePublishedPost();
        SetupRepositoryGetById(post);

        await _facade.UnpublishPostAsync(post.Id);

        _mockSchedulingService.Verify(s => s.Unpublish(post), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(post, default), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UnpublishPostAsync_WhenPostNotFound_ThrowsInvalidOperationException()
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync((BlogPost?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _facade.UnpublishPostAsync("missing"));
    }

    #endregion

    #region AdoptPostAsync

    [Fact]
    public async Task AdoptPostAsync_SetsAuthorIdAndName()
    {
        var post = CreateDraftPost();
        post.AuthorId = "original-author";
        post.AuthorName = "Original Author";
        SetupRepositoryGetById(post);

        await _facade.AdoptPostAsync(post.Id);

        Assert.Equal(TestUserId, post.AuthorId);
        Assert.Equal(TestUserName, post.AuthorName);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task AdoptPostAsync_WhenPostNotFound_ThrowsInvalidOperationException()
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(It.IsAny<string>(), default))
            .ReturnsAsync((BlogPost?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _facade.AdoptPostAsync("missing"));
    }

    #endregion

    #region Helper Methods

    private void SetupRepositoryGetById(BlogPost post)
    {
        _mockRepository
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(post.Id, default))
            .ReturnsAsync(post);
        _mockRepository.Setup(r => r.UpdateAsync(post, default)).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);
    }

    private static BlogPost CreateDraftPost() => new BlogPost
    {
        Id = Guid.NewGuid().ToString(),
        GroupKey = "blog-posts",
        Slug = "test-post",
        Draft = new BlogPostContent { Title = "Test Post", Markdown = "Content" },
        Live = null,
        Schedule = new ContentSchedule()
    };

    private static BlogPost CreatePublishedPost()
    {
        var post = CreateDraftPost();
        post.Live = new BlogPostContent { Title = "Test Post", Markdown = "Content" };
        return post;
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
