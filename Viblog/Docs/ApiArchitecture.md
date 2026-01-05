# API Architecture Documentation

## Overview
The Vilog blog uses a clean, modular Minimal API architecture for all public API endpoints. This structure provides excellent organization, testability, and extensibility.

## Project Structure

```
Vilog/
??? Api/
?   ??? ApiServiceExtensions.cs        ? Central registration point
?   ??? Endpoints/
?       ??? FeedEndpoints.cs           ? RSS/Atom feed endpoints
?       ??? SitemapEndpoints.cs        ? Sitemap/robots.txt endpoints
```

## Architecture Pattern

### 1. Centralized Registration
All API endpoints are registered through a single extension method in `Program.cs`:

```csharp
// In Program.cs
app.MapBlogApiEndpoints();
```

### 2. Modular Endpoint Groups
Each feature area has its own endpoint class:

```csharp
namespace Vilog.Api.Endpoints;

public static class FeedEndpoints
{
    public static WebApplication MapFeedEndpoints(this WebApplication app)
    {
        // Define endpoints here
        return app;
    }
}
```

### 3. Service Extension Orchestration
The `ApiServiceExtensions` class orchestrates all endpoint groups:

```csharp
public static class ApiServiceExtensions
{
    public static WebApplication MapBlogApiEndpoints(this WebApplication app)
    {
        app.MapFeedEndpoints();
        app.MapSitemapEndpoints();
        // Add more endpoint groups as needed
        return app;
    }
}
```

## Current Endpoints

### Feed Endpoints (`/Api/Endpoints/FeedEndpoints.cs`)

**Purpose:** Provide RSS and Atom feeds for content syndication

**Endpoints:**

#### 1. GET /feed.xml
- **Description:** RSS 2.0 feed of recent blog posts
- **Content-Type:** `application/rss+xml`
- **Parameters:**
  - `maxPosts` (int, optional): Maximum posts to include (default: 20)
- **Response:** XML feed with post titles, descriptions, content, categories
- **OpenAPI:** ? Documented with Swagger/OpenAPI

#### 2. GET /atom.xml
- **Description:** Atom 1.0 feed of recent blog posts
- **Content-Type:** `application/atom+xml`
- **Parameters:**
  - `maxPosts` (int, optional): Maximum posts to include (default: 20)
- **Response:** XML feed compliant with Atom 1.0 specification
- **OpenAPI:** ? Documented with Swagger/OpenAPI

**Features:**
- Error handling with standardized problem details
- Async/await for non-blocking I/O
- Dependency injection of facades
- OpenAPI documentation
- Route grouping with tags

**Code Example:**
```csharp
var feedGroup = app.MapGroup("/")
    .WithTags("Feeds")
    .WithDescription("RSS and Atom feed endpoints");

feedGroup.MapGet("/feed.xml", GetRssFeed)
    .WithName("GetRssFeed")
    .WithSummary("Get RSS 2.0 feed")
    .WithDescription("Returns an RSS 2.0 formatted XML feed of recent posts")
    .Produces<string>(200, "application/rss+xml")
    .ProducesProblem(500);
```

### Sitemap Endpoints (`/Api/Endpoints/SitemapEndpoints.cs`)

**Purpose:** Provide SEO-friendly sitemaps and robots.txt

**Endpoints:**

#### 1. GET /sitemap.xml
- **Description:** XML sitemap of all blog pages
- **Content-Type:** `application/xml`
- **Response:** Sitemap with all public URLs
- **Status:** ?? Placeholder (ready for implementation)
- **OpenAPI:** ? Documented with Swagger/OpenAPI

#### 2. GET /robots.txt
- **Description:** Robots.txt file for search engine crawlers
- **Content-Type:** `text/plain`
- **Response:** Robot exclusion rules
- **Status:** ?? Placeholder (ready for implementation)
- **OpenAPI:** ? Documented with Swagger/OpenAPI

**Planned Features:**
- Dynamic sitemap generation
- Include all published posts
- Include category/tag pages
- Include archive pages
- Automatic URL discovery
- Change frequency hints
- Priority weighting

## Design Principles

### 1. Separation of Concerns
Each endpoint file handles one logical feature area:
- Feed-related endpoints ? `FeedEndpoints.cs`
- Sitemap-related endpoints ? `SitemapEndpoints.cs`
- Future: Health checks ? `HealthCheckEndpoints.cs`

### 2. OpenAPI/Swagger Integration
All endpoints are documented with modern ASP.NET Core methods:
- `.WithName()` - Unique operation ID for client generation
- `.WithSummary()` - Brief summary for Swagger UI
- `.WithDescription()` - Detailed description for documentation
- `.Produces<T>()` - Response type and content-type documentation
- `.ProducesProblem()` - Error response documentation
- `.WithTags()` - Logical grouping in Swagger UI

**Note:** The deprecated `.WithOpenApi()` method has been replaced with the more explicit `.WithSummary()` and `.WithDescription()` methods, which provide better control over OpenAPI metadata.

### 3. Error Handling
All endpoints implement standardized error handling:

```csharp
try
{
    var result = await GenerateContent();
    return Results.Content(result, "application/xml");
}
catch (Exception)
{
    return Results.Problem(
        statusCode: 500,
        title: "Generation Error",
        detail: "Unable to generate content.");
}
```

### 4. Dependency Injection
Endpoints receive dependencies via method parameters:

```csharp
private static async Task<IResult> GetRssFeed(
    IRssFeedFacade rssFeedFacade,  // ? Injected
    int maxPosts = 20,
    CancellationToken cancellationToken = default)
```

### 5. Route Grouping
Related endpoints are grouped for better organization:

```csharp
var feedGroup = app.MapGroup("/")
    .WithTags("Feeds")
    .WithDescription("Feed endpoints");
```

## Adding New Endpoints

### Step 1: Create Endpoint Class

Create a new file in `/Api/Endpoints/`:

```csharp
// /Api/Endpoints/WebhookEndpoints.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Vilog.Api.Endpoints;

public static class WebhookEndpoints
{
    public static WebApplication MapWebhookEndpoints(this WebApplication app)
    {
        var webhookGroup = app.MapGroup("/api/webhooks")
            .WithTags("Webhooks")
            .WithDescription("Webhook endpoints");

        webhookGroup.MapPost("/publish", PublishWebhook)
            .WithName("PublishWebhook")
            .WithDescription("Trigger on post publish")
            .Produces(200)
            .WithOpenApi();

        return app;
    }

    private static IResult PublishWebhook()
    {
        // Implementation
        return Results.Ok();
    }
}
```

### Step 2: Register in ApiServiceExtensions

Add the registration call:

```csharp
public static WebApplication MapBlogApiEndpoints(this WebApplication app)
{
    app.MapFeedEndpoints();
    app.MapSitemapEndpoints();
    app.MapWebhookEndpoints();  // ? Add here
    return app;
}
```

### Step 3: Build & Test

```bash
dotnet build
dotnet run
# Navigate to /swagger to see your new endpoints
```

## OpenAPI/Swagger Configuration

### Package Reference
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.1" />
```

### Benefits
- Automatic API documentation
- Swagger UI for testing
- Client SDK generation support
- API versioning support
- Request/response examples

### Accessing Swagger UI
In development mode:
```
https://localhost:5001/swagger
```

## Route Conventions

### Public API Routes
- Feeds: `/feed.xml`, `/atom.xml`
- Sitemap: `/sitemap.xml`, `/robots.txt`

### Future API Routes (Recommended)
- REST API: `/api/v1/{resource}`
- Webhooks: `/api/webhooks/{event}`
- Health: `/health`, `/health/ready`
- Metrics: `/metrics`

## Security Considerations

### Current Implementation
- Read-only endpoints (GET only)
- No authentication required
- Public access (intended for feeds/sitemaps)

### Future Considerations
For write endpoints, consider:
- API key authentication
- JWT bearer tokens
- Rate limiting
- CORS configuration
- Input validation
- Request size limits

Example with authentication:

```csharp
webhookGroup.MapPost("/publish", PublishWebhook)
    .RequireAuthorization()  // ? Add auth
    .WithName("PublishWebhook");
```

## Testing Endpoints

### Manual Testing
```bash
# RSS Feed
curl https://localhost:5001/feed.xml

# Atom Feed
curl https://localhost:5001/atom.xml

# With parameters
curl "https://localhost:5001/feed.xml?maxPosts=10"
```

### Unit Testing
```csharp
[Fact]
public async Task GetRssFeed_Returns_ValidXml()
{
    // Arrange
    var facade = new Mock<IRssFeedFacade>();
    facade.Setup(f => f.GenerateRssFeedAsync(20, default))
          .ReturnsAsync("<rss>...</rss>");

    // Act
    var result = await FeedEndpoints.GetRssFeed(
        facade.Object, 20, default);

    // Assert
    Assert.IsType<ContentResult>(result);
}
```

### Integration Testing
```csharp
[Fact]
public async Task GetRssFeed_ReturnsSuccessStatusCode()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/feed.xml");

    // Assert
    response.EnsureSuccessStatusCode();
    Assert.Equal("application/rss+xml", 
        response.Content.Headers.ContentType?.MediaType);
}
```

## Performance Considerations

### Caching
Consider adding response caching for feeds:

```csharp
feedGroup.MapGet("/feed.xml", GetRssFeed)
    .CacheOutput(policy => policy
        .Expire(TimeSpan.FromMinutes(15))
        .SetVaryByQuery("maxPosts"));
```

### Async Execution
All endpoints use async/await:
- Non-blocking I/O
- Better resource utilization
- Supports cancellation tokens

### Compression
Enable response compression in `Program.cs`:

```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = new[] 
    { 
        "application/rss+xml", 
        "application/atom+xml",
        "application/xml"
    };
});
```

## Monitoring & Logging

### Endpoint Logging
Endpoints automatically log requests via ASP.NET Core logging:

```
info: Microsoft.AspNetCore.Routing.EndpointMiddleware[0]
      Executing endpoint 'GetRssFeed'
```

### Custom Logging
Add logging in endpoint methods:

```csharp
private static async Task<IResult> GetRssFeed(
    IRssFeedFacade facade,
    ILogger<FeedEndpoints> logger)  // ? Inject logger
{
    logger.LogInformation("Generating RSS feed");
    // ...
}
```

### Metrics
Track endpoint metrics:
- Request count
- Response time
- Error rate
- Cache hit rate

## Future Enhancements

### Planned Endpoint Groups

1. **Health Check Endpoints**
   - `/health` - Liveness probe
   - `/health/ready` - Readiness probe
   - `/health/dependencies` - Dependency health

2. **Webhook Endpoints**
   - `/api/webhooks/publish` - Post published
   - `/api/webhooks/update` - Post updated
   - `/api/webhooks/delete` - Post deleted

3. **Analytics Endpoints**
   - `/api/analytics/popular` - Popular posts
   - `/api/analytics/trending` - Trending topics
   - `/api/analytics/stats` - Blog statistics

4. **Search Endpoints**
   - `/api/search` - Advanced search
   - `/api/search/suggest` - Auto-suggest
   - `/api/search/filters` - Available filters

5. **Export Endpoints**
   - `/api/export/markdown` - Export as Markdown
   - `/api/export/json` - Export as JSON
   - `/api/export/backup` - Full backup

## Best Practices

### DO ?
- Group related endpoints
- Use OpenAPI documentation
- Implement error handling
- Use dependency injection
- Follow async/await pattern
- Add XML doc comments
- Use route grouping
- Version your APIs

### DON'T ?
- Mix endpoint concerns
- Skip error handling
- Use blocking I/O
- Hardcode dependencies
- Ignore OpenAPI docs
- Skip validation
- Use magic strings
- Couple to implementation

## Related Files

### API Structure
- `Vilog\Api\ApiServiceExtensions.cs`
- `Vilog\Api\Endpoints\FeedEndpoints.cs`
- `Vilog\Api\Endpoints\SitemapEndpoints.cs`

### Registration
- `Vilog\Program.cs`

### Facades (Used by APIs)
- `Vilog\Shared\Facades\IRssFeedFacade.cs`
- `Vilog\Shared\Facades\RssFeedFacade.cs`

### Documentation
- This file: `ApiArchitecture.md`
- `BlogEnhancements.md` - Overall enhancements
- `IMPLEMENTATION_COMPLETE.md` - Implementation summary

## Summary

The Vilog API architecture provides:

? **Clean Organization** - Modular endpoint files  
? **Easy to Extend** - Add new endpoint groups easily  
? **Well Documented** - Full OpenAPI/Swagger support  
? **Production Ready** - Error handling, logging, async  
? **Testable** - Dependency injection, clear contracts  
? **Maintainable** - Separation of concerns, clear structure  

The architecture is ready to grow with your blog's needs!
