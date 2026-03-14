using System.Linq.Expressions;
using Viblog.Admin.Facades;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Infrastructure.Facades;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for DashboardFacade covering all five query methods.
/// </summary>
public class DashboardFacadeTests
{
    private readonly Mock<IBlogPostRepository> _mockRepository;
    private readonly Mock<IAuditLogFacade> _mockAuditLogFacade;
    private readonly DashboardFacade _facade;

    public DashboardFacadeTests()
    {
        _mockRepository = new Mock<IBlogPostRepository>();
        _mockAuditLogFacade = new Mock<IAuditLogFacade>();
        _facade = new DashboardFacade(_mockRepository.Object, _mockAuditLogFacade.Object);
    }

    #region Constructor

    [Fact]
    public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DashboardFacade(null!, _mockAuditLogFacade.Object));
    }

    [Fact]
    public void Constructor_WhenAuditLogFacadeIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DashboardFacade(_mockRepository.Object, null!));
    }

    #endregion

    #region GetPostStatsAsync

    [Fact]
    public async Task GetPostStatsAsync_WhenNoPosts_ReturnsAllZeros()
    {
        SetupGetAllAsync([]);

        var result = await _facade.GetPostStatsAsync();

        Assert.Equal(0, result.PublishedCount);
        Assert.Equal(0, result.DraftCount);
        Assert.Equal(0, result.ScheduledCount);
        Assert.Equal(0, result.TotalViews);
    }

    [Fact]
    public async Task GetPostStatsAsync_CountsPublishedPostsCorrectly()
    {
        SetupGetAllAsync([
            CreatePublishedPost(viewCount: 100),
            CreatePublishedPost(viewCount: 50),
            CreateDraftPost(),
        ]);

        var result = await _facade.GetPostStatsAsync();

        Assert.Equal(2, result.PublishedCount);
    }

    [Fact]
    public async Task GetPostStatsAsync_CountsDraftPostsCorrectly()
    {
        SetupGetAllAsync([
            CreatePublishedPost(),
            CreateDraftPost(),
            CreateDraftPost(),
        ]);

        var result = await _facade.GetPostStatsAsync();

        Assert.Equal(2, result.DraftCount);
    }

    [Fact]
    public async Task GetPostStatsAsync_CountsScheduledPostsCorrectly()
    {
        SetupGetAllAsync([
            CreatePublishedPost(),
            CreateScheduledPost(DateTimeOffset.UtcNow.AddDays(1)),
            CreateScheduledPost(DateTimeOffset.UtcNow.AddDays(2)),
        ]);

        var result = await _facade.GetPostStatsAsync();

        Assert.Equal(2, result.ScheduledCount);
    }

    [Fact]
    public async Task GetPostStatsAsync_SumsTotalViewsAcrossAllPosts()
    {
        SetupGetAllAsync([
            CreatePublishedPost(viewCount: 100),
            CreatePublishedPost(viewCount: 200),
            CreateDraftPost(viewCount: 50),
        ]);

        var result = await _facade.GetPostStatsAsync();

        Assert.Equal(350, result.TotalViews);
    }

    [Fact]
    public async Task GetPostStatsAsync_WhenMultiplePages_AggregatesAllPages()
    {
        BlogPost[] page1 = [CreatePublishedPost(viewCount: 100), CreatePublishedPost(viewCount: 200)];
        BlogPost[] page2 = [CreateDraftPost(viewCount: 50)];

        _mockRepository
            .SetupSequence(r => r.GetAllAsync(
                It.IsAny<PagingParameters>(),
                It.IsAny<Expression<Func<BlogPost, DateTimeOffset>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePagedResult(page1, pageNumber: 1, totalCount: 3, pageSize: 2))
            .ReturnsAsync(CreatePagedResult(page2, pageNumber: 2, totalCount: 3, pageSize: 2));

        var result = await _facade.GetPostStatsAsync();

        Assert.Equal(2, result.PublishedCount);
        Assert.Equal(1, result.DraftCount);
        Assert.Equal(350, result.TotalViews);
    }

    #endregion

    #region GetTopPostsByViewsAsync

    [Fact]
    public async Task GetTopPostsByViewsAsync_ReturnsPosts()
    {
        SetupFindAsyncByViewCount([
            CreatePublishedPost(viewCount: 500),
            CreatePublishedPost(viewCount: 300),
        ]);

        var result = await _facade.GetTopPostsByViewsAsync(DashboardDateFilter.Ever);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTopPostsByViewsAsync_WhenNoPosts_ReturnsEmptyList()
    {
        SetupFindAsyncByViewCount([]);

        var result = await _facade.GetTopPostsByViewsAsync(DashboardDateFilter.Ever);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(DashboardDateFilter.Ever)]
    [InlineData(DashboardDateFilter.LastDay)]
    [InlineData(DashboardDateFilter.LastWeek)]
    [InlineData(DashboardDateFilter.LastMonth)]
    [InlineData(DashboardDateFilter.LastYear)]
    public async Task GetTopPostsByViewsAsync_AllFilters_QueriesRepositoryOnce(DashboardDateFilter filter)
    {
        SetupFindAsyncByViewCount([]);

        await _facade.GetTopPostsByViewsAsync(filter);

        _mockRepository.Verify(r => r.FindAsync(
            It.IsAny<Expression<Func<BlogPost, bool>>>(),
            It.IsAny<PagingParameters>(),
            It.IsAny<Expression<Func<BlogPost, int>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetViewsByPublishedMonthAsync

    [Fact]
    public async Task GetViewsByPublishedMonthAsync_GroupsPostsByMonth()
    {
        var june = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

        SetupFindAsyncByPublishedAt([
            CreatePublishedPost(viewCount: 100, publishedAt: june),
            CreatePublishedPost(viewCount: 200, publishedAt: june.AddDays(5)),
            CreatePublishedPost(viewCount: 50,  publishedAt: june.AddMonths(1)),
        ]);

        var result = await _facade.GetViewsByPublishedMonthAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetViewsByPublishedMonthAsync_SumsViewsWithinSameMonth()
    {
        var june = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

        SetupFindAsyncByPublishedAt([
            CreatePublishedPost(viewCount: 100, publishedAt: june),
            CreatePublishedPost(viewCount: 200, publishedAt: june.AddDays(10)),
        ]);

        var result = await _facade.GetViewsByPublishedMonthAsync();

        Assert.Equal(300, result[0].TotalViews);
    }

    [Fact]
    public async Task GetViewsByPublishedMonthAsync_UsesFullMonthNameAsLabel()
    {
        SetupFindAsyncByPublishedAt([
            CreatePublishedPost(viewCount: 1, publishedAt: new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero)),
        ]);

        var result = await _facade.GetViewsByPublishedMonthAsync();

        Assert.Equal("June", result[0].Month);
    }

    [Fact]
    public async Task GetViewsByPublishedMonthAsync_OrdersMonthsChronologically()
    {
        SetupFindAsyncByPublishedAt([
            CreatePublishedPost(viewCount: 50,  publishedAt: new DateTimeOffset(2024, 8, 1, 0, 0, 0, TimeSpan.Zero)),
            CreatePublishedPost(viewCount: 100, publishedAt: new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero)),
            CreatePublishedPost(viewCount: 75,  publishedAt: new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.Zero)),
        ]);

        var result = await _facade.GetViewsByPublishedMonthAsync();

        Assert.Equal("June",   result[0].Month);
        Assert.Equal("July",   result[1].Month);
        Assert.Equal("August", result[2].Month);
    }

    [Fact]
    public async Task GetViewsByPublishedMonthAsync_WhenMultiplePages_AggregatesAllPages()
    {
        var june = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var july = new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.Zero);

        BlogPost[] page1 = [CreatePublishedPost(viewCount: 100, publishedAt: june)];
        BlogPost[] page2 = [CreatePublishedPost(viewCount: 50,  publishedAt: july)];

        _mockRepository
            .SetupSequence(r => r.FindAsync(
                It.IsAny<Expression<Func<BlogPost, bool>>>(),
                It.IsAny<PagingParameters>(),
                It.IsAny<Expression<Func<BlogPost, DateTimeOffset?>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePagedResult(page1, pageNumber: 1, totalCount: 2, pageSize: 1))
            .ReturnsAsync(CreatePagedResult(page2, pageNumber: 2, totalCount: 2, pageSize: 1));

        var result = await _facade.GetViewsByPublishedMonthAsync();

        Assert.Equal(2,   result.Count);
        Assert.Equal(100, result[0].TotalViews);
        Assert.Equal(50,  result[1].TotalViews);
    }

    #endregion

    #region GetScheduledPostsAsync

    [Fact]
    public async Task GetScheduledPostsAsync_ReturnsPosts()
    {
        SetupFindAsyncByScheduledDate([
            CreateScheduledPost(DateTimeOffset.UtcNow.AddDays(1)),
        ]);

        var result = await _facade.GetScheduledPostsAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task GetScheduledPostsAsync_WhenNoPosts_ReturnsEmptyList()
    {
        SetupFindAsyncByScheduledDate([]);

        var result = await _facade.GetScheduledPostsAsync();

        Assert.Empty(result);
    }

    #endregion

    #region GetRecentActivityAsync

    [Fact]
    public async Task GetRecentActivityAsync_DelegatesTo_AuditLogFacadeWithCount10()
    {
        _mockAuditLogFacade
            .Setup(f => f.GetRecentActivityAsync(10, default))
            .ReturnsAsync([]);

        await _facade.GetRecentActivityAsync();

        _mockAuditLogFacade.Verify(f => f.GetRecentActivityAsync(10, default), Times.Once);
    }

    [Fact]
    public async Task GetRecentActivityAsync_ReturnsLogsFromAuditLogFacade()
    {
        AuditLog[] logs = [new AuditLog { Id = "log-1" }, new AuditLog { Id = "log-2" }];

        _mockAuditLogFacade
            .Setup(f => f.GetRecentActivityAsync(10, default))
            .ReturnsAsync(logs);

        var result = await _facade.GetRecentActivityAsync();

        Assert.Equal(2, result.Count);
    }

    #endregion

    #region Helpers

    private void SetupGetAllAsync(IList<BlogPost> posts)
    {
        _mockRepository
            .Setup(r => r.GetAllAsync(
                It.IsAny<PagingParameters>(),
                It.IsAny<Expression<Func<BlogPost, DateTimeOffset>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSinglePageResult(posts));
    }

    private void SetupFindAsyncByViewCount(IList<BlogPost> posts)
    {
        _mockRepository
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<BlogPost, bool>>>(),
                It.IsAny<PagingParameters>(),
                It.IsAny<Expression<Func<BlogPost, int>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSinglePageResult(posts));
    }

    private void SetupFindAsyncByPublishedAt(IList<BlogPost> posts)
    {
        _mockRepository
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<BlogPost, bool>>>(),
                It.IsAny<PagingParameters>(),
                It.IsAny<Expression<Func<BlogPost, DateTimeOffset?>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSinglePageResult(posts));
    }

    private void SetupFindAsyncByScheduledDate(IList<BlogPost> posts)
    {
        _mockRepository
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<BlogPost, bool>>>(),
                It.IsAny<PagingParameters>(),
                It.IsAny<Expression<Func<BlogPost, DateTimeOffset?>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSinglePageResult(posts));
    }

    private static PagedResult<BlogPost> CreateSinglePageResult(IList<BlogPost> items) =>
        CreatePagedResult(items, pageNumber: 1, totalCount: items.Count, pageSize: Math.Max(1, items.Count));

    private static PagedResult<BlogPost> CreatePagedResult(
        IEnumerable<BlogPost> items,
        int pageNumber,
        int totalCount,
        int pageSize) =>
        new(items, totalCount, pageNumber, pageSize);

    private static BlogPost CreatePublishedPost(int viewCount = 0, DateTimeOffset? publishedAt = null) => new()
    {
        Id = Guid.NewGuid().ToString(),
        GroupKey = "blog-posts",
        Draft = new BlogPostContent { Title = "Test Post" },
        Live = new BlogPostContent { Title = "Test Post" },
        IsPublished = true,
        PublishedAt = publishedAt ?? DateTimeOffset.UtcNow,
        ViewCount = viewCount,
        Schedule = new ContentSchedule { Status = ContentStatus.Draft }
    };

    private static BlogPost CreateDraftPost(int viewCount = 0) => new()
    {
        Id = Guid.NewGuid().ToString(),
        GroupKey = "blog-posts",
        Draft = new BlogPostContent { Title = "Draft Post" },
        IsPublished = false,
        ViewCount = viewCount,
        Schedule = new ContentSchedule { Status = ContentStatus.Draft }
    };

    private static BlogPost CreateScheduledPost(DateTimeOffset scheduledDate) => new()
    {
        Id = Guid.NewGuid().ToString(),
        GroupKey = "blog-posts",
        Draft = new BlogPostContent { Title = "Scheduled Post" },
        IsPublished = false,
        Schedule = new ContentSchedule
        {
            Status = ContentStatus.Scheduled,
            ScheduledPublishDate = scheduledDate
        }
    };

    #endregion
}
