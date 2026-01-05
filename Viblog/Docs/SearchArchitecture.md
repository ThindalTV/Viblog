# Blog Search Architecture

## Overview
This document describes the search abstraction layer implemented for the Vilog blogging platform.

## Architecture

### Separation of Concerns
- **Repository Layer**: Handles data access for blog posts (CRUD operations)
- **Service Layer**: Handles business logic and search operations
- **Future-Proof**: Easy to swap search implementations (e.g., Azure Cognitive Search)

## Components

### 1. IBlogSearchService (Interface)
Location: `Vilog\Shared\Services\IBlogSearchService.cs`

Defines the contract for blog search operations:
- `SearchAsync()` - General search across all indexed content
- `SearchByTitleAsync()` - Search limited to post titles
- `SearchMultipleTermsAsync()` - AND logic search with multiple terms
- `GetRelatedPostsAsync()` - Find related posts based on tags/categories

### 2. BlogSearchService (Implementation)
Location: `Vilog\Shared\Services\BlogSearchService.cs`

Default implementation using the repository pattern:
- Uses `SearchIndex` property for efficient searching
- Case-insensitive search
- Supports published/draft filtering
- Implements pagination
- Virtual methods for easy testing/mocking

### 3. SearchIndex Property
Location: `Vilog\Shared\Data\Entities\BlogPost.cs`

Optimized search field that concatenates:
- Title
- Short description
- Content
- Tags
- Category names
- Author name

**Auto-maintained**: Repository automatically updates this field on Add/Update operations.

### 4. Service Registration
Location: `Vilog\Shared\Services\ServiceExtensions.cs`

Extension method `AddBlogServices()` for DI registration.

## Usage Examples

### Basic Search
```csharp
public class BlogPage : ComponentBase
{
    [Inject]
    private IBlogSearchService SearchService { get; set; } = default!;

    private async Task SearchAsync(string term)
    {
        var results = await SearchService.SearchAsync(
            term, 
            new PagingParameters(1, 10)
        );
        
        // Display results
    }
}
```

### Title-Only Search
```csharp
var results = await SearchService.SearchByTitleAsync(
    "blazor",
    new PagingParameters(1, 10),
    publishedOnly: true
);
```

### Multi-Term Search (AND Logic)
```csharp
var results = await SearchService.SearchMultipleTermsAsync(
    new[] { "blazor", "performance", "tips" },
    new PagingParameters(1, 10)
);
```

### Related Posts
```csharp
var relatedPosts = await SearchService.GetRelatedPostsAsync(
    currentPostId,
    partitionKey,
    maxResults: 5
);
```

## Benefits

### 1. Clean Separation
- Repository focuses on data access
- Service layer handles business logic
- Easy to understand and maintain

### 2. Testability
```csharp
// Easy to mock for unit tests
var mockSearchService = new Mock<IBlogSearchService>();
mockSearchService
    .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<PagingParameters>(), true, default))
    .ReturnsAsync(new PagedResult<BlogPost>(posts, 10, 1, 10));
```

### 3. Future-Proof
Easy to add new implementations:
```csharp
// Azure Cognitive Search implementation
public class AzureCognitiveSearchService : IBlogSearchService
{
    // Implementation using Azure SDK
}

// Register in ServiceExtensions.cs
public static IServiceCollection AddBlogServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var searchProvider = configuration["Search:Provider"];
    
    if (searchProvider == "AzureCognitive")
    {
        services.AddScoped<IBlogSearchService, AzureCognitiveSearchService>();
    }
    else
    {
        services.AddScoped<IBlogSearchService, BlogSearchService>();
    }
    
    return services;
}
```

### 4. Performance
- Single field search (`SearchIndex`) vs. multiple field queries
- Optimized for CosmosDB
- Lowercase index for case-insensitive matching

## Migration Path to Azure Cognitive Search

When ready to upgrade:

1. Create `AzureCognitiveSearchService` implementing `IBlogSearchService`
2. Add configuration for Azure Search connection
3. Update `ServiceExtensions.cs` to choose implementation
4. No changes needed in consuming code (pages, components)

## Testing

### Unit Tests Example
```csharp
public class BlogSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_WithValidTerm_ReturnsMatchingPosts()
    {
        // Arrange
        var mockRepo = new Mock<IBlogPostRepository>();
        var service = new BlogSearchService(mockRepo.Object);
        
        // Act
        var result = await service.SearchAsync("test", new PagingParameters(1, 10));
        
        // Assert
        Assert.NotNull(result);
    }
}
```

## Notes

- All search methods support pagination
- Published-only filter available for public vs. admin views
- CancellationToken support for async operations
- Virtual methods allow for easy overriding in derived classes
