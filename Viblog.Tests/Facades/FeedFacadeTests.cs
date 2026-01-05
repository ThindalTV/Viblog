using Microsoft.Extensions.Options;
using Moq;
using Viblog.Frontend.Facades;
using Viblog.Shared.Configuration;
using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;
using Viblog.Shared.Data.Repositories;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for FeedFacade
/// </summary>
public class FeedFacadeTests
{
    private readonly Mock<IBlogPostRepository> _mockRepository;
    private readonly FeedFacade _facade;

    public FeedFacadeTests()
    {
        _mockRepository = new Mock<IBlogPostRepository>();
        var mockOptions = new Mock<IOptions<SiteMetadata>>();
        mockOptions.Setup(o => o.Value).Returns(new SiteMetadata
        {
            SiteName = "Test Blog",
            BaseUrl = "https://testblog.com",
            DefaultDescription = "Test blog description",
            Author = "Test Author"
        });
        
        _facade = new FeedFacade(_mockRepository.Object, mockOptions.Object);
    }

    [Fact]
    public async Task GenerateRssFeedAsync_ReturnsValidFeed()
    {
        // Arrange
        var posts = CreateTestPosts(3);
        SetupRepositoryMock(posts, 20);

        // Act
        var result = await _facade.GenerateRssFeedAsync(maxPosts: 20);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Channel);
        Assert.Equal("2.0", result.Version);
        Assert.Equal("Test Blog", result.Channel.Title);
        Assert.Equal("https://testblog.com", result.Channel.Link);
        Assert.Equal("Test blog description", result.Channel.Description);
        Assert.Equal(3, result.Channel.Items.Count);
    }

    [Fact]
    public async Task GenerateRssFeedAsync_ContainsPostData()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateTestPost("Test Post Title", "test-post-slug", "Test content")
        };
        SetupRepositoryMock(posts, 20);

        // Act
        var result = await _facade.GenerateRssFeedAsync();

        // Assert
        Assert.Single(result.Channel.Items);
        var item = result.Channel.Items[0];
        Assert.Equal("Test Post Title", item.Title);
        Assert.Contains("test-post-slug", item.Link);
        Assert.Equal("Test content", item.Description);
    }

    [Fact]
    public async Task GenerateRssFeedAsync_WithMaxPosts_LimitsResults()
    {
        // Arrange
        var posts = CreateTestPosts(5);
        SetupRepositoryMock(posts.Take(3).ToList(), 3);

        // Act
        var result = await _facade.GenerateRssFeedAsync(maxPosts: 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Channel.Items.Count);
    }

    [Fact]
    public async Task GenerateRssFeedAsync_WithNoPosts_ReturnsEmptyChannel()
    {
        // Arrange
        SetupRepositoryMock(new List<BlogPost>(), 20);

        // Act
        var result = await _facade.GenerateRssFeedAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Channel);
        Assert.Empty(result.Channel.Items);
    }

    [Fact]
    public async Task GenerateAtomFeedAsync_ReturnsValidFeed()
    {
        // Arrange
        var posts = CreateTestPosts(3);
        SetupRepositoryMock(posts, 20);

        // Act
        var result = await _facade.GenerateAtomFeedAsync(maxPosts: 20);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Blog", result.Title);
        Assert.Equal("Test blog description", result.Subtitle);
        Assert.Equal("https://testblog.com", result.Id);
        Assert.Equal(3, result.Entries.Count);
        Assert.NotEmpty(result.Links);
    }

    [Fact]
    public async Task GenerateAtomFeedAsync_ContainsPostData()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateTestPost("Atom Test Title", "atom-test-slug", "Atom test content")
        };
        SetupRepositoryMock(posts, 20);

        // Act
        var result = await _facade.GenerateAtomFeedAsync();

        // Assert
        Assert.Single(result.Entries);
        var entry = result.Entries[0];
        Assert.Equal("Atom Test Title", entry.Title);
        Assert.Contains("atom-test-slug", entry.Id);
        Assert.Equal("Atom test content", entry.Summary);
        Assert.NotNull(entry.Author);
    }

    [Fact]
    public async Task GenerateAtomFeedAsync_WithMaxPosts_LimitsResults()
    {
        // Arrange
        var posts = CreateTestPosts(10);
        SetupRepositoryMock(posts.Take(5).ToList(), 5);

        // Act
        var result = await _facade.GenerateAtomFeedAsync(maxPosts: 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Entries.Count);
    }

    [Fact]
    public async Task GenerateAtomFeedAsync_WithNoPosts_ReturnsEmptyFeed()
    {
        // Arrange
        SetupRepositoryMock(new List<BlogPost>(), 20);

        // Act
        var result = await _facade.GenerateAtomFeedAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task GenerateRssFeedAsync_IncludesPublicationDates()
    {
        // Arrange
        var publishedAt = new DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.Zero);
        var posts = new List<BlogPost>
        {
            CreateTestPost("Date Test", "date-test", "Content", publishedAt)
        };
        SetupRepositoryMock(posts, 20);

        // Act
        var result = await _facade.GenerateRssFeedAsync();

        // Assert
        Assert.NotNull(result.Channel.Items[0].PubDate);
        Assert.NotEmpty(result.Channel.Items[0].PubDate);
    }

    [Fact]
    public async Task GenerateAtomFeedAsync_IncludesTimestamps()
    {
        // Arrange
        var publishedAt = new DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.Zero);
        var posts = new List<BlogPost>
        {
            CreateTestPost("Timestamp Test", "timestamp-test", "Content", publishedAt)
        };
        SetupRepositoryMock(posts, 20);

        // Act
        var result = await _facade.GenerateAtomFeedAsync();

        // Assert
        Assert.NotNull(result.Entries[0].Published);
        Assert.NotNull(result.Entries[0].Updated);
        Assert.NotEmpty(result.Entries[0].Published);
        Assert.NotEmpty(result.Entries[0].Updated);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task GenerateRssFeedAsync_WithVariousMaxPosts_WorksCorrectly(int maxPosts)
    {
        // Arrange
        var posts = CreateTestPosts(25);
        SetupRepositoryMock(posts.Take(maxPosts).ToList(), maxPosts);

        // Act
        var result = await _facade.GenerateRssFeedAsync(maxPosts);

        // Assert
        Assert.Equal(maxPosts, result.Channel.Items.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task GenerateAtomFeedAsync_WithVariousMaxPosts_WorksCorrectly(int maxPosts)
    {
        // Arrange
        var posts = CreateTestPosts(25);
        SetupRepositoryMock(posts.Take(maxPosts).ToList(), maxPosts);

        // Act
        var result = await _facade.GenerateAtomFeedAsync(maxPosts);

        // Assert
        Assert.Equal(maxPosts, result.Entries.Count);
    }

    [Fact]
    public async Task GenerateRssFeedAsync_IncludesCategories()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateTestPost("Test", "test", "Content")
        };
        SetupRepositoryMock(posts, 20);

        // Act
        var result = await _facade.GenerateRssFeedAsync();

        // Assert
        Assert.NotEmpty(result.Channel.Items[0].Categories);
        Assert.Contains("Test Category", result.Channel.Items[0].Categories);
    }

    [Fact]
    public async Task GenerateAtomFeedAsync_IncludesCategories()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            CreateTestPost("Test", "test", "Content")
        };
        SetupRepositoryMock(posts, 20);

        // Act
        var result = await _facade.GenerateAtomFeedAsync();

        // Assert
        Assert.NotEmpty(result.Entries[0].Categories);
        Assert.Equal("Test Category", result.Entries[0].Categories[0].Term);
    }

    private void SetupRepositoryMock(List<BlogPost> posts, int maxPosts)
    {
        _mockRepository.Setup(r => r.GetPublishedPostsAsync(
                It.Is<PagingParameters>(p => p.PageSize == maxPosts),
                default))
            .ReturnsAsync(new PagedResult<BlogPost>
            {
                Items = posts,
                TotalCount = posts.Count
            });
    }

    private static List<BlogPost> CreateTestPosts(int count)
    {
        var posts = new List<BlogPost>();
        for (int i = 1; i <= count; i++)
        {
            posts.Add(CreateTestPost($"Post {i}", $"post-{i}", $"Content for post {i}"));
        }
        return posts;
    }

    private static BlogPost CreateTestPost(
        string title,
        string slug,
        string content,
        DateTimeOffset? publishedAt = null)
    {
        return new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = "test",
            Title = title,
            Slug = slug,
            Content = content,
            Short = content.Length > 100 ? content[..100] : content,
            IsPublished = true,
            PublishedAt = publishedAt ?? DateTimeOffset.UtcNow,
            AuthorName = "Test Author",
            Tags = new List<string> { "test", "sample" },
            CategoryNames = new List<string> { "Test Category" },
            CreatedAt = publishedAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = publishedAt ?? DateTimeOffset.UtcNow
        };
    }
}
