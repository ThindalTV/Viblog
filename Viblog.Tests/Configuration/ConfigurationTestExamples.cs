using Microsoft.Extensions.Options;
using Viblog.Shared.Configuration;

namespace Viblog.Tests.Configuration;

/// <summary>
/// Tests demonstrating how to test code that uses the IOptions pattern
/// </summary>
public class ConfigurationTestExamples
{
    /// <summary>
    /// Example: Testing a service that uses SiteMetadata configuration
    /// </summary>
    [Fact]
    public void ServiceUsesSiteMetadata_BuildsCorrectUrl()
    {
        // Arrange
        var siteMetadata = new SiteMetadata
        {
            SiteName = "Test Blog",
            BaseUrl = "https://testblog.com",
        };

        var options = Options.Create(siteMetadata);
        var service = new ExampleService(options);

        // Act
        var url = service.BuildAbsoluteUrl("/posts/test");

        // Assert
        Assert.Equal("https://testblog.com/posts/test", url);
    }

    /// <summary>
    /// Example: Testing with multiple configuration values
    /// </summary>
    [Theory]
    [InlineData("https://blog.com", "/post", "https://blog.com/post")]
    [InlineData("https://blog.com/", "/post", "https://blog.com/post")]
    [InlineData("https://blog.com", "post", "https://blog.com/post")]
    [InlineData("https://blog.com/", "post", "https://blog.com/post")]
    public void BuildAbsoluteUrl_HandlesVariousFormats(string baseUrl, string relativePath, string expected)
    {
        // Arrange
        var siteMetadata = new SiteMetadata
        {
            BaseUrl = baseUrl
        };

        var options = Options.Create(siteMetadata);
        var service = new ExampleService(options);

        // Act
        var result = service.BuildAbsoluteUrl(relativePath);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Example: Testing with the full ViblogConfiguration
    /// </summary>
    [Fact]
    public void ServiceUsesFullConfig_AccessesMultipleSections()
    {
        // Arrange
        var config = new ViblogConfiguration
        {
            SiteMetadata = new SiteMetadata
            {
                SiteName = "Test Blog",
                BaseUrl = "https://testblog.com"
            },
            CosmosDb = new CosmosDbSettings
            {
                DatabaseName = "TestDb"
            }
        };

        var options = Options.Create(config);
        var service = new ServiceUsingFullConfig(options);

        // Act
        var info = service.GetSystemInfo();

        // Assert
        Assert.Contains("Test Blog", info);
        Assert.Contains("TestDb", info);
    }

    // Example service for testing
    private class ExampleService
    {
        private readonly SiteMetadata _siteMetadata;

        public ExampleService(IOptions<SiteMetadata> siteMetadata)
        {
            _siteMetadata = siteMetadata.Value;
        }

        public string BuildAbsoluteUrl(string relativePath)
        {
            return $"{_siteMetadata.BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        }
    }

    // Example service using full config
    private class ServiceUsingFullConfig
    {
        private readonly ViblogConfiguration _config;

        public ServiceUsingFullConfig(IOptions<ViblogConfiguration> config)
        {
            _config = config.Value;
        }

        public string GetSystemInfo()
        {
            return $"Site: {_config.SiteMetadata.SiteName}, Database: {_config.CosmosDb.DatabaseName}";
        }
    }
}
