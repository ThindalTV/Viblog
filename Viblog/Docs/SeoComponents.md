# SEO Components Architecture

## Overview

The SEO implementation uses a component-based architecture with specialized components for different page types. This keeps pages clean and SEO logic encapsulated in reusable components.

## Component Hierarchy

```
SeoHead (base component)
??? MetaTags (meta tags, Open Graph, Twitter Cards)
??? HeadContent slot (for StructuredData components)

Specialized SEO Components (wrap SeoHead):
??? HomeSeoHead - Homepage SEO
??? PostSeoHead - Blog post SEO
??? CollectionSeoHead - Category/Tag/Archive pages SEO

StructuredData (standalone component)
??? Generates JSON-LD schemas (BlogPost, WebSite, Organization, etc.)

Supporting Models:
??? TwitterMetadata - DTO for Twitter Card label/data pairs
```

## Components

### 1. `SeoHead.razor` - Base Component

The foundation component that wraps `MetaTags` and provides a slot for additional head content.

**Usage:**
```razor
<SeoHead Title="My Page" Description="..." CanonicalUrl="..."
         TwitterMetadataItems="@(new List<TwitterMetadata> { new("Label", "Value") })">
    <StructuredData Type="StructuredDataType.WebSite" />
</SeoHead>
```

### 2. `HomeSeoHead.razor` - Homepage

Specialized component for the homepage with WebSite and Organization schemas.

**Usage:**
```razor
<HomeSeoHead />
```

**Optional Parameters:**
- `Title` - Override site tagline
- `Description` - Override site description
- `CanonicalUrl` - Override base URL
- `ImageUrl` - Override default image

**Example:**
```razor
@page "/"
@using Viblog.Frontend.Components

<HomeSeoHead />

<section>
    <!-- Your content -->
</section>
```

### 3. `PostSeoHead.razor` - Blog Posts

Specialized component for blog post pages with BlogPosting and Breadcrumb schemas. Automatically generates Twitter metadata from post properties.

**Required Parameters:**
- `Post` - The BlogPost entity
- `CanonicalUrl` - Full URL to the post

**Optional Parameters:**
- `BreadcrumbItems` - Navigation breadcrumbs

**Automatic Twitter Metadata:**
- Reading time (if available)
- Additional metadata can be added by extending `GetTwitterMetadata()` method

**Example:**
```razor
@page "/post/{year:int}/{slug}"
@using Viblog.Frontend.Components
@using Viblog.Shared.Data.Entities

<PostSeoHead Post="@_post" 
            CanonicalUrl="@GetCanonicalUrl()" 
            BreadcrumbItems="@_breadcrumbItems" />

<article>
    <!-- Your post content -->
</article>
```

### 4. `CollectionSeoHead.razor` - Listing Pages

Specialized component for category, tag, archive, and other listing pages with CollectionPage schema.

**Required Parameters:**
- `Title` - Page title (e.g., "Category: Technology")

**Optional Parameters:**
- `Description` - Page description
- `CanonicalUrl` - Override automatic URL detection
- `ImageUrl` - Custom image
- `BreadcrumbItems` - Navigation breadcrumbs

**Example:**
```razor
@page "/category/{categoryId}"
@using Viblog.Frontend.Components

<CollectionSeoHead 
    Title="@($"Category: {CategoryId}")" 
    Description="@($"Posts in the {CategoryId} category")"
    BreadcrumbItems="@_breadcrumbItems" />

<section>
    <!-- Your posts list -->
</section>
```

### 5. `StructuredData.razor` - JSON-LD Generator

Generates Schema.org JSON-LD structured data. Can be used standalone or within SEO components.

**Parameters:**
- `Type` - Enum: `BlogPost`, `WebSite`, `Organization`, `Breadcrumb`, `CollectionPage`
- `Post` - For BlogPost type
- `Url` - For BlogPost and CollectionPage types
- `BreadcrumbItems` - For Breadcrumb type
- `PageTitle` - For CollectionPage type
- `PageDescription` - For CollectionPage type

**Example (standalone):**
```razor
<HeadContent>
    <StructuredData Type="StructuredDataType.BlogPost" Post="@_post" Url="@url" />
</HeadContent>
```

## Models

### `TwitterMetadata` - Twitter Card Metadata DTO

A simple DTO for Twitter Card label/data pairs. Twitter supports up to 2 metadata items per card.

**Properties:**
- `Label` - The label for the data (e.g., "Reading time", "Category")
- `Data` - The data value (e.g., "5 min read", "Technology")

**Common Label Constants:**

The `TwitterMetadata.Labels` static class provides common label constants:

- `ReadingTime` - "Reading time"
- `Category` - "Category"
- `Author` - "Written by"
- `Views` - "Views"
- `PublishedDate` - "Published"
- `UpdatedDate` - "Updated"
- `WordCount` - "Word count"
- `Comments` - "Comments"
- `Series` - "Series"
- `Level` - "Level"
- `Tags` - "Tags"
- `Location` - "Location"

**Usage with constants:**
```csharp
var twitterData = new List<TwitterMetadata>
{
    new TwitterMetadata(TwitterMetadata.Labels.ReadingTime, "5 min read"),
    new TwitterMetadata(TwitterMetadata.Labels.Category, "Technology")
};
```

**Usage with custom labels:**
```csharp
var twitterData = new List<TwitterMetadata>
{
    new TwitterMetadata("Custom Label", "Custom Value"),
    new TwitterMetadata(TwitterMetadata.Labels.ReadingTime, "5 min read")
};
```

**Rendered as:**
```html
<meta name="twitter:label1" content="Reading time" />
<meta name="twitter:data1" content="5 min read" />
<meta name="twitter:label2" content="Category" />
<meta name="twitter:data2" content="Technology" />
```

## Benefits

? **Clean Pages** - Minimal SEO code in page components  
? **Type Safety** - Strongly-typed parameters  
? **Reusability** - Use the same component across multiple pages  
? **Maintainability** - SEO changes in one place  
? **Consistency** - Standardized SEO implementation  
? **Testability** - Components can be tested independently  

## Migration Guide

### Before (Old Approach)
```razor
@inject StructuredDataHelper Helper
@inject IOptions<SiteMetadata> SiteMetadata

<MetaTags Title="..." Description="..." ... />
<HeadContent>
    <script type="application/ld+json">
        @((MarkupString)Helper.GenerateBlogPostingSchema(...))
    </script>
</HeadContent>
```

### After (Component-Based)
```razor
<PostSeoHead Post="@_post" CanonicalUrl="@url" />
```

## Page Type Recommendations

| Page Type | Component |
|-----------|-----------|
| Homepage | `HomeSeoHead` |
| Blog Post | `PostSeoHead` |
| Category List | `CollectionSeoHead` |
| Tag List | `CollectionSeoHead` |
| Archive | `CollectionSeoHead` |
| Static Pages | `SeoHead` (base) |
| Custom Pages | `SeoHead` + `StructuredData` |

## Configuration

All components use `SiteMetadata` from `appsettings.json`:

```json
{
  "SiteMetadata": {
    "SiteName": "Your Blog Name",
    "BaseUrl": "https://yourdomain.com",
    "DefaultDescription": "Your blog description",
    "Tagline": "Your tagline",
    "DefaultImageUrl": "https://yourdomain.com/og-image.jpg"
  }
}
