# Front Page Facade Pattern

## Overview
The Front Page Facade provides a simplified interface for retrieving blog posts to display on the home page. It implements the Display-Facade-Repository pattern used throughout the Vilog application.

## Architecture

### Pattern: Display ? Facade ? Repository

```
Index.razor (Display)
    ?
FrontPageFacade (Business Logic)
    ?
BlogPostRepository (Data Access)
    ?
CosmosDB
```

## Components

### 1. IFrontPageFacade (Interface)
Location: `Vilog\Shared\Facades\IFrontPageFacade.cs`

Defines the contract for front page operations:
- `GetFrontPagePostsAsync()` - Main method: Gets featured + latest posts (max 8)
- `GetRecentFeaturedPostsAsync()` - Gets featured posts from last month
- `GetLatestPostsAsync()` - Gets latest published posts

### 2. FrontPageFacade (Implementation)
Location: `Vilog\Shared\Facades\FrontPageFacade.cs`

Business logic for assembling the front page:

**Algorithm for `GetFrontPagePostsAsync()`:**
1. Get featured posts from the last month (published within last 30 days)
2. Sort by publish date descending (newest first)
3. Calculate remaining slots (maxPosts - featured count)
4. Fill remaining slots with latest posts (excluding featured posts already shown)
5. Return combined list (featured first, then latest)

**Key Features:**
- No duplicate posts
- Featured posts always appear first
- Total posts capped at specified limit (default: 8)
- Only published posts shown
- Virtual methods for testability

### 3. Index.razor (Display)
Location: `Vilog\Frontend\Pages\Index.razor`

Front-end display component:
- Injects `IFrontPageFacade`
- Loads posts on initialization
- Displays loading state
- Shows error message if load fails
- Renders posts with metadata (author, date, reading time, tags)
- Highlights featured posts with badge

## Usage Example

### In a Blazor Component
```csharp
@inject IFrontPageFacade FrontPageFacade

@code {
    private IEnumerable<BlogPost> _posts = Enumerable.Empty<BlogPost>();

    protected override async Task OnInitializedAsync()
    {
        _posts = await FrontPageFacade.GetFrontPagePostsAsync(maxPosts: 8);
    }
}
```

### In a Service or Controller
```csharp
public class BlogService
{
    private readonly IFrontPageFacade _frontPageFacade;

    public BlogService(IFrontPageFacade frontPageFacade)
    {
        _frontPageFacade = frontPageFacade;
    }

    public async Task<IEnumerable<BlogPost>> GetHomePagePosts()
    {
        return await _frontPageFacade.GetFrontPagePostsAsync(maxPosts: 10);
    }
}
```

## Configuration

### Service Registration
Located in `Vilog\Shared\Services\ServiceExtensions.cs`:

```csharp
services.AddScoped<IFrontPageFacade, FrontPageFacade>();
```

Registered in `Program.cs`:
```csharp
builder.Services.AddBlogServices();
```

### Default Settings
- Maximum posts: 8
- Featured post timeframe: Last 30 days
- Sort order: Newest first (PublishedAt descending)

## Benefits

### 1. Separation of Concerns
- **Display Layer**: Only handles rendering
- **Facade Layer**: Contains business logic
- **Repository Layer**: Handles data access

### 2. Testability
```csharp
// Easy to mock for unit tests
var mockFacade = new Mock<IFrontPageFacade>();
mockFacade
    .Setup(f => f.GetFrontPagePostsAsync(8, default))
    .ReturnsAsync(new List<BlogPost> { /* test data */ });
```

### 3. Maintainability
- Business logic centralized in facade
- Easy to modify featured post criteria
- Simple to add new front page features

### 4. Performance
- Single database query for featured posts
- Single database query for latest posts
- Efficient de-duplication in memory

## Front Page Display Features

### Post Information Shown
- ? Featured badge (for featured posts)
- ? Title (linked to full post)
- ? Author name
- ? Published date
- ? Reading time (if available)
- ? Short excerpt
- ? Tags (first 3)

### States Handled
- ? Loading state
- ? Error state
- ? Empty state (no posts)
- ? Normal state (displaying posts)

## Styling

Location: `Vilog\wwwroot\blog.css`

Key CSS classes:
- `.blog-post` - Post container
- `.featured-badge` - Featured indicator
- `.post-title` - Post heading
- `.post-meta` - Author, date, reading time
- `.post-excerpt` - Summary text
- `.post-tags` - Tag list
- `.tag` - Individual tag
- `.loading-message` - Loading indicator
- `.error-message` - Error display

## Future Enhancements

### Possible Improvements
1. **Caching**: Add response caching for performance
2. **Personalization**: Show posts based on user preferences
3. **Analytics**: Track which posts get clicked
4. **A/B Testing**: Test different post ordering strategies
5. **Infinite Scroll**: Load more posts dynamically
6. **Featured Post Rotation**: Rotate featured posts if more than max

### Example: Adding Caching
```csharp
public class CachedFrontPageFacade : IFrontPageFacade
{
    private readonly IFrontPageFacade _innerFacade;
    private readonly IMemoryCache _cache;

    public async Task<IEnumerable<BlogPost>> GetFrontPagePostsAsync(
        int maxPosts = 8, 
        CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync("front-page-posts", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _innerFacade.GetFrontPagePostsAsync(maxPosts, ct);
        });
    }
}
```

## Testing

### Unit Test Example
```csharp
[Fact]
public async Task GetFrontPagePostsAsync_WithFeaturedAndLatest_ReturnsCombinedList()
{
    // Arrange
    var mockRepo = new Mock<IBlogPostRepository>();
    var facade = new FrontPageFacade(mockRepo.Object);
    
    // Act
    var result = await facade.GetFrontPagePostsAsync(8);
    
    // Assert
    Assert.NotNull(result);
    Assert.True(result.Count() <= 8);
}
```

## Notes

- Featured posts are determined by the `IsFeatured` flag on BlogPost
- Only posts published within the last month are considered for featured status
- The facade ensures no duplicate posts appear in the combined list
- All methods are virtual to support mocking and inheritance
- Error handling should be implemented in the display layer
