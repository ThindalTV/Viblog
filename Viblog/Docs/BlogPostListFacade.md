# BlogPostListFacade

## Overview
The `BlogPostListFacade` is responsible for managing blog post list operations, specifically providing paginated access to published blog posts. This facade follows the Display-Facade-Repository pattern and serves as the business logic layer for the blog posts listing page.

## Purpose
- Separate concerns: Keep the Posts.razor page focused on presentation
- Provide paginated access to published blog posts ordered by date
- Allow for future expansion of list-specific features (filtering, sorting, etc.)

## Interface: IBlogPostListFacade

### Methods

#### GetPaginatedPostsAsync
```csharp
Task<PagedResult<BlogPost>> GetPaginatedPostsAsync(
    PagingParameters pagingParameters, 
    CancellationToken cancellationToken = default)
```

**Description:** Retrieves a paginated list of published blog posts ordered by publish date (newest first).

**Parameters:**
- `pagingParameters`: Paging parameters containing page number and page size
- `cancellationToken`: Optional cancellation token

**Returns:** `PagedResult<BlogPost>` containing:
- Items: The blog posts for the current page
- PageNumber: Current page number
- PageSize: Number of items per page
- TotalCount: Total number of published posts
- TotalPages: Total number of pages
- HasPreviousPage: Whether a previous page exists
- HasNextPage: Whether a next page exists

**Throws:**
- `ArgumentNullException`: If pagingParameters is null

## Implementation: BlogPostListFacade

### Dependencies
- `IBlogPostRepository`: Used to retrieve published posts from the data layer

### Business Rules
1. Only published posts are returned
2. Posts are ordered by PublishedAt date in descending order (newest first)
3. Paging is handled at the repository level for efficiency

## Usage Example

### In Posts.razor
```csharp
@inject IBlogPostListFacade BlogPostListFacade

private async Task LoadPostsAsync()
{
    var pagingParams = new PagingParameters(currentPage, 5);
    var result = await BlogPostListFacade.GetPaginatedPostsAsync(pagingParams);
}
```

## Registration
The facade is registered in `ServiceExtensions.cs`:
```csharp
services.AddScoped<IBlogPostListFacade, BlogPostListFacade>();
```

## Design Decisions

### Why a Separate Facade?
1. **Single Responsibility**: `FrontPageFacade` handles front page logic (featured posts, latest posts mix), while `BlogPostListFacade` handles simple paginated lists
2. **Future Extensibility**: List page may need filtering by category, tag, search, etc.
3. **Clear Dependencies**: Pages depend only on what they need

### Why Not Use Repository Directly?
- Facades allow for business logic (e.g., future filtering, caching)
- Consistent architecture across the application
- Easier to test and mock
- Virtual methods enable testing scenarios

## Future Enhancements
Potential additions to this facade:
- `GetPostsByCategoryAsync(categoryId, pagingParams)`
- `GetPostsByTagAsync(tag, pagingParams)`
- `SearchPostsAsync(searchTerm, pagingParams)`
- Caching of frequently accessed pages
- Post view count tracking
