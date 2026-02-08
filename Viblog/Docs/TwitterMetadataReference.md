# Twitter Metadata Quick Reference

## Overview

The `TwitterMetadata` class provides a simple way to add label/data pairs to Twitter Cards. Twitter supports a maximum of 2 metadata items per card.

## Common Labels

Use the `TwitterMetadata.Labels` constants for consistency:

| Constant | Label Text | Common Usage Example |
|----------|-----------|---------------------|
| `ReadingTime` | "Reading time" | "5 min read" |
| `Category` | "Category" | "Technology", "Lifestyle" |
| `Author` | "Written by" | "John Doe" |
| `Views` | "Views" | "1,234 views" |
| `PublishedDate` | "Published" | "Jan 15, 2024" |
| `UpdatedDate` | "Updated" | "Updated Jan 20, 2024" |
| `WordCount` | "Word count" | "1,500 words" |
| `Comments` | "Comments" | "23 comments" |
| `Series` | "Series" | "Part 3 of 5" |
| `Level` | "Level" | "Beginner", "Advanced" |
| `Tags` | "Tags" | "C#, Blazor, SEO" |
| `Location` | "Location" | "San Francisco, CA" |

## Usage Examples

### Basic Usage with Constants

```csharp
using Viblog.Frontend.Models;

var metadata = new List<TwitterMetadata>
{
    new(TwitterMetadata.Labels.ReadingTime, "5 min read"),
    new(TwitterMetadata.Labels.Category, "Technology")
};
```

### Blog Post Example

```csharp
private List<TwitterMetadata> GetTwitterMetadata()
{
    var metadata = new List<TwitterMetadata>();

    // Reading time
    if (Post.ReadingTimeMinutes > 0)
    {
        metadata.Add(new(TwitterMetadata.Labels.ReadingTime, 
            $"{Post.ReadingTimeMinutes} min read"));
    }

    // Category (only if metadata list has room)
    if (metadata.Count < 2 && Post.CategoryNames.Any())
    {
        metadata.Add(new(TwitterMetadata.Labels.Category, 
            Post.CategoryNames.First()));
    }

    // Views (only if significant)
    if (metadata.Count < 2 && Post.ViewCount > 100)
    {
        metadata.Add(new(TwitterMetadata.Labels.Views, 
            $"{Post.ViewCount:N0}"));
    }

    return metadata;
}
```

### Custom Labels

You can still use custom labels when the predefined constants don't fit:

```csharp
var metadata = new List<TwitterMetadata>
{
    new("Difficulty", "Intermediate"),
    new("Estimated Time", "45 minutes")
};
```

### Collection Pages Example

```csharp
// Category page
var metadata = new List<TwitterMetadata>
{
    new(TwitterMetadata.Labels.Category, categoryName),
    new("Posts", $"{postCount} posts")
};

// Archive page
var metadata = new List<TwitterMetadata>
{
    new(TwitterMetadata.Labels.PublishedDate, $"{month} {year}"),
    new("Posts", $"{postCount} posts")
};

// Tag page
var metadata = new List<TwitterMetadata>
{
    new(TwitterMetadata.Labels.Tags, tagName),
    new("Posts", $"{postCount} posts")
};
```

## Best Practices

1. **Prioritize Important Information** - Only 2 items are supported, choose wisely
2. **Keep Data Concise** - Short, scannable values work best
3. **Use Consistent Labels** - Prefer constants over custom strings
4. **Check Count** - Ensure you don't exceed 2 items
5. **Conditional Logic** - Only add metadata if the data is meaningful

## Priority Recommendations by Page Type

### Blog Post
1. **Reading time** (if available) - Helps users gauge commitment
2. **Category** or **Views** - Context or social proof

### Collection Pages
1. **Category/Tag name** - What this collection is about
2. **Post count** - How much content is available

### Author Pages
1. **Author name** - Who this is about
2. **Post count** or **Joined date** - Activity level

### Search Results
1. **Search term** - What was searched
2. **Result count** - How many results found

## Rendered Output

Twitter Card metadata is rendered as:

```html
<meta name="twitter:label1" content="Reading time" />
<meta name="twitter:data1" content="5 min read" />
<meta name="twitter:label2" content="Category" />
<meta name="twitter:data2" content="Technology" />
```

This appears in Twitter cards as supplementary information below the main content.
