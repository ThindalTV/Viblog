# CosmosDB Infrastructure Setup

This document describes the CosmosDB infrastructure implementation for the Viblog blogging platform.

## Overview

The application uses **Azure CosmosDB** with **Entity Framework Core** for data persistence. The repository pattern provides a clean abstraction layer for data access operations with built-in paging and sorting support.

## Architecture

### Components

1. **BaseEntity** - Abstract base class for all entities
2. **IRepository<TEntity>** - Generic repository interface
3. **Repository<TEntity>** - Generic repository implementation
4. **ApplicationDbContext** - EF Core DbContext configured for CosmosDB
5. **DataServiceExtensions** - DI registration helper
6. **PagingParameters** - Paging configuration
7. **PagedResult<T>** - Paginated result wrapper

### Entity Inheritance

All domain entities should inherit from `BaseEntity` which provides:
- `Id` - Unique identifier (string/GUID)
- `PartitionKey` - CosmosDB partition key
- `CreatedAt` - Creation timestamp
- `UpdatedAt` - Last update timestamp
- `IsDeleted` - Soft delete flag
- `DeletedAt` - Deletion timestamp

### Repository Pattern

The generic repository provides paged CRUD operations:
- `GetByIdAsync` - Retrieve by ID and partition key
- `GetAllAsync` - Get all entities with paging and optional sorting
- `FindAsync` - Query with predicate, paging, and optional sorting
- `FirstOrDefaultAsync` - Get first match
- `AddAsync` / `AddRangeAsync` - Insert operations
- `UpdateAsync` - Update operation
- `DeleteAsync` - Soft/hard delete
- `AnyAsync` - Existence check
- `CountAsync` - Count query
- `SaveChangesAsync` - Persist changes

**Note:** All methods that return multiple entities use paging to prevent loading excessive data.

## Configuration

### Development (Local Cosmos DB Emulator)

```json
{
  "ConnectionStrings": {
    "CosmosConnection": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  },
  "CosmosDb": {
    "DatabaseName": "ViblogDb"
  }
}
```

### Production (Azure CosmosDB)

Use **Azure Key Vault** or **User Secrets** for production credentials:

```json
{
  "ConnectionStrings": {
    "CosmosConnection": "AccountEndpoint=https://{account-name}.documents.azure.com:443/;AccountKey={key};"
  },
  "CosmosDb": {
    "DatabaseName": "ViblogDb"
  }
}
```

## Usage Examples

### Creating a Domain Entity

```csharp
using Viblog.Shared.Data.Entities;

namespace Viblog.Shared.Data.Entities;

public class BlogPost : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTimeOffset PublishedAt { get; set; }
    
    // Override partition key if needed
    // public new string PartitionKey 
    // { 
    //     get => AuthorId; 
    //     set { } 
    // }
}
```

### Creating a Specific Repository

```csharp
using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;
using Viblog.Shared.Data.Repositories;

namespace Viblog.Shared.Data.Repositories;

public interface IBlogPostRepository : IRepository<BlogPost>
{
    Task<PagedResult<BlogPost>> GetByAuthorAsync(
        string authorId, 
        PagingParameters paging, 
        CancellationToken cancellationToken = default);
    
    Task<PagedResult<BlogPost>> GetPublishedPostsAsync(
        PagingParameters paging, 
        CancellationToken cancellationToken = default);
}

public class BlogPostRepository : Repository<BlogPost>, IBlogPostRepository
{
    public BlogPostRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<BlogPost>> GetByAuthorAsync(
        string authorId, 
        PagingParameters paging, 
        CancellationToken cancellationToken = default)
    {
        // Get posts by author, sorted by publish date descending
        return await FindAsync(
            p => p.AuthorId == authorId,
            paging,
            orderBy: p => p.PublishedAt,
            ascending: false,
            cancellationToken: cancellationToken);
    }

    public async Task<PagedResult<BlogPost>> GetPublishedPostsAsync(
        PagingParameters paging, 
        CancellationToken cancellationToken = default)
    {
        // Get all published posts, sorted by publish date descending
        return await FindAsync(
            p => p.PublishedAt <= DateTimeOffset.UtcNow,
            paging,
            orderBy: p => p.PublishedAt,
            ascending: false,
            cancellationToken: cancellationToken);
    }
}
```

### Registering Custom Repositories

Update `DataServiceExtensions.cs`:

```csharp
public static IServiceCollection AddRepositories(this IServiceCollection services)
{
    services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    
    // Register specific repositories
    services.AddScoped<IBlogPostRepository, BlogPostRepository>();
    services.AddScoped<ICategoryRepository, CategoryRepository>();
    
    return services;
}
```

### Using Repositories in Services

```csharp
using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;
using Viblog.Shared.Data.Repositories;

public class BlogPostService
{
    private readonly IBlogPostRepository _blogPostRepository;

    public BlogPostService(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository;
    }

    public async Task<BlogPost> CreatePostAsync(BlogPost post, CancellationToken cancellationToken = default)
    {
        post.PartitionKey = post.AuthorId;
        await _blogPostRepository.AddAsync(post, cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);
        return post;
    }

    public async Task<PagedResult<BlogPost>> GetAuthorPostsAsync(
        string authorId, 
        int pageNumber = 1, 
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagingParameters(pageNumber, pageSize);
        return await _blogPostRepository.GetByAuthorAsync(authorId, paging, cancellationToken);
    }

    public async Task<PagedResult<BlogPost>> GetRecentPostsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagingParameters(pageNumber, pageSize);
        
        // Get all posts sorted by creation date, newest first
        return await _blogPostRepository.GetAllAsync(
            paging,
            orderBy: p => p.CreatedAt,
            ascending: false,
            cancellationToken: cancellationToken);
    }
}
```

### Paging and Sorting Examples

#### Basic Paging

```csharp
// Get first page with 10 items
var paging = new PagingParameters(pageNumber: 1, pageSize: 10);
var result = await _repository.GetAllAsync<DateTimeOffset>(paging);

Console.WriteLine($"Page {result.PageNumber} of {result.TotalPages}");
Console.WriteLine($"Total items: {result.TotalCount}");
foreach (var item in result.Items)
{
    // Process items
}
```

#### Paging with Sorting

```csharp
// Get second page sorted by title (ascending)
var paging = new PagingParameters(pageNumber: 2, pageSize: 20);
var result = await _repository.GetAllAsync(
    paging,
    orderBy: p => p.Title,
    ascending: true);

// Get first page sorted by creation date (descending/newest first)
var recentPosts = await _repository.GetAllAsync(
    paging,
    orderBy: p => p.CreatedAt,
    ascending: false);
```

#### Filtering with Paging and Sorting

```csharp
// Find posts with tag, sorted by published date
var paging = new PagingParameters(1, 10);
var taggedPosts = await _repository.FindAsync(
    predicate: p => p.Tags.Contains("blazor"),
    pagingParameters: paging,
    orderBy: p => p.PublishedAt,
    ascending: false);

// Complex filter with sorting
var archivedPosts = await _repository.FindAsync(
    predicate: p => p.CreatedAt < DateTimeOffset.UtcNow.AddYears(-1),
    pagingParameters: new PagingParameters(1, 50),
    orderBy: p => p.CreatedAt,
    ascending: true);
```

#### Working with PagedResult

```csharp
var result = await _repository.GetAllAsync(
    new PagingParameters(1, 10),
    orderBy: p => p.CreatedAt);

// Check if there are more pages
if (result.HasNextPage)
{
    var nextPage = await _repository.GetAllAsync(
        new PagingParameters(result.PageNumber + 1, result.PageSize),
        orderBy: p => p.CreatedAt);
}

// Display pagination info
Console.WriteLine($"Showing {result.Items.Count()} of {result.TotalCount} items");
Console.WriteLine($"Page {result.PageNumber}/{result.TotalPages}");
```

## CosmosDB Containers

The application creates the following containers:

### Identity Containers
- `Users` - User accounts (partitioned by UserId)
- `Roles` - User roles (partitioned by RoleId)
- `UserClaims` - User claims (partitioned by UserId)
- `UserRoles` - User-role mappings (partitioned by UserId)
- `UserLogins` - External logins (partitioned by UserId)
- `RoleClaims` - Role claims (partitioned by RoleId)
- `UserTokens` - User tokens (partitioned by UserId)

### Blog Containers (to be created as needed)
- `BlogPosts` - Blog post content
- `Categories` - Post categories
- `Tags` - Post tags
- `Comments` - User comments
- `Media` - Media metadata

## Features

### Paging

All methods that return multiple entities use `PagedResult<T>` to ensure efficient data retrieval:

```csharp
var paging = new PagingParameters
{
    PageNumber = 1,     // 1-based page number
    PageSize = 20       // Max 100 items per page
};

var result = await _repository.GetAllAsync<DateTimeOffset>(paging);

// Access pagination metadata
Console.WriteLine($"Total items: {result.TotalCount}");
Console.WriteLine($"Total pages: {result.TotalPages}");
Console.WriteLine($"Has previous: {result.HasPreviousPage}");
Console.WriteLine($"Has next: {result.HasNextPage}");
```

### Sorting

Sorting uses type-safe expression predicates:

```csharp
// Sort ascending by title
var ascending = await _repository.GetAllAsync(
    paging,
    orderBy: p => p.Title,
    ascending: true);

// Sort descending by date
var descending = await _repository.GetAllAsync(
    paging,
    orderBy: p => p.CreatedAt,
    ascending: false);

// No sorting (database order)
var unsorted = await _repository.GetAllAsync<object>(paging);
```

### Soft Delete

All entities support soft deletion by default. Use the `includeDeleted` parameter to query deleted items:

```csharp
// Exclude deleted (default)
var active = await _repository.GetAllAsync<DateTimeOffset>(paging);

// Include deleted
var all = await _repository.GetAllAsync<DateTimeOffset>(
    paging, 
    includeDeleted: true);

// Get only deleted items
var deleted = await _repository.FindAsync(
    predicate: p => p.IsDeleted,
    pagingParameters: paging,
    includeDeleted: true);
```

### Automatic Timestamps

The `ApplicationDbContext` automatically updates `CreatedAt` and `UpdatedAt` timestamps during `SaveChanges`.

### Partition Keys

Each entity must have a partition key. By default, entities use their `Id` as the partition key, but this can be overridden for better data distribution.

## Best Practices

1. **Partition Key Selection** - Choose partition keys that:
   - Distribute data evenly
   - Support common query patterns
   - Avoid hot partitions

2. **Use Transactions** - CosmosDB transactions are limited to a single partition key. Design entities accordingly.

3. **Optimize Queries** - Always include the partition key in queries when possible to avoid cross-partition queries.

4. **Paging** - Always use paging parameters to control data volume. Maximum page size is 100 items.

5. **Sorting** - Specify sorting for consistent, predictable results, especially for pagination.

6. **Connection Resilience** - The EF Core Cosmos provider handles retries automatically.

## Testing

For unit testing, mock the `IRepository<TEntity>` interface:

```csharp
using Moq;
using Xunit;
using Viblog.Shared.Data.Common;

public class BlogPostServiceTests
{
    [Fact]
    public async Task GetAuthorPostsAsync_ReturnsPaginatedResults()
    {
        // Arrange
        var mockRepo = new Mock<IBlogPostRepository>();
        var expectedResult = new PagedResult<BlogPost>(
            new List<BlogPost> { new BlogPost { Title = "Test" } },
            totalCount: 1,
            pageNumber: 1,
            pageSize: 10);

        mockRepo
            .Setup(r => r.GetByAuthorAsync(
                "author1", 
                It.IsAny<PagingParameters>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var service = new BlogPostService(mockRepo.Object);

        // Act
        var result = await service.GetAuthorPostsAsync("author1");

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }
}
```

## Migration from SQL Server

The original SQL Server configuration has been replaced with CosmosDB. Key differences:

1. **No relational constraints** - Foreign keys are handled at the application level
2. **Document-based** - Nested objects are stored as JSON
3. **Partition keys required** - All queries should include partition keys when possible
4. **No migrations** - Schema changes are handled through EF Core model configuration
5. **Paging required** - All multi-item queries use paging to prevent over-fetching

## Resources

- [CosmosDB .NET SDK](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/sdk-dotnet-v3)
- [EF Core Cosmos Provider](https://learn.microsoft.com/en-us/ef/core/providers/cosmos/)
- [CosmosDB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator)
- [CosmosDB Pagination](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/query/pagination)
