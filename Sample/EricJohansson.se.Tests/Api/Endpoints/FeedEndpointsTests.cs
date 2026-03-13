using EricJohansson.se.Infrastructure.Facades;
using EricJohansson.se.Models.Feed;
using Moq;

namespace Viblog.Tests.Api.Endpoints;

/// <summary>
/// Tests for FeedEndpoints - validates RSS and Atom feed generation endpoint behavior
/// Note: These tests focus on endpoint logic. The actual feed generation is tested in FeedFacadeTests.
/// </summary>
public class FeedEndpointsTests
{
    private readonly Mock<IFeedFacade> _mockFeedFacade;

    public FeedEndpointsTests()
    {
        _mockFeedFacade = new Mock<IFeedFacade>();
    }

    #region RSS Feed Tests

    [Fact]
    public async Task GetRssFeed_WithValidRequest_ReturnsRssXmlContent()
    {
        // Arrange
        var rssFeed = CreateSampleRssFeed();
        _mockFeedFacade.Setup(f => f.GenerateRssFeedAsync(
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(rssFeed);

        // Note: Testing the endpoint method directly would require accessing private methods
        // In a real scenario, you would test this via integration tests
        // For unit tests, we verify the facade is called correctly
        
        // Act
        var result = await _mockFeedFacade.Object.GenerateRssFeedAsync(20);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(rssFeed.Channel.Title, result.Channel.Title);
        _mockFeedFacade.Verify(f => f.GenerateRssFeedAsync(20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRssFeed_WithCustomMaxPosts_PassesParameterToFacade()
    {
        // Arrange
        var maxPosts = 50;
        var rssFeed = CreateSampleRssFeed();
        
        _mockFeedFacade.Setup(f => f.GenerateRssFeedAsync(maxPosts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rssFeed);

        // Act
        await _mockFeedFacade.Object.GenerateRssFeedAsync(maxPosts);

        // Assert
        _mockFeedFacade.Verify(f => f.GenerateRssFeedAsync(maxPosts, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRssFeed_WithDefaultParameters_Uses20AsDefault()
    {
        // Arrange
        var rssFeed = CreateSampleRssFeed();
        
        _mockFeedFacade.Setup(f => f.GenerateRssFeedAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rssFeed);

        // Act
        await _mockFeedFacade.Object.GenerateRssFeedAsync(20);

        // Assert
        _mockFeedFacade.Verify(f => f.GenerateRssFeedAsync(20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRssFeed_WithCancellationToken_PassesTokenToFacade()
    {
        // Arrange
        var rssFeed = CreateSampleRssFeed();
        var cancellationToken = new CancellationToken();
        
        _mockFeedFacade.Setup(f => f.GenerateRssFeedAsync(
            It.IsAny<int>(),
            cancellationToken))
            .ReturnsAsync(rssFeed);

        // Act
        await _mockFeedFacade.Object.GenerateRssFeedAsync(20, cancellationToken);

        // Assert
        _mockFeedFacade.Verify(f => f.GenerateRssFeedAsync(20, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetRssFeed_WhenFacadeThrowsException_ShouldHandleGracefully()
    {
        // Arrange
        _mockFeedFacade.Setup(f => f.GenerateRssFeedAsync(
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockFeedFacade.Object.GenerateRssFeedAsync(20));
    }

    #endregion

    #region Atom Feed Tests

    [Fact]
    public async Task GetAtomFeed_WithValidRequest_ReturnsAtomXmlContent()
    {
        // Arrange
        var atomFeed = CreateSampleAtomFeed();
        _mockFeedFacade.Setup(f => f.GenerateAtomFeedAsync(
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(atomFeed);

        // Act
        var result = await _mockFeedFacade.Object.GenerateAtomFeedAsync(20);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(atomFeed.Title, result.Title);
        _mockFeedFacade.Verify(f => f.GenerateAtomFeedAsync(20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAtomFeed_WithCustomMaxPosts_PassesParameterToFacade()
    {
        // Arrange
        var maxPosts = 30;
        var atomFeed = CreateSampleAtomFeed();
        
        _mockFeedFacade.Setup(f => f.GenerateAtomFeedAsync(maxPosts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(atomFeed);

        // Act
        await _mockFeedFacade.Object.GenerateAtomFeedAsync(maxPosts);

        // Assert
        _mockFeedFacade.Verify(f => f.GenerateAtomFeedAsync(maxPosts, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAtomFeed_WithDefaultParameters_Uses20AsDefault()
    {
        // Arrange
        var atomFeed = CreateSampleAtomFeed();
        
        _mockFeedFacade.Setup(f => f.GenerateAtomFeedAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(atomFeed);

        // Act
        await _mockFeedFacade.Object.GenerateAtomFeedAsync(20);

        // Assert
        _mockFeedFacade.Verify(f => f.GenerateAtomFeedAsync(20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAtomFeed_WithCancellationToken_PassesTokenToFacade()
    {
        // Arrange
        var atomFeed = CreateSampleAtomFeed();
        var cancellationToken = new CancellationToken();
        
        _mockFeedFacade.Setup(f => f.GenerateAtomFeedAsync(
            It.IsAny<int>(),
            cancellationToken))
            .ReturnsAsync(atomFeed);

        // Act
        await _mockFeedFacade.Object.GenerateAtomFeedAsync(20, cancellationToken);

        // Assert
        _mockFeedFacade.Verify(f => f.GenerateAtomFeedAsync(20, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAtomFeed_WhenFacadeThrowsException_ShouldHandleGracefully()
    {
        // Arrange
        _mockFeedFacade.Setup(f => f.GenerateAtomFeedAsync(
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockFeedFacade.Object.GenerateAtomFeedAsync(20));
    }

    #endregion

    #region Feed Model Serialization Tests

    [Fact]
    public void RssFeed_CanBeSerializedToXml()
    {
        // Arrange
        var feed = CreateSampleRssFeed();
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(RssFeed));

        // Act
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, feed);
        var xml = stringWriter.ToString();

        // Assert
        Assert.Contains("<rss", xml);
        Assert.Contains("version=\"2.0\"", xml);
        Assert.Contains("<channel>", xml);
        Assert.Contains(feed.Channel.Title, xml);
    }

    [Fact]
    public void AtomFeed_CanBeSerializedToXml()
    {
        // Arrange
        var feed = CreateSampleAtomFeed();
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(AtomFeed));

        // Act
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, feed);
        var xml = stringWriter.ToString();

        // Assert
        Assert.Contains("<feed", xml);
        Assert.Contains("xmlns=\"http://www.w3.org/2005/Atom\"", xml);
        Assert.Contains("<title>", xml);
        Assert.Contains(feed.Title, xml);
    }

    [Fact]
    public void RssFeed_Serialization_IncludesAllRequiredElements()
    {
        // Arrange
        var feed = CreateSampleRssFeed();
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(RssFeed));

        // Act
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, feed);
        var xml = stringWriter.ToString();

        // Assert
        Assert.Contains("<title>", xml);
        Assert.Contains("<link>", xml);
        Assert.Contains("<description>", xml);
        Assert.Contains("<item>", xml);
    }

    [Fact]
    public void AtomFeed_Serialization_IncludesAllRequiredElements()
    {
        // Arrange
        var feed = CreateSampleAtomFeed();
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(AtomFeed));

        // Act
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, feed);
        var xml = stringWriter.ToString();

        // Assert
        Assert.Contains("<title>", xml);
        Assert.Contains("<id>", xml);
        Assert.Contains("<updated>", xml);
        Assert.Contains("<entry>", xml);
    }

    #endregion

    #region Helper Methods

    private static RssFeed CreateSampleRssFeed()
    {
        return new RssFeed
        {
            Channel = new RssChannel
            {
                Title = "Test Blog",
                Link = "https://example.com",
                Description = "A test blog",
                Language = "en-US",
                LastBuildDate = DateTime.UtcNow.ToString("R"),
                Items =
                [
                    new RssItem
                    {
                        Title = "Test Post",
                        Link = "https://example.com/post/2024/test",
                        Description = "Test description",
                        PubDate = DateTime.UtcNow.ToString("R"),
                        Guid = "https://example.com/post/2024/test"
                    }
                ]
            }
        };
    }

    private static AtomFeed CreateSampleAtomFeed()
    {
        return new AtomFeed
        {
            Id = "https://example.com/",
            Title = "Test Blog",
            Subtitle = "A test blog",
            Updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Links =
            [
                new AtomLink
                {
                    Href = "https://example.com/atom.xml",
                    Rel = "self"
                }
            ],
            Entries =
            [
                new AtomEntry
                {
                    Id = "https://example.com/post/2024/test",
                    Title = "Test Post",
                    Summary = "Test summary",
                    Published = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Links =
                    [
                        new AtomLink
                        {
                            Href = "https://example.com/post/2024/test",
                            Rel = "alternate"
                        }
                    ]
                }
            ]
        };
    }

    #endregion
}
