# Blog Post Detail Feature

## Overview
The blog post detail feature displays the full content of a blog post with rich formatting, metadata, and navigation. It supports both long-form posts with full content and short-only posts without additional detail pages.

## Components

### 1. BlogPostDetailFacade
**Location:** `Viblog\Shared\Facades\BlogPostDetailFacade.cs`

**Purpose:** Provides business logic for retrieving and managing individual blog posts.

**Methods:**
- `GetPostBySlugAsync(slug)`: Retrieves a published post by its URL-friendly slug
- `IncrementViewCountAsync(id, partitionKey)`: Increments the view count for a post

**Registration:** `services.AddScoped<IBlogPostDetailFacade, BlogPostDetailFacade>()`

### 2. Post.razor
**Location:** `Viblog\Frontend\Pages\Post.razor`

**Route:** `/post/{slug}`

**Purpose:** Displays the full content of a blog post with all its metadata.

**Features:**
- SEO-friendly URLs using slugs
- Displays full markdown-rendered content
- Shows post metadata (author, date, reading time, view count)
- Featured image support
- Categories and tags display
- Automatic view count tracking
- Loading and error states
- Navigation back to posts list
- Static prerendering support

**Parameters:**
- `Slug` (string): URL-friendly identifier for the post

### 3. PostCard Component Updates
**Location:** `Viblog\Frontend\Components\PostCard.razor`

**New Features:**
- Conditional linking based on content availability
- "Read more" link for posts with long-form content
- Non-linked title for short-only posts

**Logic:**
```csharp
private bool HasLongFormContent => 
    !string.IsNullOrWhiteSpace(Post.Content) || 
    !string.IsNullOrWhiteSpace(Post.Markdown);
```

**New Parameters:**
- `ShowReadMore` (bool, default: true): Whether to show "Read more" link

## URL Structure

### Post Detail URLs
- Format: `/post/{slug}`
- Examples:
  - `/post/getting-started-with-blazor`
  - `/post/async-programming-best-practices`
  - `/post/2024-year-in-review`

### Benefits of Slug-Based URLs
1. **SEO-friendly**: Descriptive, readable URLs
2. **User-friendly**: Easy to share and remember
3. **Permanent**: Can remain unchanged even if title is edited
4. **Clean**: No numeric IDs or query parameters

## Static Prerendering

### Configuration
The frontend pages are configured for static server-side rendering (SSR) in `Program.cs`:

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Viblog.Frontend._Imports).Assembly);
```

### How It Works
1. All blog pages (Index, Posts, Post detail) are prerendered on the server
2. Initial HTML is sent to the browser (fast first paint)
3. Blazor runtime loads and hydrates the page for interactivity
4. Navigation within the app is handled client-side (SPA experience)

### Benefits
- **Fast initial load**: Users see content immediately
- **SEO-friendly**: Search engines can crawl full content
- **Progressive enhancement**: Works even if JavaScript fails
- **Better UX**: Instant perceived performance

## Content Types

### Long-Form Posts
Posts with `Content` or `Markdown` populated:
- **Title**: Linked to detail page
- **Read More**: Shows "Read more ?" link
- **Detail Page**: Full content displayed at `/post/{slug}`

### Short-Only Posts
Posts with only `Short` populated (no Content or Markdown):
- **Title**: Not linked (plain text)
- **Read More**: No link shown
- **Detail Page**: Not accessible (returns 404)

## View Count Tracking

### Implementation
View counts are tracked asynchronously when a post is viewed:

```csharp
_ = Task.Run(async () =>
{
    try
    {
        await BlogPostDetailFacade.IncrementViewCountAsync(_post.Id, _post.Id);
    }
    catch
    {
        // Silently fail - view count is not critical
    }
});
```

### Design Decisions
- **Fire-and-forget**: Doesn't block page rendering
- **Silent failures**: View count errors don't affect user experience
- **Async**: No performance impact on page load

## Styling

### Post Detail Styles
All styles are defined in `Viblog\wwwroot\blog.scss`:

**Key Classes:**
- `.post-detail`: Main article container (max-width: 800px)
- `.post-header`: Header section with title and metadata
- `.post-detail-title`: Large title (2.5rem)
- `.post-detail-meta`: Metadata row with separators
- `.post-content`: Rich content area with typography styles
- `.post-featured-image`: Featured image container
- `.post-categories`: Category badges
- `.post-tags-section`: Tags in footer
- `.post-navigation`: Back to posts link

### Content Typography
The `.post-content` class provides rich formatting:
- **Headings**: H2 (2rem), H3 (1.5rem), H4 (1.25rem)
- **Paragraphs**: 1.125rem with 1.8 line-height
- **Code**: Inline and block code styling
- **Lists**: Proper spacing and indentation
- **Blockquotes**: Left border with background
- **Links**: Underlined with hover effects
- **Images**: Responsive with border radius

### Responsive Design
Mobile optimizations (< 640.98px):
- Title reduced to 1.75rem
- Content font size reduced to 1rem
- Metadata stacked vertically
- Padding adjusted for smaller screens

## Error Handling

### States
1. **Loading**: Shows "Loading post..." message
2. **Post Not Found**: Friendly 404 with link to posts list
3. **Error**: Generic error message with retry option
4. **Success**: Full post content displayed

### User Experience
- Clear error messages
- Navigation options from error states
- No blank screens or exceptions

## Usage Examples

### Linking to a Post
```razor
<a href="/post/@post.Slug">@post.Title</a>
```

### PostCard with Long-Form Content
```razor
<PostCard Post="post" />
<!-- Renders:
- Linked title
- "Read more ?" link
-->
```

### PostCard with Short-Only Content
```razor
<PostCard Post="post" />
<!-- Renders:
- Plain text title (no link)
- No "Read more" link
-->
```

### Without Read More Link
```razor
<PostCard Post="post" ShowReadMore="false" />
```

## Testing Considerations

### Test Scenarios
1. **Post with full content**: Verify detail page loads correctly
2. **Short-only post**: Verify no link in PostCard
3. **Invalid slug**: Verify 404 page
4. **View count**: Verify increment (doesn't block rendering)
5. **Static prerendering**: Verify HTML is rendered server-side
6. **Navigation**: Verify back link works
7. **Responsive**: Verify mobile layout
8. **Featured image**: Verify image loads correctly
9. **Tags and categories**: Verify display
10. **Markdown rendering**: Verify HTML conversion

### Mock Data Requirements
- BlogPost with all fields populated
- BlogPost with only Short field
- Various slug formats
- Posts with/without featured images
- Posts with different tag/category counts

## Future Enhancements

Potential additions:
- **Related posts**: Show similar content at bottom
- **Social sharing**: Share buttons for Twitter, LinkedIn, etc.
- **Print styling**: Optimized print CSS
- **Table of contents**: Auto-generated from headings
- **Reading progress**: Scroll indicator
- **Comments section**: Integration with comment system
- **Syntax highlighting**: For code blocks in markdown
- **Estimated read time**: Display more prominently
- **Previous/Next**: Navigation between posts
- **Breadcrumbs**: Category ? Post navigation
- **Schema.org markup**: Enhanced SEO with structured data

## Related Files

### Core Implementation
- `Viblog\Frontend\Pages\Post.razor`
- `Viblog\Shared\Facades\IBlogPostDetailFacade.cs`
- `Viblog\Shared\Facades\BlogPostDetailFacade.cs`
- `Viblog\Frontend\Components\PostCard.razor`

### Styling
- `Viblog\wwwroot\blog.scss` (post detail styles)
- `Viblog\wwwroot\styles\_variables.scss` (variables)

### Data Layer
- `Viblog\Shared\Data\Entities\BlogPost.cs`
- `Viblog\Shared\Data\Repositories\IBlogPostRepository.cs`
- `Viblog\Shared\Data\Repositories\BlogPostRepository.cs`

### Configuration
- `Viblog\Program.cs` (prerendering setup)
- `Viblog\Shared\Services\ServiceExtensions.cs` (facade registration)

## Best Practices

### When Creating Posts
1. Always provide a unique, SEO-friendly slug
2. Populate `Content` or `Markdown` for long-form posts
3. Use `Short` for excerpt in listings
4. Add relevant tags and categories
5. Include a featured image when appropriate
6. Set `MetaDescription` for SEO

### When Displaying Posts
1. Use PostCard component for consistency
2. Check `HasLongFormContent` before linking
3. Handle loading and error states
4. Track views asynchronously (non-blocking)
5. Render content with `@((MarkupString)_post.Content)`
6. Always sanitize content if user-generated

### Performance
1. Static prerender for fast first load
2. Async view count (fire-and-forget)
3. Lazy load images if many in content
4. Cache frequently accessed posts (future)
5. CDN for featured images (future)
