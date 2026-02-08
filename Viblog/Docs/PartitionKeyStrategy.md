# Blog Post Partition Key Strategy

## Overview

Blog posts in Viblog use a **year-based partition key strategy** for optimal performance with Azure CosmosDB.

## Partition Key Format

The partition key for blog posts is based on the **publication year**:

- **Published posts**: The year from `PublishedAt` (e.g., "2025", "2024", "2023")
- **Draft posts**: The string "draft" for unpublished posts

## URL Structure

Blog post URLs include the year for SEO and partitioning alignment:

```
/post/{year}/{slug}
```

**Examples:**
- `/post/2025/getting-started-blazor-server-net10`
- `/post/2024/building-scalable-apps-cosmosdb-efcore`

## Automatic Partition Key Management

The `BlogPost` entity automatically manages its partition key:

### Helper Methods

```csharp
// Get the publication year as a string
string year = post.GetPublicationYear(); // Returns "2025" or "draft"

// Update the partition key based on current PublishedAt value
post.UpdatePartitionKey();
```

### Automatic Updates

The `BlogPostRepository` automatically sets partition keys when:
- Adding new posts (`AddAsync`, `AddRangeAsync`)
- Seeding the database

## Changing Publication Dates

### Important: Partition Key Immutability

?? **CosmosDB partition keys are immutable**. When a blog post's publication year changes, the post must be deleted from the old partition and recreated in the new partition.

### Using UpdatePublicationDateAsync

The repository provides a method to handle this safely:

```csharp
var updatedPost = await blogPostRepository.UpdatePublicationDateAsync(
    postId: post.Id,
    currentPartitionKey: post.PartitionKey, // e.g., "2024"
    newPublishedAt: new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero),
    cancellationToken: cancellationToken
);
```

This method:
1. Checks if the year changed
2. If the year is the same, performs a normal update
3. If the year changed, deletes the post from the old partition and recreates it in the new partition
4. Preserves all post data including comments, view counts, etc.

### Example Scenarios

#### Scenario 1: Same Year (Normal Update)
```csharp
// Post published on 2025-01-10, updating to 2025-03-15
// Partition key stays "2025" - normal update
await UpdatePublicationDateAsync(postId, "2025", new DateTimeOffset(2025, 3, 15, ...));
```

#### Scenario 2: Year Change (Delete & Recreate)
```csharp
// Post published on 2024-12-20, updating to 2025-01-05
// Partition key changes from "2024" to "2025" - delete & recreate
await UpdatePublicationDateAsync(postId, "2024", new DateTimeOffset(2025, 1, 5, ...));
```

#### Scenario 3: Publishing a Draft
```csharp
// Draft post being published for the first time
// Partition key changes from "draft" to "2025" - delete & recreate
await UpdatePublicationDateAsync(postId, "draft", new DateTimeOffset(2025, 1, 10, ...));
```

## Benefits of Year-Based Partitioning

1. **Efficient Queries**: Queries for posts by year are highly efficient
2. **Natural Distribution**: Posts are naturally distributed across years
3. **Archive-Friendly**: Easy to query historical content by year
4. **URL SEO**: Year in URL improves SEO and content organization
5. **Scalability**: Prevents hot partitions as posts are distributed by time

## Query Performance

### Efficient Queries (Use Partition Key)
```csharp
// Query posts from a specific year - uses partition key
var posts2025 = await context.BlogPosts
    .Where(p => p.PartitionKey == "2025")
    .ToListAsync();

// Query specific post by year and slug - uses partition key
var post = await context.BlogPosts
    .FirstOrDefaultAsync(p => p.PartitionKey == "2025" && p.Slug == "my-post");
```

### Cross-Partition Queries (Higher RU Cost)
```csharp
// Query all published posts across all years - cross-partition query
var allPosts = await context.BlogPosts
    .Where(p => p.IsPublished)
    .ToListAsync();
```

## Best Practices

1. **Always set PublishedAt before saving**: Ensure `PublishedAt` is set before calling repository methods
2. **Use UpdatePublicationDateAsync for date changes**: Never manually change `PublishedAt` and `PartitionKey` separately
3. **Include year in URLs**: Always use the `/post/{year}/{slug}` format for post links
4. **Cache partition key**: When querying, cache the partition key to avoid additional lookups
5. **Handle drafts consistently**: Use "draft" partition for all unpublished posts

## Migration Considerations

If migrating from a different partition key strategy:

1. Read all existing posts
2. Update their `PublishedAt` values if needed
3. Call `UpdatePartitionKey()` on each post
4. Delete old documents
5. Add updated documents with new partition keys

## Code Examples

### Creating a New Post
```csharp
var post = new BlogPost
{
    Title = "My New Post",
    Slug = "my-new-post",
    PublishedAt = DateTimeOffset.UtcNow,
    IsPublished = true,
    // ... other properties
};

// Partition key is automatically set by repository
await blogPostRepository.AddAsync(post);
```

### Updating a Post's Publication Date
```csharp
var post = await blogPostRepository.GetByIdAsync(postId, partitionKey);
var newDate = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

// Use the safe update method
var updatedPost = await blogPostRepository.UpdatePublicationDateAsync(
    postId,
    post.PartitionKey,
    newDate
);
```

### Seeding Posts
```csharp
var post = new BlogPost
{
    // ... properties
    PublishedAt = DateTimeOffset.UtcNow,
};

// Call UpdatePartitionKey before adding to context
post.UpdatePartitionKey();
await context.BlogPosts.AddAsync(post);
```
