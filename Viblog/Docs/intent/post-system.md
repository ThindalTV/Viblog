# Viblog Post System: Complete Guide

## Overview

The Viblog post system is the core feature of the blogging platform, providing comprehensive content management capabilities with a focus on flexibility, performance, and SEO optimization. This document covers both the user-facing features and the technical implementation.

## Table of Contents

1. [Post Lifecycle](#post-lifecycle)
2. [Post Structure](#post-structure)
3. [Creating and Editing Posts](#creating-and-editing-posts)
4. [Content Features](#content-features)
5. [Organization and Discovery](#organization-and-discovery)
6. [SEO and Social Sharing](#seo-and-social-sharing)
7. [Technical Implementation](#technical-implementation)
8. [API Reference](#api-reference)

---

## Post Lifecycle

### States

Posts in Viblog can exist in one of two primary states:

#### 1. Draft
- **IsPublished**: `false`
- Not visible on the public blog
- Accessible only through admin interface
- Can have a future or past `PublishedAt` date
- Allows iterative editing without affecting live content

#### 2. Published
- **IsPublished**: `true`
- Visible on public blog (if `PublishedAt` ? current time)
- Included in RSS/Atom feeds
- Indexed for search
- Can be featured on homepage

### Workflow

```
???????????     ???????????     ?????????????
? Create  ???????  Draft  ??????? Published ?
???????????     ???????????     ?????????????
                     ?                 ?
                     ???????????????????
                        Can transition
                          both ways
```

**Typical Workflow:**
1. **Create** new post in admin interface
2. **Save as Draft** for iterative editing
3. **Preview** to check formatting and appearance
4. **Publish** when ready (immediately or scheduled)
5. **Update** published posts as needed
6. **Unpublish** if content needs revision

### Soft Delete

Posts are never hard-deleted from the database:
- **IsDeleted**: `true` marks posts as deleted
- **DeletedAt**: Timestamp of deletion
- Deleted posts excluded from all public queries
- Can be restored through admin interface (future feature)
- Maintains data integrity and audit trail

---

## Post Structure

### Core Content

#### Title
- **Required**: Yes
- **Type**: String
- **Purpose**: Main heading of the post
- **Constraints**: Should be concise and descriptive
- **SEO Impact**: Primary heading (H1), critical for search rankings

#### Slug
- **Required**: Yes
- **Type**: String (URL-safe)
- **Purpose**: URL-friendly identifier
- **Example**: `"getting-started-with-blazor"` from `"Getting Started with Blazor"`
- **Constraints**: Unique, lowercase, hyphens for spaces
- **SEO Impact**: Appears in URL, affects search rankings
- **Auto-generation**: Can be generated from title (future feature)

#### Short
- **Required**: No (but recommended)
- **Type**: String
- **Purpose**: Brief excerpt or summary
- **Usage**: 
  - Post listing pages
  - Social media sharing
  - RSS/Atom feeds
  - Search results
- **Length**: 150-300 characters recommended
- **SEO Impact**: Often used as meta description

#### Markdown
- **Required**: Yes (for full posts)
- **Type**: Markdown text
- **Purpose**: Raw content in Markdown format
- **Features**:
  - Standard Markdown syntax
  - Code blocks with syntax highlighting
  - Images and media embedding
  - Links and references
  - Tables and lists
- **Storage**: Stored alongside rendered HTML

#### Content
- **Required**: No (auto-generated from Markdown)
- **Type**: HTML string
- **Purpose**: Rendered HTML from Markdown
- **Generation**: Automatic on save/update
- **Security**: Sanitized to prevent XSS attacks

### Visual Assets

#### Featured Image
- **FeaturedImageUrl**: URL to the image
- **FeaturedImageAlt**: Alt text for accessibility
- **Purpose**: 
  - Main visual for post
  - Used in post listings
  - Social media sharing preview
  - SEO image signal
- **Recommendations**:
  - 1200x630px for optimal social sharing
  - Descriptive alt text for accessibility
  - Use Azure Blob Storage for hosting

#### Media URLs
- **Type**: List of strings
- **Purpose**: Additional images, videos, or media
- **Storage**: Links to Azure Blob Storage
- **Future**: Gallery components, video embeds

### Author Information

#### AuthorId
- **Type**: String (GUID)
- **Purpose**: Unique identifier for author
- **Usage**: Multi-author support (future)

#### AuthorName
- **Type**: String
- **Purpose**: Display name
- **Denormalization**: Stored with post for performance
- **Update**: Must be manually synced if author name changes

### Publication Metadata

#### PublishedAt
- **Type**: DateTimeOffset (UTC)
- **Default**: Current time
- **Purpose**: Publication timestamp
- **Features**:
  - Scheduled publishing (future posts)
  - Historical dating (backdated posts)
  - Sorting posts by date
  - Archive organization

#### IsPublished
- **Type**: Boolean
- **Default**: `false`
- **Purpose**: Publication state flag
- **Logic**: Post is public if `IsPublished == true` AND `PublishedAt <= Now`

#### IsFeatured
- **Type**: Boolean
- **Default**: `false`
- **Purpose**: Mark exceptional content
- **Usage**:
  - Homepage featured section
  - Special prominence in listings
  - Editorial curation

### Engagement Features

#### AllowComments
- **Type**: Boolean
- **Default**: `true`
- **Purpose**: Enable/disable comments per post
- **Use Cases**:
  - Disable for announcements
  - Control controversial topics
  - Limit spam on older posts

#### ViewCount
- **Type**: Integer
- **Default**: 0
- **Purpose**: Track popularity
- **Usage**: Analytics, trending posts
- **Privacy**: Server-side tracking only

#### CommentCount
- **Type**: Integer
- **Computed**: From `Comments.Count`
- **Purpose**: Display engagement level
- **Performance**: Denormalized for quick access

### Organization

#### Tags
- **Type**: List of strings
- **Purpose**: Free-form topic markers
- **Example**: `["blazor", "csharp", "web-development"]`
- **Features**:
  - Tag cloud generation
  - Tag-based filtering
  - Related post suggestions
- **Best Practices**: 3-7 tags per post

#### CategoryIds & CategoryNames
- **CategoryIds**: List of category GUIDs
- **CategoryNames**: List of category display names (denormalized)
- **Purpose**: Hierarchical organization
- **Relationship**: Many-to-many with Category entity
- **Usage**:
  - Navigation menu
  - Category archive pages
  - Content organization

### SEO Metadata

#### MetaDescription
- **Type**: String (nullable)
- **Purpose**: Search engine description
- **Length**: 150-160 characters recommended
- **Fallback**: Uses `Short` if not provided
- **SEO Impact**: High - appears in search results

#### MetaKeywords
- **Type**: String (nullable)
- **Purpose**: Legacy SEO keywords
- **Relevance**: Limited modern SEO value
- **Recommendation**: Use tags instead

#### ReadingTimeMinutes
- **Type**: Integer
- **Calculation**: Auto-computed from word count
- **Purpose**: User experience metric
- **Display**: "5 min read" badges
- **Formula**: ~200-250 words per minute

### Audit Fields (from BaseEntity)

#### Id
- **Type**: String (GUID)
- **Purpose**: Unique identifier
- **Auto-generation**: On creation

#### PartitionKey
- **Type**: String
- **Purpose**: CosmosDB partitioning
- **Strategy**: Based on publication year/month
- **Auto-generation**: From `PublishedAt`

#### CreatedAt
- **Type**: DateTimeOffset
- **Auto-set**: On creation
- **Purpose**: Audit trail

#### UpdatedAt
- **Type**: DateTimeOffset
- **Auto-update**: On any change
- **Purpose**: Last modified tracking

#### IsDeleted / DeletedAt
- **Purpose**: Soft delete support
- **See**: [Soft Delete](#soft-delete)

---

## Creating and Editing Posts

### Admin Interface

#### Creating a New Post

1. **Navigate** to `/admin/posts`
2. **Click** "New Post" button
3. **Fill** required fields:
   - Title
   - Slug
   - Author Name
4. **Optionally add**:
   - Short description
   - Markdown content
   - Featured image
   - Tags and categories
5. **Save as Draft** or **Publish**

#### Editing an Existing Post

1. **Navigate** to `/admin/posts`
2. **Click** edit icon on the post
3. **Modify** fields as needed
4. **Save** changes
   - Draft posts remain drafts
   - Published posts update immediately
5. **Change publication state** if needed

#### Editor Features

**Markdown Editor:**
- Syntax highlighting for Markdown
- Live preview (future feature)
- Toolbar with formatting shortcuts
- Auto-save drafts (future feature)

**Metadata Panel:**
- Publication date/time picker
- Featured toggle
- Comments enabled toggle
- Tag management
- Category selection

**Image Upload:**
- Drag-and-drop support (future)
- Azure Blob Storage integration (future)
- Automatic image optimization (future)

### Validation Rules

**Required Fields:**
- Title: Must not be empty
- Slug: Must not be empty, must be unique
- AuthorName: Must not be empty

**Recommended Fields:**
- Short: For better post discovery
- FeaturedImageUrl: For visual appeal
- MetaDescription: For SEO
- Tags: For organization (3-7 recommended)

**Constraints:**
- Slug must be URL-safe (lowercase, hyphens, no special characters)
- PublishedAt should be realistic (not too far in future)
- Tags should be consistent (check existing tags first)

---

## Content Features

### Markdown Support

Viblog uses standard Markdown with these features:

#### Headings
```markdown
# H1 Heading
## H2 Heading
### H3 Heading
```

#### Emphasis
```markdown
*italic* or _italic_
**bold** or __bold__
***bold italic***
~~strikethrough~~
```

#### Lists
```markdown
- Unordered list item
- Another item
  - Nested item

1. Ordered list item
2. Another item
   1. Nested item
```

#### Links and Images
```markdown
[Link text](https://example.com)
![Alt text](https://example.com/image.jpg)
```

#### Code Blocks
````markdown
```csharp
public class Example
{
    public string Property { get; set; }
}
```
````

#### Blockquotes
```markdown
> This is a blockquote
> 
> Multiple paragraphs
```

#### Tables
```markdown
| Column 1 | Column 2 |
|----------|----------|
| Data 1   | Data 2   |
```

### Content Processing

**On Save/Update:**
1. Markdown is parsed and validated
2. HTML is generated and sanitized
3. Search index is built (lowercase, normalized)
4. Reading time is calculated
5. Partition key is updated

**Security:**
- HTML sanitization prevents XSS
- Image URLs validated
- External links optionally opened in new tab
- Embedded content sandboxed

---

## Organization and Discovery

### Categories

**Purpose**: Hierarchical content organization

**Features:**
- Posts can belong to multiple categories
- Category pages list all posts in that category
- Breadcrumb navigation
- SEO-friendly category URLs

**Example Structure:**
```
Technology
??? Web Development
?   ??? Frontend
?   ??? Backend
??? Mobile Development
    ??? iOS
    ??? Android
```

**URL Pattern**: `/category/{category-slug}`

### Tags

**Purpose**: Free-form topic markers

**Features:**
- Flexible, non-hierarchical
- Tag cloud visualization
- Tag-based filtering
- Cross-category discovery

**Best Practices:**
- Use 3-7 tags per post
- Prefer specific over generic tags
- Check existing tags before creating new ones
- Use lowercase for consistency

**URL Pattern**: `/tag/{tag-name}`

### Search

**Search Index:**
- Auto-generated on save/update
- Includes: title, short, content, tags, categories
- Normalized (lowercase, no special characters)
- Full-text search capability

**Search Features:**
- **Single term**: Matches anywhere in content
- **Multiple terms**: All terms must match
- **Title search**: Search only in titles
- **Published filter**: Only published posts (default)

**Performance:**
- CosmosDB full-text search
- Paginated results
- Sorted by relevance (future) or date

**URL Pattern**: `/search?q={query}`

### Archive

**Date-based Organization:**
- Monthly archives: `/archive/2024/03`
- Yearly archives: `/archive/2024`
- Chronological post listing

**Use Cases:**
- Historical browsing
- Temporal context
- Content audit
- Nostalgia browsing

---

## SEO and Social Sharing

### Meta Tags

**Generated for Each Post:**
```html
<title>{Post Title} - {Site Name}</title>
<meta name="description" content="{MetaDescription or Short}">
<meta name="keywords" content="{Tags joined}">
<link rel="canonical" href="{Post URL}">
```

### Open Graph (Facebook, LinkedIn)

```html
<meta property="og:type" content="article">
<meta property="og:title" content="{Post Title}">
<meta property="og:description" content="{Short}">
<meta property="og:image" content="{FeaturedImageUrl}">
<meta property="og:url" content="{Post URL}">
<meta property="article:published_time" content="{PublishedAt}">
<meta property="article:author" content="{AuthorName}">
<meta property="article:tag" content="{Tag1}">
```

### Twitter Cards

```html
<meta name="twitter:card" content="summary_large_image">
<meta name="twitter:title" content="{Post Title}">
<meta name="twitter:description" content="{Short}">
<meta name="twitter:image" content="{FeaturedImageUrl}">
<meta name="twitter:creator" content="{@Author}">
```

### Structured Data (JSON-LD)

```json
{
  "@context": "https://schema.org",
  "@type": "BlogPosting",
  "headline": "{Post Title}",
  "image": "{FeaturedImageUrl}",
  "datePublished": "{PublishedAt}",
  "dateModified": "{UpdatedAt}",
  "author": {
    "@type": "Person",
    "name": "{AuthorName}"
  },
  "publisher": {
    "@type": "Organization",
    "name": "{Site Name}",
    "logo": "{Site Logo}"
  },
  "description": "{Short}",
  "mainEntityOfPage": {
    "@type": "WebPage",
    "@id": "{Post URL}"
  }
}
```

### RSS Feed

**Endpoint**: `/feed/rss`

**Included Fields:**
- Title, Link, Description (Short)
- Publication date
- Categories and tags
- Author information
- Full content (HTML)

### Atom Feed

**Endpoint**: `/feed/atom`

**Similar to RSS with Atom-specific features:**
- More detailed author information
- Updated timestamps
- Content type specification
- Alternate links

### Sitemap

**Endpoint**: `/sitemap.xml`

**Post Inclusion:**
- All published posts
- Priority: 0.7 (higher for featured)
- Change frequency: Weekly
- Last modified: UpdatedAt timestamp

---

## Technical Implementation

### Architecture

The post system follows the **Display-Facade-Repository** pattern:

```
???????????????????
?   Blazor Pages  ?  (Display Layer - Minimal Logic)
???????????????????
         ?
         ?
???????????????????
?     Facades     ?  (Business Logic Layer)
?  - PostsList    ?
?  - PostDetail   ?
?  - PostsAdmin   ?
???????????????????
         ?
         ?
???????????????????
?   Repositories  ?  (Data Access Layer)
? - BlogPostRepo  ?
???????????????????
         ?
         ?
???????????????????
?   CosmosDB      ?  (Data Storage)
???????????????????
```

### Entity Model

**Class**: `BlogPost : BaseEntity`
**Location**: `Viblog.Shared.Data.Entities`

**Key Properties**:
See [Post Structure](#post-structure) for complete list.

**Partition Strategy**:
```csharp
public void UpdatePartitionKey()
{
    PartitionKey = IsPublished 
        ? PublishedAt.ToString("yyyy-MM")  // e.g., "2024-03"
        : "drafts";
}
```

**Benefits**:
- Published posts partitioned by year/month
- Efficient date-range queries
- Drafts isolated for admin performance
- Balanced partition sizes

### Repository Interface

**Interface**: `IBlogPostRepository : IRepository<BlogPost>`
**Implementation**: `BlogPostRepository`
**Location**: `Viblog.Shared.Data.Repositories`

**Specialized Methods**:

```csharp
// Get published posts with pagination
Task<PagedResult<BlogPost>> GetPublishedPostsAsync(
    PagingParameters pagingParameters,
    CancellationToken cancellationToken = default);

// Get posts by category
Task<PagedResult<BlogPost>> GetPostsByCategoryAsync(
    string categoryId,
    PagingParameters pagingParameters,
    bool publishedOnly = true,
    CancellationToken cancellationToken = default);

// Get posts by tag
Task<PagedResult<BlogPost>> GetPostsByTagAsync(
    string tag,
    PagingParameters pagingParameters,
    bool publishedOnly = true,
    CancellationToken cancellationToken = default);

// Get featured posts
Task<PagedResult<BlogPost>> GetFeaturedPostsAsync(
    PagingParameters pagingParameters,
    bool publishedOnly = true,
    CancellationToken cancellationToken = default);

// Get post by slug (unique URL identifier)
Task<BlogPost?> GetBySlugAsync(
    string slug,
    bool publishedOnly = true,
    CancellationToken cancellationToken = default);

// Get post by ID without partition key (slower, for admin)
Task<BlogPost?> GetByIdWithoutPartitionKeyAsync(
    string id,
    CancellationToken cancellationToken = default);

// Get posts by author
Task<PagedResult<BlogPost>> GetPostsByAuthorAsync(
    string authorId,
    PagingParameters pagingParameters,
    bool publishedOnly = true,
    CancellationToken cancellationToken = default);

// Get archive grouped by month
Task<List<MonthlyArchive>> GetArchiveAsync(
    CancellationToken cancellationToken = default);

// Get all unique tags
Task<List<string>> GetAllTagsAsync(
    CancellationToken cancellationToken = default);

// Increment view count
Task IncrementViewCountAsync(
    string id,
    string partitionKey,
    CancellationToken cancellationToken = default);
```

### Facades

#### Frontend Facades

**BlogPostListFacade**
- **Purpose**: Blog post listing pages
- **Methods**: `GetPaginatedPostsAsync()`
- **Features**: Published posts only, sorted by date

**BlogPostDetailFacade**
- **Purpose**: Individual post pages
- **Methods**: `GetPostBySlugAsync()`, `IncrementViewCountAsync()`
- **Features**: Full post with comments, view tracking

**CategoryPostsFacade**
- **Purpose**: Category archive pages
- **Methods**: `GetPostsByCategoryAsync()`
- **Features**: Category filtering, pagination

**TagPostsFacade**
- **Purpose**: Tag archive pages
- **Methods**: `GetPostsByTagAsync()`
- **Features**: Tag filtering, pagination

**BlogSearchFacade**
- **Purpose**: Search results
- **Methods**: `SearchAsync()`, `SearchByTitleAsync()`
- **Features**: Full-text search, relevance sorting

**FrontPageFacade**
- **Purpose**: Homepage
- **Methods**: `GetFeaturedPostsAsync()`, `GetRecentPostsAsync()`
- **Features**: Featured content, recent posts

**ArchiveFacade**
- **Purpose**: Date-based archives
- **Methods**: `GetArchiveAsync()`, `GetPostsByMonthAsync()`
- **Features**: Monthly grouping, chronological listing

#### Admin Facades

**PostsAdminFacade**
- **Purpose**: Post management interface
- **Methods**: 
  - `GetPostsAsync()` - All posts with filters
  - `GetPostByIdAsync()` - Single post for editing
  - `CreatePostAsync()` - Create new post
  - `UpdatePostAsync()` - Update existing post
  - `DeletePostAsync()` - Soft delete post
  - `PublishPostAsync()` - Publish draft
  - `UnpublishPostAsync()` - Revert to draft
- **Features**: Full CRUD, draft management, bulk operations

### Search Service

**Interface**: `IBlogSearchService`
**Implementation**: `BlogSearchService`
**Location**: `Viblog.Shared.Services`

**Search Index**:
```csharp
public string SearchIndex { get; private set; } = string.Empty;

public void UpdateSearchIndex()
{
    var indexParts = new[]
    {
        Title,
        Short,
        Markdown,
        string.Join(" ", Tags),
        string.Join(" ", CategoryNames),
        AuthorName
    };
    
    SearchIndex = string.Join(" ", indexParts)
        .ToLowerInvariant()
        .Trim();
}
```

**Search Methods**:
```csharp
// Single term search
Task<PagedResult<BlogPost>> SearchAsync(
    string searchTerm,
    PagingParameters pagingParameters,
    bool publishedOnly = true,
    CancellationToken cancellationToken = default);

// Title-only search
Task<PagedResult<BlogPost>> SearchByTitleAsync(
    string titleTerm,
    PagingParameters pagingParameters,
    bool publishedOnly = true,
    CancellationToken cancellationToken = default);

// Multiple terms (AND logic)
Task<PagedResult<BlogPost>> SearchMultipleTermsAsync(
    string[] searchTerms,
    PagingParameters pagingParameters,
    bool publishedOnly = true,
    CancellationToken cancellationToken = default);
```

### Data Flow Examples

#### Creating a Post

```csharp
// 1. User fills BlogPostModel in admin UI
var model = new BlogPostModel
{
    Title = "Getting Started with Blazor",
    Slug = "getting-started-with-blazor",
    Short = "Learn the basics of Blazor...",
    Markdown = "# Introduction\n\nBlazor is...",
    AuthorName = "Eric Johansson",
    Tags = new List<string> { "blazor", "csharp", "web" },
    IsPublished = false // Save as draft
};

// 2. Admin facade converts to entity
var post = new BlogPost
{
    Title = model.Title,
    Slug = model.Slug,
    // ... other properties mapped
};

// 3. Repository saves to CosmosDB
await _repository.AddAsync(post);
await _repository.SaveChangesAsync();
// Auto-triggers: UpdatePartitionKey(), UpdateSearchIndex()
```

#### Displaying a Post List

```csharp
// 1. Frontend page requests posts
var pagingParams = new PagingParameters 
{ 
    PageNumber = 1, 
    PageSize = 10 
};

// 2. Facade queries repository
var result = await _facade.GetPaginatedPostsAsync(pagingParams);

// 3. Page renders results
@foreach (var post in result.Items)
{
    <PostCard Post="@post" />
}

<Pagination PagedResult="@result" />
```

#### Searching Posts

```csharp
// 1. User enters search term
var searchTerm = "blazor components";

// 2. Search service processes query
var results = await _searchService.SearchAsync(
    searchTerm, 
    pagingParams, 
    publishedOnly: true);

// 3. Results displayed with highlighting (future)
@foreach (var post in results.Items)
{
    <SearchResultItem Post="@post" Query="@searchTerm" />
}
```

### Performance Optimizations

#### Paging
- **Mandatory**: All list queries use `PagingParameters`
- **Default**: 10 items per page
- **Maximum**: 100 items per page
- **Benefits**: Reduced memory, faster queries, better UX

#### Denormalization
- **CategoryNames**: Avoid join queries
- **AuthorName**: Fast display without user lookup
- **CommentCount**: Quick engagement metrics
- **Trade-off**: Update complexity vs. query performance

#### Partition Strategy
- **Published posts**: Year-month partitions (`"2024-03"`)
- **Drafts**: Single partition (`"drafts"`)
- **Benefits**: Efficient date-range queries, balanced partitions

#### Search Index
- **Pre-computed**: On save, not on query
- **Lowercase**: Case-insensitive search
- **Normalized**: Consistent matching
- **Trade-off**: Storage space vs. query speed

#### Caching (Future)
- **Output caching**: Static pages (homepage, category pages)
- **Distributed cache**: Post entities (Redis)
- **Cache invalidation**: On post update/publish
- **Benefits**: Reduced database load, faster responses

### Extension Points

#### Custom Post Types

Create derived classes for specialized content:

```csharp
public class VideoPost : BlogPost
{
    public string VideoUrl { get; set; }
    public int VideoDurationSeconds { get; set; }
    public string VideoTranscript { get; set; }
}

public class GalleryPost : BlogPost
{
    public List<GalleryImage> Images { get; set; }
    public string GalleryLayout { get; set; } // "grid", "masonry", "slider"
}
```

#### Custom Facades

Add specialized business logic:

```csharp
public interface IRelatedPostsFacade
{
    Task<List<BlogPost>> GetRelatedPostsAsync(
        string postId, 
        int count = 5);
}

public class RelatedPostsFacade : IRelatedPostsFacade
{
    // Implementation using tag/category similarity
}
```

#### Custom Repositories

Extend repository with domain-specific queries:

```csharp
public interface IBlogPostRepository : IRepository<BlogPost>
{
    Task<List<BlogPost>> GetTrendingPostsAsync(
        int days = 7, 
        int count = 10);
    
    Task<Dictionary<string, int>> GetTagCloudAsync(
        int minCount = 1);
}
```

---

## API Reference

### Query Parameters

#### Pagination
- `page`: Page number (1-based, default: 1)
- `pageSize`: Items per page (1-100, default: 10)

#### Filtering
- `published`: `true`/`false` - Filter by publication state
- `featured`: `true`/`false` - Filter featured posts
- `category`: Category ID or slug
- `tag`: Tag name
- `author`: Author ID
- `year`: Publication year (archive)
- `month`: Publication month (archive)

#### Sorting
- `sort`: Field name (`publishedAt`, `title`, `viewCount`, etc.)
- `order`: `asc` or `desc` (default: `desc`)

#### Search
- `q`: Search query
- `title`: Search only titles
- `terms[]`: Multiple search terms (AND logic)

### Response Format

**PagedResult<BlogPost>**:
```json
{
  "items": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "title": "Getting Started with Blazor",
      "slug": "getting-started-with-blazor",
      "short": "Learn the basics...",
      "featuredImageUrl": "https://...",
      "authorName": "Eric Johansson",
      "publishedAt": "2024-03-15T10:00:00Z",
      "tags": ["blazor", "csharp"],
      "categoryNames": ["Web Development"],
      "viewCount": 42,
      "commentCount": 5,
      "readingTimeMinutes": 5
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### Common Usage Patterns

#### Get Latest Posts

```csharp
var recent = await _repository.GetPublishedPostsAsync(
    new PagingParameters { PageNumber = 1, PageSize = 5 });
```

#### Get Post by URL Slug

```csharp
var post = await _repository.GetBySlugAsync("getting-started-with-blazor");
```

#### Search Posts

```csharp
var results = await _searchService.SearchAsync(
    "blazor components",
    new PagingParameters { PageNumber = 1, PageSize = 20 });
```

#### Get Posts by Tag

```csharp
var tagged = await _repository.GetPostsByTagAsync(
    "blazor",
    new PagingParameters { PageNumber = 1, PageSize = 10 });
```

#### Get Featured Posts

```csharp
var featured = await _repository.GetFeaturedPostsAsync(
    new PagingParameters { PageNumber = 1, PageSize = 3 });
```

---

## Best Practices

### Content Creation

1. **Write compelling titles**: Clear, specific, benefit-driven
2. **Craft strong summaries**: 150-300 characters, hook readers
3. **Use meaningful slugs**: Descriptive, keyword-rich URLs
4. **Add featured images**: 1200x630px, descriptive alt text
5. **Tag appropriately**: 3-7 relevant tags per post
6. **Categorize wisely**: 1-3 categories per post
7. **Optimize meta descriptions**: Unique, engaging, 150-160 characters
8. **Structure content**: Use headings, lists, code blocks
9. **Include media**: Images, diagrams, code snippets
10. **Preview before publishing**: Check formatting and layout

### SEO Optimization

1. **Unique titles and descriptions** for every post
2. **Keyword-rich slugs** without keyword stuffing
3. **Internal linking** to related posts
4. **External links** to authoritative sources
5. **Alt text** for all images
6. **Structured headings** (H1 ? H6 hierarchy)
7. **Mobile-friendly** content (short paragraphs)
8. **Fast load times** (optimized images)
9. **Fresh content** (regular updates)
10. **Social sharing** optimization

### Performance

1. **Use pagination** for all lists (never load all posts)
2. **Optimize images** before upload (compression, sizing)
3. **Leverage caching** (output caching for public pages)
4. **Denormalize judiciously** (category names, author info)
5. **Index search fields** (searchIndex pre-computed)
6. **Batch operations** (bulk updates, batch saves)
7. **Async/await** consistently (no blocking calls)
8. **Monitor metrics** (view counts, load times)

### Security

1. **Sanitize HTML** output (prevent XSS)
2. **Validate input** (title length, slug format)
3. **Authenticate admin** (secure post management)
4. **Authorize operations** (only authors can edit own posts - future)
5. **Rate limit** (prevent spam, abuse)
6. **Audit changes** (track who changed what, when)
7. **Backup data** (regular CosmosDB backups)
8. **Encrypt connections** (HTTPS only)

---

## Troubleshooting

### Common Issues

**Post not visible on public site**
- ? Check `IsPublished == true`
- ? Verify `PublishedAt <= current time`
- ? Confirm `IsDeleted == false`
- ? Check slug matches URL

**Search not finding post**
- ? Ensure `SearchIndex` was updated (save post again)
- ? Verify search term matches content
- ? Check `IsPublished == true` for public search
- ? Try different search terms (title, tags)

**Images not displaying**
- ? Verify image URL is accessible
- ? Check HTTPS vs HTTP protocol
- ? Confirm CORS settings for Blob Storage
- ? Validate image file format

**Slow query performance**
- ? Use pagination (never query without limits)
- ? Check partition key usage
- ? Monitor CosmosDB RU consumption
- ? Consider adding indexes (future)

**Categories not showing**
- ? Ensure `CategoryIds` and `CategoryNames` are synced
- ? Verify category exists in database
- ? Check many-to-many relationship

---

## Future Enhancements

### Planned Features

- [ ] Auto-generate slugs from titles
- [ ] Live Markdown preview in editor
- [ ] Auto-save drafts (every 30 seconds)
- [ ] Revision history and rollback
- [ ] Scheduled publishing (cron job)
- [ ] Multi-author workflow (draft ? review ? publish)
- [ ] Rich media embedding (YouTube, Twitter, CodePen)
- [ ] Image gallery components
- [ ] Related posts suggestions (ML-based)
- [ ] Content recommendations
- [ ] A/B testing for titles
- [ ] Analytics dashboard (views, engagement)
- [ ] Export to PDF/ePub
- [ ] Newsletter integration
- [ ] Webhooks for post events
- [ ] GraphQL API

### Under Consideration

- [ ] Post templates (tutorial, announcement, review)
- [ ] Custom fields (extensible metadata)
- [ ] Post series/collections
- [ ] Co-authoring (multiple authors per post)
- [ ] Translation management (multi-language)
- [ ] Content approval workflow
- [ ] Plagiarism detection
- [ ] AI-powered content suggestions
- [ ] Voice dictation for posts
- [ ] Mobile admin app

---

## Summary

The Viblog post system provides a comprehensive, performant, and SEO-optimized blogging platform with:

- **Flexible content management** with drafts and publishing
- **Rich organization** through categories and tags
- **Powerful search** with full-text indexing
- **SEO optimization** with meta tags and structured data
- **Social sharing** via Open Graph and Twitter Cards
- **Performance** through paging, denormalization, and partitioning
- **Extensibility** via facades, repositories, and custom entities
- **Clean architecture** following Display-Facade-Repository pattern

Whether you're creating content, managing posts, or extending functionality, the post system offers the tools and patterns needed for a professional blogging experience.

For technical support or feature requests, please refer to the main [Viblog Architecture Guide](./general.md).
