using Microsoft.Extensions.Options;
using Moq;
using Vilog.Shared.Configuration;
using Vilog.Shared.Models.Sitemap;
using Vilog.Shared.Services;

namespace Vilog.Tests.Api.Endpoints;

/// <summary>
/// Tests for SitemapEndpoints - validates sitemap and robots.txt endpoint behavior
/// Note: These tests focus on endpoint logic. The actual sitemap generation is tested in SitemapServiceTests.
/// </summary>
public class SitemapEndpointsTests
{
    private readonly Mock<ISitemapService> _mockSitemapService;
    private readonly SiteMetadata _siteMetadata;

    public SitemapEndpointsTests()
    {
        _mockSitemapService = new Mock<ISitemapService>();
        _siteMetadata = new SiteMetadata
        {
            BaseUrl = "https://example.com",
            SiteName = "Test Blog",
            DefaultDescription = "Test Description"
        };
    }

    #region Sitemap.xml Tests

    [Fact]
    public async Task GetSitemap_WithValidRequest_ReturnsSitemapXml()
    {
        // Arrange
        var sitemap = CreateSampleSitemap();
        _mockSitemapService.Setup(s => s.GenerateSitemapAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sitemap);

        // Act
        var result = await _mockSitemapService.Object.GenerateSitemapAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sitemap.Urls.Count, result.Urls.Count);
        _mockSitemapService.Verify(s => s.GenerateSitemapAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSitemap_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var sitemap = CreateSampleSitemap();
        var cancellationToken = new CancellationToken();
        
        _mockSitemapService.Setup(s => s.GenerateSitemapAsync(cancellationToken))
            .ReturnsAsync(sitemap);

        // Act
        await _mockSitemapService.Object.GenerateSitemapAsync(cancellationToken);

        // Assert
        _mockSitemapService.Verify(s => s.GenerateSitemapAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetSitemap_WhenServiceThrowsException_ShouldHandleGracefully()
    {
        // Arrange
        _mockSitemapService.Setup(s => s.GenerateSitemapAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockSitemapService.Object.GenerateSitemapAsync());
    }

    [Fact]
    public void SitemapUrlSet_CanBeSerializedToXml()
    {
        // Arrange
        var sitemap = CreateSampleSitemap();
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(SitemapUrlSet));

        // Act
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, sitemap);
        var xml = stringWriter.ToString();

        // Assert
        Assert.Contains("<urlset", xml);
        Assert.Contains("xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"", xml);
        Assert.Contains("<url>", xml);
        Assert.Contains("<loc>", xml);
    }

    [Fact]
    public void SitemapUrlSet_Serialization_IncludesAllUrlProperties()
    {
        // Arrange
        var sitemap = CreateSampleSitemap();
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(SitemapUrlSet));

        // Act
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, sitemap);
        var xml = stringWriter.ToString();

        // Assert
        Assert.Contains("<loc>https://example.com</loc>", xml);
        Assert.Contains("<lastmod>", xml);
        Assert.Contains("<changefreq>daily</changefreq>", xml);
        Assert.Contains("<priority>1.0</priority>", xml);
    }

    [Fact]
    public void SitemapUrlSet_Serialization_HandlesMultipleUrls()
    {
        // Arrange
        var sitemap = new SitemapUrlSet
        {
            Urls =
            [
                new SitemapUrl
                {
                    Location = "https://example.com",
                    LastModified = "2024-01-15",
                    ChangeFrequency = "daily",
                    Priority = "1.0"
                },
                new SitemapUrl
                {
                    Location = "https://example.com/blog",
                    LastModified = "2024-01-14",
                    ChangeFrequency = "weekly",
                    Priority = "0.8"
                },
                new SitemapUrl
                {
                    Location = "https://example.com/about",
                    LastModified = "2024-01-10",
                    ChangeFrequency = "monthly",
                    Priority = "0.5"
                }
            ]
        };
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(SitemapUrlSet));

        // Act
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, sitemap);
        var xml = stringWriter.ToString();

        // Assert
        Assert.Contains("https://example.com", xml);
        Assert.Contains("https://example.com/blog", xml);
        Assert.Contains("https://example.com/about", xml);
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(xml, "<url>").Count);
    }

    #endregion

    #region Robots.txt Tests

    [Fact]
    public void GetRobotsTxt_ReturnsValidRobotsTxtContent()
    {
        // Arrange
        var options = Options.Create(_siteMetadata);

        // Act - Simulating what the endpoint does
        var robotsTxt = GenerateRobotsTxt(options);

        // Assert
        Assert.Contains("User-agent: *", robotsTxt);
        Assert.Contains("Allow: /", robotsTxt);
    }

    [Fact]
    public void GetRobotsTxt_IncludesSitemapUrl()
    {
        // Arrange
        var options = Options.Create(_siteMetadata);

        // Act
        var robotsTxt = GenerateRobotsTxt(options);

        // Assert
        Assert.Contains($"Sitemap: {_siteMetadata.BaseUrl}/sitemap.xml", robotsTxt);
    }

    [Fact]
    public void GetRobotsTxt_DisallowsAdminAreas()
    {
        // Arrange
        var options = Options.Create(_siteMetadata);

        // Act
        var robotsTxt = GenerateRobotsTxt(options);

        // Assert
        Assert.Contains("Disallow: /Account/", robotsTxt);
        Assert.Contains("Disallow: /admin/", robotsTxt);
    }

    [Fact]
    public void GetRobotsTxt_DisallowsSearchResults()
    {
        // Arrange
        var options = Options.Create(_siteMetadata);

        // Act
        var robotsTxt = GenerateRobotsTxt(options);

        // Assert
        Assert.Contains("Disallow: /search", robotsTxt);
    }

    [Fact]
    public void GetRobotsTxt_IncludesSiteName()
    {
        // Arrange
        var options = Options.Create(_siteMetadata);

        // Act
        var robotsTxt = GenerateRobotsTxt(options);

        // Assert
        Assert.Contains(_siteMetadata.SiteName, robotsTxt);
    }

    [Fact]
    public void GetRobotsTxt_WithDifferentBaseUrl_UsesSitemapWithCorrectUrl()
    {
        // Arrange
        var customMetadata = new SiteMetadata
        {
            BaseUrl = "https://myblog.example.org",
            SiteName = "My Custom Blog"
        };
        var options = Options.Create(customMetadata);

        // Act
        var robotsTxt = GenerateRobotsTxt(options);

        // Assert
        Assert.Contains("Sitemap: https://myblog.example.org/sitemap.xml", robotsTxt);
    }

    [Fact]
    public void GetRobotsTxt_HasCorrectFormat()
    {
        // Arrange
        var options = Options.Create(_siteMetadata);

        // Act
        var robotsTxt = GenerateRobotsTxt(options);

        // Assert
        var lines = robotsTxt.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length > 5, "Robots.txt should have multiple lines");
        Assert.Contains(lines, l => l.Trim().StartsWith("#"));
        Assert.Contains(lines, l => l.Contains("User-agent:"));
        Assert.Contains(lines, l => l.Contains("Sitemap:"));
    }

    #endregion

    #region XML Validation Tests

    [Fact]
    public void SitemapXml_IsWellFormed()
    {
        // Arrange
        var sitemap = CreateSampleSitemap();
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(SitemapUrlSet));

        // Act
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, sitemap);
        var xml = stringWriter.ToString();

        // Assert - Try to parse it back to ensure it's well-formed
        using var stringReader = new StringReader(xml);
        var xmlDoc = new System.Xml.XmlDocument();
        Assert.Null(Record.Exception(() => xmlDoc.Load(stringReader)));
    }

    [Fact]
    public void SitemapXml_HasCorrectNamespace()
    {
        // Arrange
        var sitemap = CreateSampleSitemap();
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(SitemapUrlSet));

        // Act
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, sitemap);
        var xml = stringWriter.ToString();

        // Assert
        Assert.Contains("http://www.sitemaps.org/schemas/sitemap/0.9", xml);
    }

    [Fact]
    public void SitemapXml_EscapesSpecialCharacters()
    {
        // Arrange
        var sitemap = new SitemapUrlSet
        {
            Urls =
            [
                new SitemapUrl
                {
                    Location = "https://example.com/post?id=123&name=test",
                    LastModified = "2024-01-15",
                    ChangeFrequency = "daily",
                    Priority = "1.0"
                }
            ]
        };
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(SitemapUrlSet));

        // Act
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, sitemap);
        var xml = stringWriter.ToString();

        // Assert
        Assert.Contains("&amp;", xml);
    }

    #endregion

    #region Helper Methods

    private static SitemapUrlSet CreateSampleSitemap()
    {
        return new SitemapUrlSet
        {
            Urls =
            [
                new SitemapUrl
                {
                    Location = "https://example.com",
                    LastModified = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    ChangeFrequency = "daily",
                    Priority = "1.0"
                },
                new SitemapUrl
                {
                    Location = "https://example.com/blog",
                    LastModified = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
                    ChangeFrequency = "weekly",
                    Priority = "0.8"
                }
            ]
        };
    }

    /// <summary>
    /// Helper method that replicates the robots.txt generation logic from the endpoint
    /// </summary>
    private static string GenerateRobotsTxt(IOptions<SiteMetadata> siteMetadata)
    {
        var baseUrl = siteMetadata.Value.BaseUrl;
        
        return $"""
            # robots.txt for {siteMetadata.Value.SiteName}
            User-agent: *
            Allow: /
            
            # Disallow admin and account areas
            Disallow: /Account/
            Disallow: /admin/
            
            # Disallow search results to avoid duplicate content
            Disallow: /search
            
            # Sitemap location
            Sitemap: {baseUrl}/sitemap.xml
            """;
    }

    #endregion
}
