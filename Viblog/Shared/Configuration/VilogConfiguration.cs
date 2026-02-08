namespace Viblog.Shared.Configuration;

/// <summary>
/// Root configuration class that provides access to all application settings
/// </summary>
public class ViblogConfiguration
{
    /// <summary>
    /// Site metadata configuration for SEO, structured data, and social sharing
    /// </summary>
    public SiteMetadata SiteMetadata { get; set; } = new();

    /// <summary>
    /// CosmosDB database settings
    /// </summary>
    public CosmosDbSettings CosmosDb { get; set; } = new();

    /// <summary>
    /// Connection strings (accessed via IConfiguration.GetConnectionString in most cases)
    /// Note: For connection strings, it's recommended to use IConfiguration.GetConnectionString()
    /// directly rather than binding them to this configuration object for security reasons.
    /// </summary>
    public ConnectionStrings ConnectionStrings { get; set; } = new();
}

/// <summary>
/// Connection strings configuration
/// </summary>
public class ConnectionStrings
{
    /// <summary>
    /// CosmosDB connection string
    /// </summary>
    public string CosmosConnection { get; set; } = string.Empty;
}
