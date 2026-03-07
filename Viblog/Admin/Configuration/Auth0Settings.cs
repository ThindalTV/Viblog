namespace Viblog.Admin.Configuration;

/// <summary>
/// Auth0 configuration settings
/// These values are loaded from appsettings.json and User Secrets
/// </summary>
public class Auth0Settings
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Auth0";

    /// <summary>
    /// Auth0 tenant domain (e.g., "viblog-dev.auth0.com")
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Auth0 application client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Auth0 application client secret
    /// NEVER commit this to source control - store in User Secrets
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Auth0 audience (usually the Management API URL)
    /// Format: https://{domain}/api/v2/
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Callback path after successful Auth0 login
    /// Default: /viblog/auth/callback
    /// </summary>
    public string CallbackPath { get; set; } = "/viblog/auth/callback";

    /// <summary>
    /// Where to redirect after logout
    /// Default: /viblog/login
    /// </summary>
    public string LogoutRedirectUri { get; set; } = "/viblog/login";

    /// <summary>
    /// Auth0 Management API connection string
    /// Format: https://{domain}
    /// </summary>
    public string ManagementApiUrl => $"https://{Domain}";

    /// <summary>
    /// Validate that all required settings are present
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Domain)
            && !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret);
    }
}
