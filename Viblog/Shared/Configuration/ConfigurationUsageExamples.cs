using Microsoft.Extensions.Options;
using Viblog.Shared.Configuration;

namespace Viblog.Shared.Examples;

/// <summary>
/// Examples demonstrating how to use the Viblog configuration with the IOptions pattern
/// </summary>
public class ConfigurationUsageExamples
{
    // Example 1: Inject the entire configuration
    public class ServiceUsingFullConfig
    {
        private readonly ViblogConfiguration _config;

        public ServiceUsingFullConfig(IOptions<ViblogConfiguration> config)
        {
            _config = config.Value;
        }

        public void UseConfiguration()
        {
            // Access any configuration section
            var siteName = _config.SiteMetadata.SiteName;
            var databaseName = _config.CosmosDb.DatabaseName;
            var connectionString = _config.ConnectionStrings.CosmosConnection;
        }
    }

    // Example 2: Inject only the specific configuration section you need (RECOMMENDED)
    public class ServiceUsingSiteMetadata
    {
        private readonly SiteMetadata _siteMetadata;

        public ServiceUsingSiteMetadata(IOptions<SiteMetadata> siteMetadata)
        {
            _siteMetadata = siteMetadata.Value;
        }

        public void UseSiteMetadata()
        {
            var siteName = _siteMetadata.SiteName;
            var baseUrl = _siteMetadata.BaseUrl;
            var author = _siteMetadata.Author;
        }
    }

    // Example 3: Use IOptionsSnapshot for scoped services that need updated config
    public class ScopedServiceWithConfig
    {
        private readonly SiteMetadata _siteMetadata;

        public ScopedServiceWithConfig(IOptionsSnapshot<SiteMetadata> siteMetadata)
        {
            // IOptionsSnapshot allows config to be reloaded per request
            _siteMetadata = siteMetadata.Value;
        }

        public string GetSiteName() => _siteMetadata.SiteName;
    }

    // Example 4: Use IOptionsMonitor for singleton services that need to react to config changes
    public class SingletonServiceWithConfig
    {
        private readonly IOptionsMonitor<SiteMetadata> _siteMetadataMonitor;

        public SingletonServiceWithConfig(IOptionsMonitor<SiteMetadata> siteMetadataMonitor)
        {
            _siteMetadataMonitor = siteMetadataMonitor;

            // You can even subscribe to changes
            _siteMetadataMonitor.OnChange(newConfig =>
            {
                // React to configuration changes
                Console.WriteLine($"Site name changed to: {newConfig.SiteName}");
            });
        }

        public string GetCurrentSiteName()
        {
            // Always gets the latest configuration
            return _siteMetadataMonitor.CurrentValue.SiteName;
        }
    }

    // Example 5: Use in Blazor components
    // Note: In Blazor components, you can inject directly
    /*
    @inject IOptions<SiteMetadata> SiteMetadataOptions

    @code {
        private SiteMetadata SiteMetadata => SiteMetadataOptions.Value;

        protected override void OnInitialized()
        {
            var siteName = SiteMetadata.SiteName;
            var baseUrl = SiteMetadata.BaseUrl;
        }
    }
    */

    // Example 6: Best practice - Use strongly-typed config in facades
    public class ExampleFacade
    {
        private readonly SiteMetadata _siteMetadata;
        private readonly CosmosDbSettings _cosmosDbSettings;

        public ExampleFacade(
            IOptions<SiteMetadata> siteMetadata,
            IOptions<CosmosDbSettings> cosmosDbSettings)
        {
            _siteMetadata = siteMetadata.Value;
            _cosmosDbSettings = cosmosDbSettings.Value;
        }

        public string BuildAbsoluteUrl(string relativePath)
        {
            return $"{_siteMetadata.BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        }

        public string GetDatabaseName()
        {
            return _cosmosDbSettings.DatabaseName;
        }
    }
}
