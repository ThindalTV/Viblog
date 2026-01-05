# Category and Tag Filtering Pages

## Overview
The blog now includes dedicated pages for filtering posts by category and tag, providing users with multiple ways to discover and browse content. Both pages support pagination and use the same clean interface as the main posts list.

## Components

### 1. CategoryPostsFacade
**Location:** `Vilog\Shared\Facades\CategoryPostsFacade.cs`

**Purpose:** Provides business logic for retrieving posts filtered by category.

**Methods:**
- `GetPostsByCategoryAsync(categoryId, pagingParameters)`: Returns paginated posts for a specific category

**Registration:** `services.AddScoped<ICategoryPostsFacade, CategoryPostsFacade>()`

### 2. TagPostsFacade
**Location:** `Vilog\Shared\Facades\TagPostsFacade.cs`

**Purpose:** Provides business logic for retrieving posts filtered by tag.

**Methods:**
- `GetPostsByTagAsync(tag, pagingParameters)`: Returns paginated posts for a specific tag

**Registration:** `services.AddScoped<ITagPostsFacade, TagPostsFacade>()`

### 3. Category.razor Page
**Location:** `Vilog\Frontend\Pages\Category.razor`

**Routes:**
- `/category/{categoryId}` - First page
- `/category/{categoryId}/{pageNumber}` - Specific page

**Features:**
- Displays posts filtered by category
- 5 posts per page
- Full pagination controls
- Empty state for categories with no posts
- Back navigation to all posts
- Static prerendering support

**Parameters:**
- `CategoryId` (string): The category identifier
- `PageNumber` (int?, optional): Page number for pagination

### 4. Tag.razor Page
**Location:** `Vilog\Frontend\Pages\Tag.razor`

**Routes:**
- `/tag/{tagName}` - First page
- `/tag/{tagName}/{pageNumber}` - Specific page

**Features:**
- Displays posts filtered by tag
- 5 posts per page
- Full pagination controls
- Empty state for tags with no posts
- Back navigation to all posts
- Static prerendering support

**Parameters:**
- `TagName` (string): The tag name
- `PageNumber` (int?, optional): Page number for pagination

## URL Structure

### Category URLs
- Format: `/category/{categoryId}` or `/category/{categoryId}/{pageNumber}`
- Examples:
  - `/category/dotnet` - First page of .NET category
  - `/category/azure/2` - Page 2 of Azure category
  - `/category/web%20development` - URL-encoded category name

### Tag URLs
- Format: `/tag/{tagName}` or `/tag/{tagName}/{pageNumber}`
- Examples:
  - `/tag/blazor` - First page of Blazor tag
  - `/tag/async/3` - Page 3 of async tag
  - `/tag/entity%20framework` - URL-encoded tag name

## Enhanced PostCard Component

### New Features
The `PostCard` component now supports clickable categories and tags.

### New Parameters
```csharp
[Parameter]
public bool ShowCategories { get; set; } = true;

[Parameter]
public int MaxCategoriesToShow { get; set; } = 3;
```

### Rendered Links
**Categories:**
```html
<div class="post-categories">
    <a href="/category/dotnet" class="category-link">DotNet</a>
    <a href="/category/azure" class="category-link">Azure</a>
</div>
```

**Tags:**
```html
<div class="post-tags">
    <a href="/tag/blazor" class="tag-link">Blazor</a>
    <a href="/tag/csharp" class="tag-link">CSharp</a>
</div>
```

## Navigation Flow

### From Any Page to Filtered View
```
PostCard ? Click Category Badge ? Category Page
PostCard ? Click Tag Badge ? Tag Page
Post Detail ? Click Category ? Category Page
Post Detail ? Click Tag ? Tag Page
```

### Within Filtered Views
```
Category Page ? Pagination ? Category Page (different page)
Tag Page ? Pagination ? Tag Page (different page)
Category/Tag Page ? "View All Posts" ? Posts List
```

### URL Navigation
Users can bookmark or share specific pages:
```
https://yourblog.com/category/blazor/2
https://yourblog.com/tag/async
```

## Styling

### Category.razor.css
- Section title with category name
- Pagination controls
- Back navigation link
- Responsive mobile styles

### Tag.razor.css
- Section title with tag name
- Pagination controls
- Back navigation link
- Responsive mobile styles

### PostCard.razor.css Updates
**New Styles:**
- `.post-categories` - Category container
- `.category-link` - Clickable category badge (black background, white text)
- `.tag-link` - Clickable tag badge (light gray background)
- Hover effects with transform animations

### Post.razor.css Updates
**Updated Styles:**
- `.category` - Now clickable link (changed from static badge)
- `.tag` - Now clickable link with hover effects

## Category vs Tag Distinction

### Categories (Hierarchical)
- **Purpose**: Broad classification of content
- **Visual**: Black badges with white text (prominent)
- **Examples**: "DotNet", "Azure", "Web Development"
- **URL Pattern**: `/category/{categoryId}`
- **Data**: Stored as IDs with denormalized names

### Tags (Metadata)
- **Purpose**: Specific topics and keywords
- **Visual**: Gray badges (subtle)
- **Examples**: "blazor", "async", "performance"
- **URL Pattern**: `/tag/{tagName}`
- **Data**: Stored as strings directly

## Empty States

### No Posts in Category
```html
<article class="blog-post">
    <h3 class="post-title">No Posts in This Category</h3>
    <p class="post-excerpt">
        There are no published posts in this category yet.
    </p>
    <div class="back-navigation">
        <a href="/posts" class="back-link">? View All Posts</a>
    </div>
</article>
```

### No Posts with Tag
Similar to category, with appropriate messaging.

## Static Prerendering

Both pages support static prerendering through Blazor's built-in SSR:

### How It Works
1. Server renders initial HTML with posts
2. User sees content immediately
3. Blazor hydrates for interactivity
4. Navigation is client-side SPA

### SEO Benefits
- Search engines can crawl category/tag pages
- Each filtered view has unique URL
- Server-rendered HTML includes post content
- Fast perceived performance

## Usage Examples

### Linking to a Category
```razor
<a href="/category/@Uri.EscapeDataString(categoryName)">
    View @categoryName Posts
</a>
```

### Linking to a Tag
```razor
<a href="/tag/@Uri.EscapeDataString(tagName)">
    Posts tagged with @tagName
</a>
```

### PostCard with Categories and Tags
```razor
<!-- Default: Shows both with links -->
<PostCard Post="post" />

<!-- Without categories -->
<PostCard Post="post" ShowCategories="false" />

<!-- Show more items -->
<PostCard Post="post" 
          MaxCategoriesToShow="5" 
          MaxTagsToShow="5" />
```

## Repository Methods

### Category Filtering
```csharp
Task<PagedResult<BlogPost>> GetPostsByCategoryAsync(
    string categoryId,
    PagingParameters pagingParameters,
    bool publishedOnly = true,
    CancellationToken cancellationToken = default);
```

### Tag Filtering
```csharp
Task<PagedResult<BlogPost>> GetPostsByTagAsync(
    string tag,
    PagingParameters pagingParameters,
    bool publishedOnly = true,
    CancellationToken cancellationToken = default);
```

## Error Handling

### Loading States
- Shows "Loading posts..." during data fetch
- Graceful error messages on failure
- Empty states for no results

### URL Validation
- Category IDs and tag names are URL-encoded
- Navigation validates page numbers
- Invalid pages don't navigate

## User Experience

### Discoverability
- Categories visible in post cards
- Tags visible in post cards and detail page
- Clicking any badge navigates to filtered view

### Navigation
- Breadcrumb-like back link to all posts
- Pagination for large filtered sets
- Consistent UI across all list pages

### Performance
- Paginated results (5 per page)
- Server-side filtering (efficient queries)
- Static prerendering (fast first load)

## Testing Scenarios

### Functional Tests
1. Click category badge ? Navigates to category page
2. Click tag badge ? Navigates to tag page
3. Pagination works on filtered pages
4. Back navigation returns to posts list
5. Direct URL access works
6. URL encoding handles special characters

### Edge Cases
1. Category with no posts ? Shows empty state
2. Tag with no posts ? Shows empty state
3. Non-existent category ? Shows empty state
4. Non-existent tag ? Shows empty state
5. Page number beyond range ? Validation prevents navigation

### URL Scenarios
```
/category/dotnet           ? Valid
/category/dotnet/2         ? Valid
/category/web%20dev        ? Valid (URL encoded)
/tag/blazor                ? Valid
/tag/c%23                  ? Valid (# encoded)
```

## Future Enhancements

Potential improvements:
- **Category Hierarchy**: Support for subcategories
- **Tag Cloud**: Visual representation of tag popularity
- **Related Categories**: Show similar categories
- **Category/Tag Counts**: Display number of posts
- **Filtering Combinations**: Filter by multiple tags/categories
- **Category Archives**: Browse by date within category
- **Tag Autocomplete**: Suggest tags as user types
- **Category Descriptions**: Rich text descriptions for categories
- **RSS Feeds**: Per-category and per-tag feeds

## Related Files

### Core Implementation
- `Vilog\Frontend\Pages\Category.razor`
- `Vilog\Frontend\Pages\Category.razor.css`
- `Vilog\Frontend\Pages\Tag.razor`
- `Vilog\Frontend\Pages\Tag.razor.css`
- `Vilog\Shared\Facades\ICategoryPostsFacade.cs`
- `Vilog\Shared\Facades\CategoryPostsFacade.cs`
- `Vilog\Shared\Facades\ITagPostsFacade.cs`
- `Vilog\Shared\Facades\TagPostsFacade.cs`

### Updated Components
- `Vilog\Frontend\Components\PostCard.razor`
- `Vilog\Frontend\Components\PostCard.razor.css`
- `Vilog\Frontend\Pages\Post.razor`
- `Vilog\Frontend\Pages\Post.razor.css`

### Configuration
- `Vilog\Shared\Services\ServiceExtensions.cs`

### Data Layer
- `Vilog\Shared\Data\Repositories\IBlogPostRepository.cs`
- `Vilog\Shared\Data\Repositories\BlogPostRepository.cs`

## Best Practices

### When Creating Posts
1. Assign relevant categories (broad classification)
2. Add specific tags (keywords, topics)
3. Use consistent naming (e.g., "DotNet" not ".NET" or "dotnet")
4. Limit categories to 1-3 per post
5. Use 3-7 tags per post

### When Displaying
1. Show categories prominently (3 max in cards)
2. Show tags subtly (3 max in cards, all in detail)
3. Make both clickable for navigation
4. URL-encode when building links

### For SEO
1. Use descriptive category names
2. Use lowercase tags (consistency)
3. Create dedicated landing pages for important categories
4. Add category/tag descriptions (future)
5. Generate sitemaps including category/tag URLs
