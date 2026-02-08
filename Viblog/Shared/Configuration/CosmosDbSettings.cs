namespace Viblog.Shared.Configuration;

/// <summary>
/// Configuration settings for CosmosDB
/// </summary>
public class CosmosDbSettings
{
    /// <summary>
    /// The name of the CosmosDB database
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;
}
