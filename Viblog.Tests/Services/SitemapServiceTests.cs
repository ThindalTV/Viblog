using Microsoft.Extensions.Options;
using Moq;
using Vilog.Shared.Configuration;
using Vilog.Shared.Data.Common;
using Vilog.Shared.Data.Entities;
using Vilog.Shared.Data.Repositories;
using Vilog.Shared.Services;

namespace Vilog.Tests.Services;

public class SitemapServiceTests
{
    private readonly Mock<IBlogPostRepository> _mockRepository;
    private readonly SiteMetadata _siteMetadata;
    private readonly SitemapService _service;

    public SitemapServiceTests()
    {
        _mockRepository = new Mock<IBlogPostRepository>();
        _siteMetadata = new SiteMetadata
        {
            BaseUrl = "https://example.com",
            SiteName = "Test Blog",
            DefaultDescription = "Test Description"
        };

        var options = Options.Create(_siteMetadata);
        _service = new SitemapService(_mockRepository.Object, options);
    }

    [Fact]
    public async Task GenerateSitemapAsync_IncludesHomepage()
    {
        // Arrange
        var emptyPosts = new PagedResult<BlogPost>([], 0, 1, 10);
        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        Assert.Contains(sitemap.Urls, u => u.Location == "https://example.com");
        var homepage = sitemap.Urls.First(u => u.Location == "https://example.com");
        Assert.Equal("daily", homepage.ChangeFrequency);
        Assert.Equal("1.0", homepage.Priority);
    }

    [Fact]
    public async Task GenerateSitemapAsync_IncludesStaticPages()
    {
        // Arrange
        var emptyPosts = new PagedResult<BlogPost>([], 0, 1, 10);
        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        Assert.Contains(sitemap.Urls, u => u.Location == "https://example.com/posts");
        Assert.Contains(sitemap.Urls, u => u.Location == "https://example.com/archive");
        
        var postsPage = sitemap.Urls.First(u => u.Location == "https://example.com/posts");
        Assert.Equal("daily", postsPage.ChangeFrequency);
        Assert.Equal("0.9", postsPage.Priority);
        
        var archivePage = sitemap.Urls.First(u => u.Location == "https://example.com/archive");
        Assert.Equal("weekly", archivePage.ChangeFrequency);
        Assert.Equal("0.8", archivePage.Priority);
    }

    [Fact]
    public async Task GenerateSitemapAsync_IncludesBlogPosts()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateBlogPost("post-1", 2024, isFeatured: true),
            CreateBlogPost("post-2", 2024, isFeatured: false)
        };
        var pagedPosts = new PagedResult<BlogPost>(posts, posts.Count, 1, 10);

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        Assert.Contains(sitemap.Urls, u => u.Location == "https://example.com/post/2024/post-1");
        Assert.Contains(sitemap.Urls, u => u.Location == "https://example.com/post/2024/post-2");
    }

    [Fact]
    public async Task GenerateSitemapAsync_FeaturedPostsHaveHigherPriority()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateBlogPost("featured", 2024, isFeatured: true),
            CreateBlogPost("regular", 2024, isFeatured: false)
        };
        var pagedPosts = new PagedResult<BlogPost>(posts, posts.Count, 1, 10);

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        var featuredPost = sitemap.Urls.First(u => u.Location.Contains("/featured"));
        var regularPost = sitemap.Urls.First(u => u.Location.Contains("/regular"));
        
        Assert.Equal("0.9", featuredPost.Priority);
        Assert.Equal("0.7", regularPost.Priority);
    }

    [Fact]
    public async Task GenerateSitemapAsync_UsesUpdatedDateIfAvailable()
    {
        // Arrange
        var updatedDate = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var publishedDate = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        
        var post = new BlogPost
        {
            Id = "1",
            Slug = "updated-post",
            Title = "Updated Post",
            Content = "Content",
            Short = "Short",
            IsPublished = true,
            PublishedAt = publishedDate,
            UpdatedAt = updatedDate,
            AuthorName = "Author",
            CategoryNames = ["Tech"]
        };

        var pagedPosts = new PagedResult<BlogPost>([post], 1, 1, 10);
        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        var postUrl = sitemap.Urls.First(u => u.Location.Contains("/updated-post"));
        Assert.Equal("2024-06-15", postUrl.LastModified);
    }

    [Fact]
    public async Task GenerateSitemapAsync_IncludesUniqueCategories()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateBlogPost("post-1", 2024, categories: ["Tech", "Web"]),
            CreateBlogPost("post-2", 2024, categories: ["Tech"]),
            CreateBlogPost("post-3", 2024, categories: ["Design"])
        };
        var pagedPosts = new PagedResult<BlogPost>(posts, posts.Count, 1, 10);

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        var categoryUrls = sitemap.Urls.Where(u => u.Location.Contains("/category/")).ToList();
        Assert.Equal(3, categoryUrls.Count);
        Assert.Contains(categoryUrls, u => u.Location.Contains("/category/Tech"));
        Assert.Contains(categoryUrls, u => u.Location.Contains("/category/Web"));
        Assert.Contains(categoryUrls, u => u.Location.Contains("/category/Design"));
    }

    [Fact]
    public async Task GenerateSitemapAsync_CategoryUrlsHaveCorrectProperties()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateBlogPost("post-1", 2024, categories: ["Tech"])
        };
        var pagedPosts = new PagedResult<BlogPost>(posts, posts.Count, 1, 10);

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        var categoryUrl = sitemap.Urls.First(u => u.Location.Contains("/category/"));
        Assert.Equal("weekly", categoryUrl.ChangeFrequency);
        Assert.Equal("0.6", categoryUrl.Priority);
    }

    [Fact]
    public async Task GenerateSitemapAsync_EscapesCategoryNamesInUrls()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateBlogPost("post-1", 2024, categories: ["C# Programming"])
        };
        var pagedPosts = new PagedResult<BlogPost>(posts, posts.Count, 1, 10);

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        var categoryUrl = sitemap.Urls.First(u => u.Location.Contains("/category/"));
        Assert.Contains("C%23%20Programming", categoryUrl.Location);
    }

    [Fact]
    public async Task GenerateSitemapAsync_IncludesUniqueTags()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateBlogPost("post-1", 2024, tags: ["blazor", "dotnet"]),
            CreateBlogPost("post-2", 2024, tags: ["blazor", "web"]),
            CreateBlogPost("post-3", 2024, tags: ["csharp"])
        };
        var pagedPosts = new PagedResult<BlogPost>(posts, posts.Count, 1, 10);

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        var tagUrls = sitemap.Urls.Where(u => u.Location.Contains("/tag/")).ToList();
        Assert.Equal(4, tagUrls.Count);
        Assert.Contains(tagUrls, u => u.Location.Contains("/tag/blazor"));
        Assert.Contains(tagUrls, u => u.Location.Contains("/tag/dotnet"));
        Assert.Contains(tagUrls, u => u.Location.Contains("/tag/web"));
        Assert.Contains(tagUrls, u => u.Location.Contains("/tag/csharp"));
    }

    [Fact]
    public async Task GenerateSitemapAsync_TagUrlsHaveCorrectProperties()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateBlogPost("post-1", 2024, tags: ["test"])
        };
        var pagedPosts = new PagedResult<BlogPost>(posts, posts.Count, 1, 10);

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        var tagUrl = sitemap.Urls.First(u => u.Location.Contains("/tag/"));
        Assert.Equal("weekly", tagUrl.ChangeFrequency);
        Assert.Equal("0.6", tagUrl.Priority);
    }

    [Fact]
    public async Task GenerateSitemapAsync_IncludesArchiveDates()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateBlogPost("post-1", 2024, month: 1),
            CreateBlogPost("post-2", 2024, month: 1),
            CreateBlogPost("post-3", 2024, month: 3),
            CreateBlogPost("post-4", 2023, month: 12)
        };
        var pagedPosts = new PagedResult<BlogPost>(posts, posts.Count, 1, 10);

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        var archiveUrls = sitemap.Urls.Where(u => u.Location.Contains("/archive/20")).ToList();
        Assert.Equal(3, archiveUrls.Count);
        Assert.Contains(archiveUrls, u => u.Location == "https://example.com/archive/2024/01");
        Assert.Contains(archiveUrls, u => u.Location == "https://example.com/archive/2024/03");
        Assert.Contains(archiveUrls, u => u.Location == "https://example.com/archive/2023/12");
    }

    [Fact]
    public async Task GenerateSitemapAsync_ArchiveUrlsHaveCorrectProperties()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateBlogPost("post-1", 2024, month: 1)
        };
        var pagedPosts = new PagedResult<BlogPost>(posts, posts.Count, 1, 10);

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        var archiveUrl = sitemap.Urls.First(u => u.Location.Contains("/archive/"));
        Assert.Equal("monthly", archiveUrl.ChangeFrequency);
        Assert.Equal("0.5", archiveUrl.Priority);
    }

    [Fact]
    public async Task GenerateSitemapAsync_PassesCancellationToken()
    {
        // Arrange
        var emptyPosts = new PagedResult<BlogPost>([], 0, 1, 10);
        var cancellationToken = new CancellationToken();
        
        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            cancellationToken))
            .ReturnsAsync(emptyPosts);

        // Act
        await _service.GenerateSitemapAsync(cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GenerateSitemapAsync_IncludesAllPublishedPosts()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateBlogPost("post-1", 2024, 1),
            CreateBlogPost("post-2", 2024, 2),
            // Post with MinValue is still a valid published date, just very old
            new BlogPost
            {
                Id = "3",
                Slug = "old-post",
                Title = "Very Old Post",
                Content = "Content",
                Short = "Short",
                IsPublished = true,
                PublishedAt = DateTimeOffset.MinValue,
                AuthorName = "Author",
                CategoryNames = ["Tech"]
            }
        };
        var pagedPosts = new PagedResult<BlogPost>(posts, posts.Count, 1, 10);

        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
            It.IsAny<PagingParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedPosts);

        // Act
        var sitemap = await _service.GenerateSitemapAsync();

        // Assert
        // All published posts should be included, even those with MinValue date
        Assert.Contains(sitemap.Urls, u => u.Location.Contains("/old-post"));
        Assert.Contains(sitemap.Urls, u => u.Location.Contains("/post-1"));
        Assert.Contains(sitemap.Urls, u => u.Location.Contains("/post-2"));
    }

    #region Helper Methods

    private static BlogPost CreateBlogPost(
        string slug, 
        int year, 
        int month = 1, 
        bool isFeatured = false,
        string[]? categories = null,
        string[]? tags = null)
    {
        return new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            Slug = slug,
            Title = slug,
            Content = "Content",
            Short = "Short",
            IsPublished = true,
            IsFeatured = isFeatured,
            PublishedAt = new DateTimeOffset(year, month, 15, 12, 0, 0, TimeSpan.Zero),
            UpdatedAt = DateTimeOffset.MinValue,
            AuthorName = "Author",
            CategoryNames = categories?.ToList() ?? new List<string> { "Tech" },
            Tags = tags?.ToList() ?? new List<string>()
        };
    }

    #endregion
}
