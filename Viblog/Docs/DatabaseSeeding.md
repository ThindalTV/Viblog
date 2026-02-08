# Database Seeding

## Overview
The Viblog application includes automatic database seeding functionality that populates CosmosDB with sample blog posts on first run.

## How It Works

### Automatic Seeding
On application startup, after the database and containers are created, the seeding process:

1. **Checks if blog posts exist** - Queries the BlogPosts container
2. **Seeds if empty** - Only adds data if the database is completely empty
3. **Skips if data exists** - If any blog posts exist, seeding is skipped
4. **Logs the process** - All seeding activities are logged

### Seeding Location
- **File:** `Viblog\Shared\Data\Seeders\BlogPostSeeder.cs`
- **Called from:** `Program.cs` during application startup
- **Timing:** After database creation, before app starts handling requests

## What Gets Seeded

### Sample Blog Posts: 15 Total

#### Featured Posts (3)
Posts with `IsFeatured = true` published within the last 30 days:

1. **Getting Started with Blazor Server in .NET 10** (5 days old)
   - Tags: Blazor, .NET 10, Web Development, Server-Side
   - Categories: Blazor, .NET
   - Views: 245

2. **Building Scalable Applications with CosmosDB and EF Core** (12 days old)
   - Tags: CosmosDB, Azure, Entity Framework, Scalability
   - Categories: Azure, Databases
   - Views: 432

3. **Modern CSS Techniques for Responsive Web Design** (18 days old)
   - Tags: CSS, Responsive Design, Web Development, Frontend
   - Categories: Frontend, CSS
   - Views: 389

#### Regular Posts (12)
Recent posts spanning the last 3 months covering topics like:
- Dependency Injection
- Docker for .NET
- Async/Await best practices
- RESTful API design
- Telerik UI for Blazor
- Azure Functions vs AWS Lambda
- SOLID Principles
- GraphQL with .NET
- Blazor Performance
- Unit Testing with xUnit
- Security in ASP.NET Core
- Minimal APIs

### Data Characteristics

**All Posts Include:**
- ? Unique ID and partition key
- ? Title and URL-friendly slug
- ? Short description for listings
- ? Markdown and HTML content
- ? Author information (System Administrator)
- ? Publication dates (distributed over time)
- ? Published status (`IsPublished = true`)
- ? Comment allowance enabled
- ? Realistic view counts
- ? Relevant tags (3-4 per post)
- ? Category assignments (1-2 per post)
- ? SEO metadata
- ? Reading time estimates
- ? Search index auto-generated

**Post Distribution:**
- Featured: 3 posts (20%)
- Regular: 12 posts (80%)
- Date range: Last 103 days
- Total categories: 10+ unique
- Total tags: 30+ unique

## Code Structure

### BlogPostSeeder.cs

```csharp
public static class BlogPostSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Check if posts already exist
        if (context.Set<BlogPost>().Any())
        {
            return; // Skip seeding
        }

        // Create 3 featured posts
        // Create 12 regular posts
        // Update search indexes
        // Save all posts
    }
}
```

### Program.cs Integration

```csharp
var app = builder.Build();

// Ensure database and containers are created
await EnsureDatabaseCreatedAsync(app);

// Seed database with sample data if empty
await SeedDatabaseAsync(app);

app.Run();
```

## Seeding Process Flow

```
Application Starts
    ?
Create Database & Containers
    ?
Check if BlogPosts exist
    ?
    ?? Yes ? Skip seeding
    ?         ?
    ?    Log: "Database already has data"
    ?         ?
    ?    Continue startup
    ?
    ?? No ? Run seeding
              ?
         Create 15 blog posts
              ?
         Generate search indexes
              ?
         Save to CosmosDB
              ?
         Log: "Database seeding completed"
              ?
         Continue startup
```

## Logs

### First Run (Seeding Occurs)
```
info: Program[0]
      Ensuring CosmosDB database and containers are created...
info: Program[0]
      CosmosDB database and containers are ready.
info: Program[0]
      Checking if database seeding is needed...
info: Program[0]
      Database seeding completed.
```

### Subsequent Runs (Seeding Skipped)
```
info: Program[0]
      Ensuring CosmosDB database and containers are created...
info: Program[0]
      CosmosDB database and containers are ready.
info: Program[0]
      Checking if database seeding is needed...
info: Program[0]
      Database seeding completed.
```
Note: Even when skipped, "completed" is logged (seeding check completed).

## Verifying Seeded Data

### Via Data Explorer
1. Open `https://localhost:8081/_explorer/index.html`
2. Navigate to `ViblogDb` ? `BlogPosts`
3. You should see 15 documents

### Via Application
1. Run the application
2. Navigate to the home page
3. You should see:
   - 3 featured posts (from last month)
   - 5 additional latest posts
   - Total: 8 posts displayed (as configured)

### Via Search
Test the search functionality:
```csharp
// Featured posts
var featured = await facade.GetRecentFeaturedPostsAsync(maxPosts: 5);
// Should return 3 posts

// All published posts
var all = await repository.GetPublishedPostsAsync(new PagingParameters(1, 20));
// Should return 15 posts
```

## Customizing Seed Data

### Add More Posts
Edit `BlogPostSeeder.cs` and add to the `posts` list:

```csharp
posts.Add(CreateBlogPost(
    "Your New Post Title",
    "your-new-post-slug",
    "Your short description",
    new[] { "Tag1", "Tag2" },
    new[] { "category1", "category2" },
    now.AddDays(-120),
    viewCount: 100,
    readingTime: 5
));
```

### Change Featured Posts
Modify the featured post section to add/remove featured content:

```csharp
posts.Add(new BlogPost
{
    // ... other properties
    IsFeatured = true, // This makes it featured
    PublishedAt = now.AddDays(-7), // Must be within last 30 days
    // ... other properties
});
```

### Adjust Date Distribution
Change the `AddDays()` values to space posts differently:

```csharp
// More recent posts
publishedAt: now.AddDays(-7) // One week ago

// Older posts
publishedAt: now.AddDays(-365) // One year ago
```

## Resetting the Database

To re-seed with fresh data:

### Option 1: Delete BlogPosts Container
1. Open Data Explorer
2. Delete the `BlogPosts` container
3. Restart the application
4. Container is recreated and re-seeded

### Option 2: Delete Entire Database
1. Open Data Explorer
2. Delete the `ViblogDb` database
3. Restart the application
4. Database, containers, and seed data recreated

### Option 3: Manual Deletion
1. Open Data Explorer
2. Navigate to `BlogPosts` container
3. Delete all documents manually
4. Restart the application
5. Seeder detects empty container and re-seeds

## Error Handling

### Seeding Failures
If seeding fails:
- Error is logged
- Exception is caught
- **Application continues to start**
- No sample data, but app is functional

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while seeding the database.");
    // Don't throw - seeding failure shouldn't prevent app startup
}
```

### Why Seeding Errors Don't Stop Startup
- Seeding is a convenience feature
- Production databases won't use seeding
- App should work without sample data
- Admins can add posts manually

## Production Considerations

### Disable Seeding in Production
Add environment check to `SeedDatabaseAsync`:

```csharp
static async Task SeedDatabaseAsync(WebApplication app)
{
    // Only seed in development
    if (!app.Environment.IsDevelopment())
    {
        return;
    }
    
    // ... rest of seeding code
}
```

### Alternative Approach
Create a separate seeding command:

```csharp
// dotnet run --seed-database
if (args.Contains("--seed-database"))
{
    await SeedDatabaseAsync(app);
    return;
}
```

## Testing

### Unit Test Example
```csharp
[Fact]
public async Task SeedAsync_WithEmptyDatabase_AddsPostsAsync()
{
    // Arrange
    var context = CreateInMemoryContext();
    
    // Act
    await BlogPostSeeder.SeedAsync(context);
    
    // Assert
    var posts = context.Set<BlogPost>().ToList();
    Assert.Equal(15, posts.Count);
    Assert.Equal(3, posts.Count(p => p.IsFeatured));
}

[Fact]
public async Task SeedAsync_WithExistingData_SkipsSeedingAsync()
{
    // Arrange
    var context = CreateInMemoryContext();
    context.Set<BlogPost>().Add(new BlogPost { /* ... */ });
    await context.SaveChangesAsync();
    
    // Act
    await BlogPostSeeder.SeedAsync(context);
    
    // Assert
    var posts = context.Set<BlogPost>().ToList();
    Assert.Equal(1, posts.Count); // Only the one we added
}
```

## Benefits

? **Immediate Visual Feedback** - See the app working with real-looking data  
? **Testing Front Page** - Verify featured posts and latest posts display  
? **Search Testing** - Test search functionality with varied content  
? **Category/Tag Testing** - Verify filtering by categories and tags  
? **No Manual Data Entry** - Saves time during development  
? **Realistic Demo** - Shows the app with production-like content  
? **Idempotent** - Safe to run multiple times  

## Sample Data Quality

The seeded data is designed to:
- **Look realistic** - Professional titles and descriptions
- **Cover diverse topics** - Wide range of development subjects
- **Vary in age** - Distributed over time for realistic sorting
- **Include metadata** - Tags, categories, views, reading time
- **Be searchable** - Search indexes pre-generated
- **Support all features** - Featured posts, pagination, filtering
