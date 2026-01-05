# Vilog Configuration System

This document explains how to use the Vilog configuration system based on the IOptions pattern.

## Overview

The Vilog configuration system provides a structured, type-safe way to access application settings. All configuration is centralized in the `VilogConfiguration` class and individual configuration sections can be injected independently.

## Configuration Structure

### Root Configuration (`VilogConfiguration`)
The root configuration class that contains all application settings:
- `SiteMetadata` - Site-wide metadata for SEO and social sharing
- `CosmosDb` - CosmosDB database settings
- `ConnectionStrings` - Connection strings (though it's recommended to use `IConfiguration.GetConnectionString()` directly)

### Configuration Sections

#### `SiteMetadata`
Site-wide metadata used in SEO, structured data, and social sharing:
- `SiteName` - The name of the blog/website
- `BaseUrl` - The base URL (e.g., https://yourblog.com)
- `DefaultDescription` - Default meta description
- `Author` - Site author/owner name
- `TwitterHandle` - Twitter handle for Twitter Cards
- `FacebookAppId` - Facebook App ID for Open Graph
- `DefaultImageUrl` - Default image for social sharing
- `Locale` - Site locale (e.g., en_US)
- `Tagline` - Site tagline or subtitle
- `ContactEmail` - Contact email address
- `LogoUrl` - URL to the site logo

#### `CosmosDbSettings`
CosmosDB configuration:
- `DatabaseName` - The name of the CosmosDB database

## Setup

Configuration is registered in `Program.cs` using the extension method:

```csharp
builder.Services.AddVilogConfiguration(builder.Configuration);
```

This single line registers:
- The root `VilogConfiguration` object
- Individual configuration sections (`SiteMetadata`, `CosmosDbSettings`)

## Usage Patterns

### 1. Inject Specific Configuration Section (RECOMMENDED)

This is the preferred approach as it follows the dependency inversion principle:

```csharp
public class MyService
{
    private readonly SiteMetadata _siteMetadata;

    public MyService(IOptions<SiteMetadata> siteMetadata)
    {
        _siteMetadata = siteMetadata.Value;
    }

    public string GetSiteName()
    {
        return _siteMetadata.SiteName;
    }
}
```

### 2. Inject Full Configuration

Use this when you need access to multiple configuration sections:

```csharp
public class MyService
{
    private readonly VilogConfiguration _config;

    public MyService(IOptions<VilogConfiguration> config)
    {
        _config = config.Value;
    }

    public void UseConfig()
    {
        var siteName = _config.SiteMetadata.SiteName;
        var dbName = _config.CosmosDb.DatabaseName;
    }
}
```

### 3. Use in Blazor Components

```razor
@inject IOptions<SiteMetadata> SiteMetadataOptions

<h1>@SiteMetadata.SiteName</h1>
<p>@SiteMetadata.Tagline</p>

@code {
    private SiteMetadata SiteMetadata => SiteMetadataOptions.Value;
}
```

### 4. Scoped Services with IOptionsSnapshot

Use `IOptionsSnapshot<T>` for scoped services that need configuration reloaded per request:

```csharp
public class MyScopedService
{
    private readonly SiteMetadata _siteMetadata;

    public MyScopedService(IOptionsSnapshot<SiteMetadata> siteMetadata)
    {
        _siteMetadata = siteMetadata.Value;
    }
}
```

### 5. Singleton Services with IOptionsMonitor

Use `IOptionsMonitor<T>` for singleton services that need to react to configuration changes:

```csharp
public class MySingletonService
{
    private readonly IOptionsMonitor<SiteMetadata> _siteMetadataMonitor;

    public MySingletonService(IOptionsMonitor<SiteMetadata> siteMetadataMonitor)
    {
        _siteMetadataMonitor = siteMetadataMonitor;
        
        // Subscribe to changes
        _siteMetadataMonitor.OnChange(newConfig =>
        {
            Console.WriteLine($"Config changed: {newConfig.SiteName}");
        });
    }

    public string GetSiteName()
    {
        return _siteMetadataMonitor.CurrentValue.SiteName;
    }
}
```

## IOptions Variants

- **`IOptions<T>`** - Singleton, reads configuration once at startup. Best for most scenarios.
- **`IOptionsSnapshot<T>`** - Scoped, recomputes configuration per request. Use for scoped services.
- **`IOptionsMonitor<T>`** - Singleton, supports change notifications. Use for singleton services that need live updates.

## Best Practices

1. **Prefer specific section injection** - Inject only the configuration section you need (e.g., `IOptions<SiteMetadata>`) rather than the entire configuration.

2. **Use IOptions<T> by default** - Unless you have a specific need for configuration reloading, use `IOptions<T>`.

3. **Avoid accessing .Value in constructors** - Store the `IOptions<T>` and access `.Value` in methods to support lazy loading.

4. **Don't store connection strings in the root config** - For security, use `IConfiguration.GetConnectionString()` directly instead of binding connection strings to your configuration classes.

5. **Validate configuration** - Consider adding data annotations to your configuration classes and use options validation.

## Adding New Configuration Sections

To add a new configuration section:

1. Create a new configuration class in `Vilog/Shared/Configuration/`:

```csharp
public class MyNewSettings
{
    public string SomeSetting { get; set; } = string.Empty;
}
```

2. Add it to `VilogConfiguration`:

```csharp
public class VilogConfiguration
{
    // ...existing properties...
    public MyNewSettings MyNewSettings { get; set; } = new();
}
```

3. Register it in `ConfigurationExtensions.AddVilogConfiguration()`:

```csharp
services.Configure<MyNewSettings>(configuration.GetSection("MyNewSettings"));
```

4. Add the section to `appsettings.json`:

```json
{
  "MyNewSettings": {
    "SomeSetting": "value"
  }
}
```

## Example: Migrating Existing Code

**Before:**
```csharp
public class MyService
{
    private readonly IConfiguration _configuration;

    public MyService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetSiteName()
    {
        return _configuration["SiteMetadata:SiteName"] ?? "";
    }
}
```

**After:**
```csharp
public class MyService
{
    private readonly SiteMetadata _siteMetadata;

    public MyService(IOptions<SiteMetadata> siteMetadata)
    {
        _siteMetadata = siteMetadata.Value;
    }

    public string GetSiteName()
    {
        return _siteMetadata.SiteName;
    }
}
```

## See Also

- [Microsoft Docs: Options pattern in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- `ConfigurationUsageExamples.cs` - Code examples in the solution
