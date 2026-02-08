# Migration Guide: Moving to Structured Configuration

This guide helps you migrate existing code to use the new structured configuration system.

## Quick Reference

### Before (Old Way)
```csharp
// Injecting IConfiguration everywhere
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

### After (New Way)
```csharp
// Injecting specific configuration section
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

## Step-by-Step Migration

### 1. Update Your Class Constructor

**Before:**
```csharp
private readonly IConfiguration _configuration;

public MyClass(IConfiguration configuration)
{
    _configuration = configuration;
}
```

**After:**
```csharp
private readonly SiteMetadata _siteMetadata;

public MyClass(IOptions<SiteMetadata> siteMetadata)
{
    _siteMetadata = siteMetadata.Value;
}
```

### 2. Update Configuration Access

**Before:**
```csharp
var siteName = _configuration["SiteMetadata:SiteName"];
var baseUrl = _configuration["SiteMetadata:BaseUrl"];
var author = _configuration.GetValue<string>("SiteMetadata:Author");
```

**After:**
```csharp
var siteName = _siteMetadata.SiteName;
var baseUrl = _siteMetadata.BaseUrl;
var author = _siteMetadata.Author;
```

### 3. Update Blazor Components

**Before:**
```razor
@inject IConfiguration Configuration

<h1>@Configuration["SiteMetadata:SiteName"]</h1>

@code {
    private string GetBaseUrl()
    {
        return Configuration["SiteMetadata:BaseUrl"] ?? "";
    }
}
```

**After:**
```razor
@inject IOptions<SiteMetadata> SiteMetadataOptions

<h1>@SiteMetadata.SiteName</h1>

@code {
    private SiteMetadata SiteMetadata => SiteMetadataOptions.Value;

    private string GetBaseUrl()
    {
        return SiteMetadata.BaseUrl;
    }
}
```

### 4. Update Tests

**Before:**
```csharp
[Fact]
public void TestMethod()
{
    // Setting up IConfiguration is complex
    var inMemorySettings = new Dictionary<string, string>
    {
        {"SiteMetadata:SiteName", "Test Blog"},
        {"SiteMetadata:BaseUrl", "https://test.com"}
    };

    IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(inMemorySettings!)
        .Build();

    var service = new MyService(configuration);
    
    // Test...
}
```

**After:**
```csharp
[Fact]
public void TestMethod()
{
    // Much simpler with Options.Create
    var siteMetadata = new SiteMetadata
    {
        SiteName = "Test Blog",
        BaseUrl = "https://test.com"
    };

    var options = Options.Create(siteMetadata);
    var service = new MyService(options);
    
    // Test...
}
```

## Common Patterns

### Pattern 1: Connection Strings

**Before:**
```csharp
var connectionString = _configuration.GetConnectionString("CosmosConnection");
```

**After:**
```csharp
// Still use IConfiguration for connection strings (security best practice)
var connectionString = _configuration.GetConnectionString("CosmosConnection");

// OR if you need it in the options object:
var connectionString = _config.ConnectionStrings.CosmosConnection;
```

### Pattern 2: Multiple Configuration Sections

**Before:**
```csharp
public MyService(IConfiguration configuration)
{
    var siteName = configuration["SiteMetadata:SiteName"];
    var dbName = configuration["CosmosDb:DatabaseName"];
}
```

**After (Option 1 - Individual Sections - RECOMMENDED):**
```csharp
public MyService(
    IOptions<SiteMetadata> siteMetadata,
    IOptions<CosmosDbSettings> cosmosDb)
{
    var siteName = siteMetadata.Value.SiteName;
    var dbName = cosmosDb.Value.DatabaseName;
}
```

**After (Option 2 - Full Config):**
```csharp
public MyService(IOptions<ViblogConfiguration> config)
{
    var siteName = config.Value.SiteMetadata.SiteName;
    var dbName = config.Value.CosmosDb.DatabaseName;
}
```

### Pattern 3: Optional Configuration Values

**Before:**
```csharp
var twitterHandle = _configuration["SiteMetadata:TwitterHandle"];
if (!string.IsNullOrEmpty(twitterHandle))
{
    // Use twitter handle
}
```

**After:**
```csharp
if (!string.IsNullOrEmpty(_siteMetadata.TwitterHandle))
{
    // Use twitter handle
}
```

## Benefits of the New System

1. **Type Safety** - Compile-time checking instead of runtime string keys
2. **IntelliSense** - Auto-completion for all configuration properties
3. **Refactoring** - Rename refactoring works across your entire codebase
4. **Testability** - Easier to create test data with `Options.Create()`
5. **Documentation** - XML comments on configuration properties
6. **Validation** - Can add data annotations for validation
7. **Separation of Concerns** - Services only get the configuration they need

## Gradual Migration

You don't have to migrate everything at once. The new system works alongside the old:

```csharp
// This still works
public MyService(IConfiguration configuration)
{
    var siteName = configuration["SiteMetadata:SiteName"];
}

// And this works in the same codebase
public MyOtherService(IOptions<SiteMetadata> siteMetadata)
{
    var siteName = siteMetadata.Value.SiteName;
}
```

Migrate services one at a time as you touch them for other changes.

## Checklist

- [ ] Update constructor to inject `IOptions<T>` instead of `IConfiguration`
- [ ] Store `.Value` in a private field in the constructor
- [ ] Replace all `_configuration["Section:Key"]` with `_sectionConfig.Property`
- [ ] Update tests to use `Options.Create()` instead of ConfigurationBuilder
- [ ] Update Blazor components to inject specific options
- [ ] Remove unused `using Microsoft.Extensions.Configuration;` statements
- [ ] Add `using Microsoft.Extensions.Options;` where needed

## Need Help?

See the following files:
- `README.md` - Full documentation
- `ConfigurationUsageExamples.cs` - Code examples
- `ConfigurationTestExamples.cs` - Testing examples
