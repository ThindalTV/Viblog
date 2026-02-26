using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Shared.Services.Content;

namespace Viblog.Tests.Services;

/// <summary>
/// Unit tests for ContentSchedulingService.
/// ContentVersionService is mocked because its repository dependencies are out of scope here;
/// separate ContentVersionServiceTests cover the version-service behaviour directly.
/// </summary>
public class ContentSchedulingServiceTests
{
    private readonly Mock<ContentVersionService> _mockVersionService;
    private readonly ContentSchedulingService _service;

    public ContentSchedulingServiceTests()
    {
        _mockVersionService = new Mock<ContentVersionService>(
            Mock.Of<IBlogPostVersionRepository>(),
            Mock.Of<IPageVersionRepository>(),
            Mock.Of<ILogger<ContentVersionService>>());

        _service = new ContentSchedulingService(
            _mockVersionService.Object,
            Mock.Of<ILogger<ContentSchedulingService>>());
    }

    #region PublishNowAsync

    [Fact]
    public async Task PublishNowAsync_FirstPublish_SetsPublishedAtOnBlogPost()
    {
        var post = CreateBlogPost(publishedAt: null);

        await _service.PublishNowAsync(post, "user1");

        Assert.NotNull(post.PublishedAt);
    }

    [Fact]
    public async Task PublishNowAsync_RePublish_DoesNotChangeOriginalPublishedAt()
    {
        var originalDate = DateTimeOffset.UtcNow.AddDays(-10);
        var post = CreateBlogPost(publishedAt: originalDate);

        await _service.PublishNowAsync(post, "user1");

        Assert.Equal(originalDate, post.PublishedAt);
    }

    [Fact]
    public async Task PublishNowAsync_ClearsScheduledPublishDate()
    {
        var post = CreateBlogPost();
        post.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddDays(1);
        post.Schedule.Status = ContentStatus.Scheduled;

        await _service.PublishNowAsync(post, "user1");

        Assert.Null(post.Schedule.ScheduledPublishDate);
    }

    [Fact]
    public async Task PublishNowAsync_SetsScheduleStatusToDraft()
    {
        var post = CreateBlogPost();
        post.Schedule.Status = ContentStatus.Scheduled;

        await _service.PublishNowAsync(post, "user1");

        Assert.Equal(ContentStatus.Draft, post.Schedule.Status);
    }

    [Fact]
    public async Task PublishNowAsync_SetsSchedulePublishedAt()
    {
        var post = CreateBlogPost();
        var before = DateTimeOffset.UtcNow;

        await _service.PublishNowAsync(post, "user1");

        Assert.NotNull(post.Schedule.PublishedAt);
        Assert.True(post.Schedule.PublishedAt >= before);
    }

    [Fact]
    public async Task PublishNowAsync_PropagatesExceptionFromVersionService()
    {
        var post = CreateBlogPost();
        _mockVersionService
            .Setup(v => v.PromoteDraftToLiveAsync(post, "user1", null, default))
            .ThrowsAsync(new InvalidOperationException("Cannot publish content without Draft"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.PublishNowAsync(post, "user1"));
    }

    #endregion

    #region ScheduleForPublish

    [Fact]
    public void ScheduleForPublish_FutureDate_SetsStatusToScheduled()
    {
        var post = CreateBlogPost();
        var publishDate = DateTimeOffset.UtcNow.AddDays(1);

        _service.ScheduleForPublish(post, publishDate);

        Assert.Equal(ContentStatus.Scheduled, post.Schedule.Status);
        Assert.Equal(publishDate, post.Schedule.ScheduledPublishDate);
    }

    [Fact]
    public void ScheduleForPublish_PastDate_ThrowsArgumentException()
    {
        var post = CreateBlogPost();

        Assert.Throws<ArgumentException>(() =>
            _service.ScheduleForPublish(post, DateTimeOffset.UtcNow.AddDays(-1)));
    }

    [Fact]
    public void ScheduleForPublish_NowDate_ThrowsArgumentException()
    {
        var post = CreateBlogPost();

        Assert.Throws<ArgumentException>(() =>
            _service.ScheduleForPublish(post, DateTimeOffset.UtcNow.AddSeconds(-1)));
    }

    #endregion

    #region Unpublish

    [Fact]
    public void Unpublish_CallsClearLiveOnVersionService()
    {
        var post = CreateBlogPost();

        _service.Unpublish(post);

        _mockVersionService.Verify(v => v.ClearLive(post), Times.Once);
    }

    [Fact]
    public void Unpublish_ResetsScheduleStatusToDraft()
    {
        var post = CreateBlogPost();
        post.Schedule.Status = ContentStatus.Scheduled;
        post.Schedule.ScheduledPublishDate = DateTimeOffset.UtcNow.AddDays(1);

        _service.Unpublish(post);

        Assert.Equal(ContentStatus.Draft, post.Schedule.Status);
        Assert.Null(post.Schedule.ScheduledPublishDate);
    }

    [Fact]
    public void Unpublish_WhenNotPublished_DoesNotThrow()
    {
        var post = CreateBlogPost();
        post.Live = null;

        var exception = Record.Exception(() => _service.Unpublish(post));

        Assert.Null(exception);
    }

    #endregion

    private static BlogPost CreateBlogPost(DateTimeOffset? publishedAt = null) => new()
    {
        Id = Guid.NewGuid().ToString(),
        GroupKey = "posts",
        Draft = new BlogPostContent { Title = "Test Post", Markdown = "Content" },
        PublishedAt = publishedAt
    };
}
