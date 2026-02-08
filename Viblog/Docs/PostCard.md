# PostCard Component

## Overview
The `PostCard` component is a reusable Blazor component that displays a blog post in a consistent card format. It's used on both the front page and the posts list page to maintain a uniform appearance across the blog. The component intelligently handles both long-form posts (with detail pages) and short-only posts (without detail pages).

## Purpose
- Provide consistent blog post presentation across the application
- Reduce code duplication between Index.razor and Posts.razor
- Allow customization of display options (featured badge, tags, etc.)
- Conditionally link to detail pages based on content availability
- Follow the DRY (Don't Repeat Yourself) principle

## Location
`Viblog\Frontend\Components\PostCard.razor`

## Parameters

### Required Parameters

#### Post
```csharp
[Parameter, EditorRequired]
public BlogPost Post { get; set; } = default!;
```
**Description:** The blog post entity to display.  
**Type:** `BlogPost`  
**Required:** Yes

### Optional Parameters

#### ShowFeaturedBadge
```csharp
[Parameter]
public bool ShowFeaturedBadge { get; set; } = true;
```
**Description:** Whether to show the "Featured" badge if the post is featured.  
**Type:** `bool`  
**Default:** `true`

#### ShowTags
```csharp
[Parameter]
public bool ShowTags { get; set; } = true;
```
**Description:** Whether to display the post's tags.  
**Type:** `bool`  
**Default:** `true`

#### MaxTagsToShow
```csharp
[Parameter]
public int MaxTagsToShow { get; set; } = 3;
```
**Description:** Maximum number of tags to display.  
**Type:** `int`  
**Default:** `3`

#### ShowReadMore
```csharp
[Parameter]
public bool ShowReadMore { get; set; } = true;
```
**Description:** Whether to show the "Read more" link for posts with long-form content.  
**Type:** `bool`  
**Default:** `true`

## Content Detection Logic

### HasLongFormContent
```csharp
private bool HasLongFormContent => 
    !string.IsNullOrWhiteSpace(Post.Content) || 
    !string.IsNullOrWhiteSpace(Post.Markdown);
```

The component automatically detects whether a post has long-form content by checking if either the `Content` or `Markdown` property is populated.

### Conditional Rendering Behavior

#### Posts WITH Long-Form Content
- **Title**: Clickable link to `/post/{slug}`
- **Read More**: Shows "Read more ?" link (if `ShowReadMore` is true)
- **User Experience**: Click title or "Read more" to view full post

#### Posts WITHOUT Long-Form Content (Short-Only)
- **Title**: Plain text (not clickable)
- **Read More**: Hidden
- **User Experience**: All content visible in the card; no detail page

## Rendered Content

The component displays the following information:
1. **Featured Badge** (conditional): Shows "Featured" if `Post.IsFeatured` is true and `ShowFeaturedBadge` is true
2. **Post Title**: Linked to the full post if `HasLongFormContent`, otherwise plain text
3. **Post Metadata**:
   - Author name
   - Published date (formatted as "MMMM dd, yyyy")
   - Reading time (if > 0)
4. **Post Excerpt**: Short description of the post
5. **Tags** (conditional): Up to `MaxTagsToShow` tags, displayed if `ShowTags` is true
6. **Read More Link** (conditional): Shows if `HasLongFormContent` and `ShowReadMore` are both true

## Usage Examples

### Basic Usage (Default Settings)
```razor
<PostCard Post="post" />
```
**Behavior:**
- Long-form posts: Linked title + "Read more" link
- Short-only posts: Plain title + no "Read more" link

### Without Featured Badge
```razor
<PostCard Post="post" ShowFeaturedBadge="false" />
```

### Without Tags
```razor
<PostCard Post="post" ShowTags="false" />
```

### Show More Tags
```razor
<PostCard Post="post" MaxTagsToShow="5" />
```

### Without Read More Link
```razor
<PostCard Post="post" ShowReadMore="false" />
```
Useful if you want to display the card but handle navigation elsewhere.

### Minimal Display
```razor
<PostCard Post="post" 
          ShowFeaturedBadge="false" 
          ShowTags="false"
          ShowReadMore="false" />
```

## Usage in Pages

### Index.razor (Front Page)
```razor
@foreach (var post in _posts)
{
    <PostCard Post="post" />
}
```

### Posts.razor (Posts List)
```razor
@foreach (var post in _pagedResult.Items)
{
    <PostCard Post="post" />
}
```

## Styling

The component uses existing CSS classes from `blog.scss`:
- `.blog-post` - Main article container
- `.featured-badge` - Featured post indicator
- `.post-title` - Post title styling
- `.post-meta` - Metadata container
- `.post-author` - Author name
- `.post-date` - Publication date
- `.reading-time` - Reading time estimate
- `.post-excerpt` - Post description
- `.post-tags` - Tag container
- `.tag` - Individual tag styling
- `.post-read-more` - Read more link container
- `.read-more-link` - Read more link styling

All styles are defined in `Viblog\wwwroot\blog.scss` and apply automatically to the component.

## Design Decisions

### Why a Component?
1. **Reusability**: Same post card used on multiple pages
2. **Maintainability**: Changes to post display only need to be made in one place
3. **Consistency**: Ensures uniform appearance across the blog
4. **Testability**: Component can be tested independently

### Why Parameterized Options?
- Different pages may have different requirements
- Front page might show featured posts prominently
- Search results might hide tags to save space
- Archive pages might show more tags

### Why EditorRequired Attribute?
- Ensures developers don't forget to provide the Post parameter
- IDE support with compiler warnings
- Prevents runtime errors from null posts

### Why Conditional Linking?
1. **User Experience**: Don't promise content that doesn't exist
2. **Accessibility**: Non-interactive elements shouldn't look clickable
3. **Flexibility**: Support both micro-posts and long-form articles
4. **SEO**: Avoid 404s from posts without detail pages

### Why Fire Link Logic in Component?
- Keeps parent pages simple
- Centralizes content detection logic
- Single source of truth for linking behavior
- Easier to change linking rules globally

## Content Strategy

### When to Use Long-Form vs Short-Only

#### Long-Form Posts (with Content/Markdown)
- **Use for**: Tutorials, guides, in-depth analysis, stories
- **Card behavior**: Linked title, "Read more" button
- **Detail page**: Full content at `/post/{slug}`
- **Examples**: "Getting Started with Blazor", "Database Design Patterns"

#### Short-Only Posts (no Content/Markdown)
- **Use for**: Announcements, quick tips, link sharing, micro-blogs
- **Card behavior**: Plain title, no "Read more"
- **Detail page**: None (404 if accessed directly)
- **Examples**: "New blog post published!", "Quick tip: Use async/await"

## Future Enhancements

Potential additions to this component:
- `ShowExcerpt` parameter to optionally hide the excerpt
- `ShowAuthor` parameter to hide author in certain contexts
- `ShowDate` parameter for flexibility
- `CssClass` parameter for additional styling options
- `OnPostClick` event callback for analytics
- Support for post thumbnails/images
- Category badge alongside featured badge
- Skeleton loading state
- Share buttons for social media
- Bookmark/save functionality

## Best Practices

When using this component:
1. Always provide a valid `BlogPost` object (required parameter)
2. Consider the context when setting optional parameters
3. Use default settings unless there's a specific reason to change them
4. Keep the post card simple and focused on essential information
5. Let the component handle formatting - don't duplicate logic in parent pages
6. Ensure posts have either full content OR make it clear they're short-only
7. Don't manually link to detail pages - let the component decide

## Related Files
- `Viblog\Frontend\Pages\Index.razor` - Uses PostCard for front page
- `Viblog\Frontend\Pages\Posts.razor` - Uses PostCard for posts list
- `Viblog\Frontend\Pages\Post.razor` - Detail page for long-form posts
- `Viblog\wwwroot\blog.scss` - Styling for post cards
- `Viblog\Shared\Data\Entities\BlogPost.cs` - Blog post entity definition
- `Viblog\Shared\Facades\IBlogPostDetailFacade.cs` - Facade for post detail
- `Viblog\Docs\BlogPostDetail.md` - Documentation for post detail feature
