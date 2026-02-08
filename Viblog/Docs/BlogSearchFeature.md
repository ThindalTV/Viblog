# Blog Search Feature

## Overview
The blog now includes a comprehensive search feature that allows users to search for posts from anywhere on the site. The search functionality is integrated into the header and provides paginated results using the familiar PostCard component.

## Components

### 1. BlogSearchFacade
**Location:** `Viblog\Shared\Facades\BlogSearchFacade.cs`

**Purpose:** Provides business logic layer for blog search operations.

**Methods:**
- `SearchPostsAsync(searchTerm, pagingParameters)`: Returns paginated search results for published posts

**Registration:** `services.AddScoped<IBlogSearchFacade, BlogSearchFacade>()`

### 2. Search.razor Page
**Location:** `Viblog\Frontend\Pages\Search.razor`

**Routes:**
- `/search?q={query}` - First page of results
- `/search/{pageNumber}?q={query}` - Specific page of results

**Features:**
- Search input box for new searches
- Display search query in title
- Paginated results (5 posts per page)
- Result count summary
- Empty states (no query, no results)
- Loading and error states
- Static prerendering support

**Parameters:**
- `PageNumber` (int?, optional): Page number for pagination
- `Query` (string?, from query string): Search term

### 3. BlogLayout Enhancement
**Location:** `Viblog\Frontend\Layout\BlogLayout.razor`

**New Features:**
- Search form in header (always visible)
- Search icon button
- Responsive design (moves below branding on mobile)

**Functionality:**
- Allows searching from any page
- Navigates to `/search?q={query}` on submit
- Clears after navigation

## URL Structure

### Search URLs
- Format: `/search?q={query}` or `/search/{pageNumber}?q={query}`
- Examples:
  - `/search?q=blazor` - Search for "blazor"
  - `/search/2?q=async` - Page 2 of "async" results
  - `/search?q=entity%20framework` - Multi-word search (URL-encoded)

### Query String Parameter
- **Parameter**: `q`
- **Required**: Yes (for results to show)
- **Encoding**: URL-encoded automatically
- **Examples**:
  - `?q=blazor`
  - `?q=async%20programming`
  - `?q=c%23`

## Search Flow

### From Header (Any Page)
```
User enters search ? Presses Enter/Clicks Search ? Navigate to /search?q={query}
```

### From Search Page
```
User modifies search ? Presses Enter ? Navigate to /search?q={newQuery}
```

### Pagination
```
/search?q=blazor ? Click Page 2 ? /search/2?q=blazor
```

## User Interface

### Header Search Form
**Desktop:**
```
???????????????????????????????????????????????????
? Your Name                        [Search...][??]?
? Developer | Consultant | Educator               ?
???????????????????????????????????????????????????
```

**Mobile:**
```
????????????????????????
?   Your Name          ?
?   Developer...       ?
?   [Search...   ][??] ?
????????????????????????
```

### Search Results Page
```
Search Results for "blazor"

??????????????????????????????????
? [Search box with query filled] ?
??????????????????????????????????

Found 15 posts matching "blazor"

[Post Card 1]
[Post Card 2]
[Post Card 3]
[Post Card 4]
[Post Card 5]

[Pagination: First Prev 1 2 3 Next Last]
```

## States

### Initial State (No Query)
```
Search

??????????????????????????????????
? [Empty search box]             ?
??????????????????????????????????

Enter a search term above to find posts.
```

### Loading State
```
Search Results for "blazor"

??????????????????????????????????
? [Search box with "blazor"]     ?
??????????????????????????????????

Searching...
```

### Results Found
```
Search Results for "blazor"

??????????????????????????????????
? [Search box with "blazor"]     ?
??????????????????????????????????

Found 15 posts matching "blazor"

[PostCard components displaying results]
[Pagination if > 5 results]
```

### No Results
```
Search Results for "xyz123"

??????????????????????????????????
? [Search box with "xyz123"]     ?
??????????????????????????????????

??????????????????????????????????
? No Results Found               ?
?                                ?
? No posts found matching        ?
? "xyz123". Try different        ?
? keywords or browse all posts.  ?
??????????????????????????????????
```

### Error State
```
Search Results for "blazor"

??????????????????????????????????
? [Search box with "blazor"]     ?
??????????????????????????????????

Unable to search blog posts. Please try again later.
```

## Search Implementation

### Backend Service
Uses the existing `IBlogSearchService` which implements full-text search:

```csharp
Task<PagedResult<BlogPost>> SearchAsync(
    string searchTerm,
    PagingParameters pagingParameters,
    bool publishedOnly = true,
    CancellationToken cancellationToken = default);
```

### Search Scope
Searches across:
- Post title
- Post content
- Post excerpt (short)
- Tags
- Category names
- Author name

### Search Index
The `BlogPost` entity includes a `SearchIndex` property:
```csharp
public string SearchIndex { get; set; } = string.Empty;
```

This is a lowercase concatenation of all searchable content for efficient searching.

## Styling

### Search.razor.css
**Key Styles:**
- `.search-box` - Prominent search input area
- `.search-form` - Flex layout for input + button
- `.search-input` - Full-width text input with focus state
- `.search-button` - Primary action button
- `.results-summary` - Result count banner
- `.search-prompt` - Empty state message
- Pagination styles (consistent with other list pages)

### BlogLayout.razor.css Updates
**New Styles:**
- `.header-top` - Flex container for branding + search
- `.header-branding` - Logo and tagline section
- `.header-search` - Search form container (300px desktop)
- `.search-input` - Transparent white input with dark background
- `.search-button` - Icon button with search SVG
- Responsive breakpoint moves search below branding

## User Experience

### Discoverability
- Search always visible in header
- Accessible from every page
- Clear search icon
- Placeholder text guides users

### Interaction
1. User types search term
2. Presses Enter or clicks search icon
3. Navigates to results page
4. Results displayed with query highlighted in title
5. Can modify search from results page
6. Pagination available for many results

### Feedback
- Loading state during search
- Result count ("Found 15 posts...")
- Empty state with helpful message
- Error state with retry option

## Pagination

### URL Pattern
```
/search?q=blazor          ? Page 1
/search/2?q=blazor        ? Page 2
/search/3?q=blazor        ? Page 3
```

### Navigation
- First/Previous/Next/Last buttons
- Page number buttons (shows 5 at a time)
- Maintains search query in URL
- Disabled states for unavailable pages

### Posts Per Page
- **5 posts** per page (same as other list pages)
- Consistent user experience
- Fast page loads

## Accessibility

### ARIA Labels
```html
<input aria-label="Search posts" />
<button aria-label="Search" />
<nav aria-label="Search results pagination" />
```

### Keyboard Navigation
- Tab through search form
- Enter to submit
- Focus states on all interactive elements
- Keyboard navigation in pagination

### Screen Readers
- Clear labeling of search function
- Result count announced
- Pagination state communicated

## Performance

### Static Prerendering
- Initial HTML rendered on server
- Fast first paint
- SEO-friendly search results pages
- Hydration for interactivity

### Search Optimization
- Server-side search (efficient)
- Paginated results (no large payloads)
- Cached search index in database
- Minimal client-side processing

## SEO Considerations

### Search Result URLs
```
https://yourblog.com/search?q=blazor
```

- Unique URL for each query
- Bookmarkable and shareable
- Indexed by search engines (if desired)

### robots.txt Consideration
You may want to exclude search results from indexing:
```
User-agent: *
Disallow: /search
```

This prevents duplicate content issues and focuses search engines on actual content.

## Usage Examples

### Searching from Header
```
1. User on Home page
2. Types "blazor" in header search
3. Presses Enter
4. Navigates to /search?q=blazor
5. Sees results
```

### Refining Search
```
1. User on /search?q=blazor
2. Modifies search to "blazor components"
3. Presses Enter
4. Navigates to /search?q=blazor%20components
5. Sees updated results
```

### Pagination
```
1. User on /search?q=async
2. 15 results found (3 pages)
3. Clicks "Page 2"
4. Navigates to /search/2?q=async
5. Sees results 6-10
```

### Empty Search
```
1. User navigates directly to /search
2. No query parameter
3. Sees search box with prompt
4. Enters query and searches
```

## Edge Cases

### Empty Query
- Header form: Does nothing (requires text)
- Direct navigation to `/search`: Shows prompt

### Whitespace Only
- Trimmed before searching
- Empty after trim: No search performed

### Special Characters
- URL-encoded automatically
- Works with: `#`, `%`, `&`, `+`, spaces, etc.

### Very Long Queries
- No length limit enforced
- Search service handles gracefully

### No Results
- Friendly message
- Link to browse all posts
- Can modify search term

## Testing Scenarios

### Functional Tests
1. Search from header ? Results page loads
2. Enter query on results page ? Updates results
3. Pagination maintains query
4. Empty search shows prompt
5. No results shows message
6. Special characters work
7. Multi-word searches work

### URL Tests
```
/search?q=blazor                 ? Valid
/search/2?q=async                ? Valid
/search?q=c%23                   ? Valid (# encoded)
/search?q=entity%20framework     ? Valid (space encoded)
/search                          ? Valid (shows prompt)
/search/1?q=test                 ? Valid (same as /search?q=test)
```

### Edge Case Tests
1. Very long query ? Handles gracefully
2. Query with HTML ? Properly escaped
3. Query with SQL ? Properly escaped
4. Query with emoji ? Works correctly
5. Multiple spaces ? Trimmed and normalized

## Future Enhancements

Potential improvements:
- **Search Suggestions**: Auto-complete as user types
- **Recent Searches**: Show recent search history
- **Popular Searches**: Display trending queries
- **Search Filters**: Filter by date, category, tag
- **Advanced Search**: Boolean operators (AND, OR, NOT)
- **Search Highlighting**: Highlight matching terms in results
- **Search Analytics**: Track popular queries
- **Fuzzy Matching**: "Did you mean...?" suggestions
- **Search API**: Expose search as API endpoint
- **Saved Searches**: Allow users to save queries

## Related Files

### Core Implementation
- `Viblog\Frontend\Pages\Search.razor`
- `Viblog\Frontend\Pages\Search.razor.css`
- `Viblog\Shared\Facades\IBlogSearchFacade.cs`
- `Viblog\Shared\Facades\BlogSearchFacade.cs`

### Layout Updates
- `Viblog\Frontend\Layout\BlogLayout.razor`
- `Viblog\Frontend\Layout\BlogLayout.razor.css`

### Services
- `Viblog\Shared\Services\IBlogSearchService.cs`
- `Viblog\Shared\Services\BlogSearchService.cs`

### Configuration
- `Viblog\Shared\Services\ServiceExtensions.cs`

### Reused Components
- `Viblog\Frontend\Components\PostCard.razor`

## Best Practices

### For Users
1. Use specific keywords for better results
2. Try different search terms if no results
3. Browse categories/tags for discovery
4. Bookmark useful searches

### For Developers
1. Keep search visible and accessible
2. Provide clear feedback (loading, results count)
3. Handle empty and error states gracefully
4. Maintain search query in pagination
5. URL-encode query parameters
6. Use semantic HTML and ARIA labels
7. Test with various query types

### For Content Creators
1. Use descriptive titles for better discoverability
2. Include keywords in content and excerpts
3. Tag posts appropriately
4. Use categories meaningfully
5. Update `SearchIndex` when content changes
