using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Shared.Services.Content;

namespace Viblog.Tests.Services;

/// <summary>
/// Unit tests for ContentVersionService.
/// </summary>
public class ContentVersionServiceTests
{
    private readonly Mock<IBlogPostVersionRepository> _mockBlogVersionRepo;
    private readonly Mock<IPageVersionRepository> _mockPageVersionRepo;
    private readonly ContentVersionService _service;

    public ContentVersionServiceTests()
    {
        _mockBlogVersionRepo = new Mock<IBlogPostVersionRepository>();
        _mockPageVersionRepo = new Mock<IPageVersionRepository>();

        _mockBlogVersionRepo
            .Setup(r => r.GetLatestVersionNumberAsync(It.IsAny<string>(), default))
            .ReturnsAsync(0);
        _mockBlogVersionRepo
            .Setup(r => r.AddAsync(It.IsAny<BlogPostVersion>(), default))
            .Returns(Task.CompletedTask);
        _mockBlogVersionRepo
            .Setup(r => r.SaveChangesAsync(default))
            .ReturnsAsync(1);
        _mockBlogVersionRepo
            .Setup(r => r.GetVersionsForContentAsync(It.IsAny<string>(), default))
            .ReturnsAsync(Enumerable.Empty<BlogPostVersion>());

        _mockPageVersionRepo
            .Setup(r => r.GetLatestVersionNumberAsync(It.IsAny<string>(), default))
            .ReturnsAsync(0);
        _mockPageVersionRepo
            .Setup(r => r.AddAsync(It.IsAny<PageVersion>(), default))
            .Returns(Task.CompletedTask);
        _mockPageVersionRepo
            .Setup(r => r.SaveChangesAsync(default))
            .ReturnsAsync(1);
        _mockPageVersionRepo
            .Setup(r => r.GetVersionsForContentAsync(It.IsAny<string>(), default))
            .ReturnsAsync(Enumerable.Empty<PageVersion>());

        _service = new ContentVersionService(
            _mockBlogVersionRepo.Object,
            _mockPageVersionRepo.Object,
            Mock.Of<ILogger<ContentVersionService>>());
    }

    #region PromoteDraftToLiveAsync — BlogPost

    [Fact]
    public async Task PromoteDraftToLiveAsync_CopiesDraftFieldsToLive()
    {
        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "posts",
            Draft = new BlogPostContent
            {
                Title = "My Title",
                Markdown = "# Hello World",
                Short = "Excerpt",
                FeaturedImageUrl = "https://example.com/image.jpg",
                MetaDescription = "Meta desc"
            }
        };

        await _service.PromoteDraftToLiveAsync(post, "user1");

        Assert.NotNull(post.Live);
        Assert.Equal(post.Draft.Title, post.Live.Title);
        Assert.Equal(post.Draft.Markdown, post.Live.Markdown);
        Assert.Equal(post.Draft.Short, post.Live.Short);
        Assert.Equal(post.Draft.FeaturedImageUrl, post.Live.FeaturedImageUrl);
        Assert.Equal(post.Draft.MetaDescription, post.Live.MetaDescription);
    }

    [Fact]
    public async Task PromoteDraftToLiveAsync_LiveIsIndependentCopy()
    {
        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "posts",
            Draft = new BlogPostContent { Title = "Original Title", Markdown = "Content" }
        };

        await _service.PromoteDraftToLiveAsync(post, "user1");
        post.Draft.Title = "Updated Draft Title";

        Assert.Equal("Original Title", post.Live!.Title);
    }

    [Fact]
    public async Task PromoteDraftToLiveAsync_CreatesVersionSnapshot()
    {
        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "posts",
            Draft = new BlogPostContent { Title = "Test", Markdown = "Content" }
        };

        await _service.PromoteDraftToLiveAsync(post, "user1");

        _mockBlogVersionRepo.Verify(r => r.AddAsync(It.IsAny<BlogPostVersion>(), default), Times.Once);
        _mockBlogVersionRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task PromoteDraftToLiveAsync_VersionNumberIncrements()
    {
        _mockBlogVersionRepo
            .Setup(r => r.GetLatestVersionNumberAsync(It.IsAny<string>(), default))
            .ReturnsAsync(5);

        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "posts",
            Draft = new BlogPostContent { Title = "Test", Markdown = "Content" }
        };

        await _service.PromoteDraftToLiveAsync(post, "user1");

        _mockBlogVersionRepo.Verify(r => r.AddAsync(
            It.Is<BlogPostVersion>(v => v.VersionNumber == 6), default), Times.Once);
    }

    [Fact]
    public async Task PromoteDraftToLiveAsync_NullDraft_ThrowsInvalidOperationException()
    {
        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "posts"
        };
        post.Draft = null!; // Force null to trigger the guard

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.PromoteDraftToLiveAsync(post, "user1"));
    }

    #endregion

    #region PromoteDraftToLiveAsync — Page

    [Fact]
    public async Task PromoteDraftToLiveAsync_ForPage_CopiesDraftFieldsToLive()
    {
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "pages",
            Draft = new PageContent { Title = "Page Title", Markdown = "Page content" }
        };

        await _service.PromoteDraftToLiveAsync(page, "user1");

        Assert.NotNull(page.Live);
        Assert.Equal(page.Draft.Title, page.Live.Title);
        Assert.Equal(page.Draft.Markdown, page.Live.Markdown);
    }

    [Fact]
    public async Task PromoteDraftToLiveAsync_ForPage_CreatesVersionSnapshot()
    {
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "pages",
            Draft = new PageContent { Title = "Test", Markdown = "Content" }
        };

        await _service.PromoteDraftToLiveAsync(page, "user1");

        _mockPageVersionRepo.Verify(r => r.AddAsync(It.IsAny<PageVersion>(), default), Times.Once);
        _mockPageVersionRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    #endregion

    #region ClearLive

    [Fact]
    public void ClearLive_ForBlogPost_SetsLiveToNull()
    {
        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "posts",
            Draft = new BlogPostContent { Title = "Test" },
            Live = new BlogPostContent { Title = "Live" }
        };

        _service.ClearLive(post);

        Assert.Null(post.Live);
    }

    [Fact]
    public void ClearLive_ForPage_SetsLiveToNull()
    {
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "pages",
            Draft = new PageContent { Title = "Test" },
            Live = new PageContent { Title = "Live" }
        };

        _service.ClearLive(page);

        Assert.Null(page.Live);
    }

    #endregion

    #region CloneContent

    [Fact]
    public void CloneContent_CreatesDeepCopyWithAllBaseFields()
    {
        var source = new BlogPostContent
        {
            Title = "Title",
            Markdown = "Content",
            Short = "Excerpt",
            FeaturedImageUrl = "https://example.com/image.jpg",
            FeaturedImageAlt = "Alt text",
            MetaDescription = "Meta",
            MetaKeywords = "kw1,kw2"
        };

        var clone = _service.CloneContent(source) as BlogPostContent;

        Assert.NotNull(clone);
        Assert.Equal(source.Title, clone.Title);
        Assert.Equal(source.Markdown, clone.Markdown);
        Assert.Equal(source.Short, clone.Short);
        Assert.Equal(source.FeaturedImageUrl, clone.FeaturedImageUrl);
        Assert.Equal(source.FeaturedImageAlt, clone.FeaturedImageAlt);
        Assert.Equal(source.MetaDescription, clone.MetaDescription);
        Assert.Equal(source.MetaKeywords, clone.MetaKeywords);
        Assert.NotSame(source, clone);
    }

    #endregion
}
