# ASP.NET Core Identity with CosmosDB Configuration

## Overview
This document explains how ASP.NET Core Identity is configured to work with Azure Cosmos DB in the Vilog application.

## The Problem

ASP.NET Core Identity entities use `ConcurrencyStamp` properties for optimistic concurrency control. However, CosmosDB uses `_etag` for concurrency, which is incompatible with the default Identity configuration.

Additionally, ASP.NET Core Identity defines indexes on certain properties (like `NormalizedUserName` and `NormalizedRoleName`), but **CosmosDB does not support EF Core index definitions**. CosmosDB manages its own indexing internally.

**Errors Without Configuration:**
```
InvalidOperationException: The entity type 'IdentityRole' has property 'ConcurrencyStamp' 
configured as a concurrency token, but only a property mapped to '_etag' is supported 
as a concurrency token. Consider using 'PropertyBuilder.IsETagConcurrency'.
```

```
InvalidOperationException: The entity type 'IdentityRole' has an index defined over 
properties 'NormalizedName'. The Azure Cosmos DB provider for EF Core currently does 
not support index definitions.
```

## The Solution

1. Configure all Identity entities in `ApplicationDbContext.OnModelCreating()` to use CosmosDB's `_etag` mechanism through the `IsETagConcurrency()` method
2. **Remove all EF Core index definitions** since CosmosDB handles indexing automatically

## Implementation

### ApplicationUser
Location: `Vilog\Shared\Data\ApplicationUser.cs`

```csharp
public class ApplicationUser : IdentityUser
{
    public string PartitionKey
    {
        get => Id;
        set { }
    }
    
    public string? DisplayName { get; set; }
}
```

### ApplicationDbContext
Location: `Vilog\Shared\Data\ApplicationDbContext.cs`

The DbContext configures three main areas:
1. **Remove Identity Indexes** - CosmosDB doesn't support EF Core index definitions
2. **Identity Entities** - ASP.NET Core Identity tables
3. **Blog Entities** - Application-specific entities

### Index Removal (Critical!)

```csharp
private static void RemoveIdentityIndexes(ModelBuilder builder)
{
    // Remove all indexes from all entities (CosmosDB doesn't support EF Core index definitions)
    foreach (var entityType in builder.Model.GetEntityTypes())
    {
        var indexes = entityType.GetIndexes().ToList();
        foreach (var index in indexes)
        {
            entityType.RemoveIndex(index);
        }
    }
}
```

**Why This is Necessary:**
- ASP.NET Core Identity automatically configures indexes on `NormalizedUserName`, `NormalizedEmail`, `NormalizedName`, etc.
- CosmosDB **does not support** EF Core's index definitions
- CosmosDB automatically indexes all properties by default (configurable via indexing policy)
- We must remove these indexes to avoid runtime errors

## Identity Entity Configuration

### Required Configuration for Each Identity Entity

1. **Container Name** - Each entity type gets its own container
2. **Partition Key** - Defines how data is distributed
3. **Concurrency Token** - Maps `ConcurrencyStamp` to `_etag`

### Configured Entities

#### 1. ApplicationUser
```csharp
modelBuilder.Entity<ApplicationUser>(entity =>
{
    entity.ToContainer("Users");
    entity.HasPartitionKey(u => u.Id);
    entity.Property(u => u.ConcurrencyStamp).IsETagConcurrency();
});
```

**Container:** `Users`  
**Partition Key:** `Id` (user identifier)  
**Purpose:** Stores user accounts and authentication information

#### 2. IdentityRole
```csharp
modelBuilder.Entity<IdentityRole>(entity =>
{
    entity.ToContainer("Roles");
    entity.HasPartitionKey(r => r.Id);
    entity.Property(r => r.ConcurrencyStamp).IsETagConcurrency();
});
```

**Container:** `Roles`  
**Partition Key:** `Id` (role identifier)  
**Purpose:** Stores application roles (e.g., Admin, Editor, User)

#### 3. IdentityUserClaim
```csharp
modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
{
    entity.ToContainer("UserClaims");
    entity.HasPartitionKey(uc => uc.UserId);
});
```

**Container:** `UserClaims`  
**Partition Key:** `UserId`  
**Purpose:** Stores custom claims associated with users

#### 4. IdentityUserRole
```csharp
modelBuilder.Entity<IdentityUserRole<string>>(entity =>
{
    entity.ToContainer("UserRoles");
    entity.HasPartitionKey(ur => ur.UserId);
});
```

**Container:** `UserRoles`  
**Partition Key:** `UserId`  
**Purpose:** Links users to their assigned roles

#### 5. IdentityUserLogin
```csharp
modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
{
    entity.ToContainer("UserLogins");
    entity.HasPartitionKey(ul => ul.UserId);
});
```

**Container:** `UserLogins`  
**Partition Key:** `UserId`  
**Purpose:** Stores external login provider information (Google, Microsoft, etc.)

#### 6. IdentityRoleClaim
```csharp
modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
{
    entity.ToContainer("RoleClaims");
    entity.HasPartitionKey(rc => rc.RoleId);
});
```

**Container:** `RoleClaims`  
**Partition Key:** `RoleId`  
**Purpose:** Stores claims associated with roles

#### 7. IdentityUserToken
```csharp
modelBuilder.Entity<IdentityUserToken<string>>(entity =>
{
    entity.ToContainer("UserTokens");
    entity.HasPartitionKey(ut => ut.UserId);
});
```

**Container:** `UserTokens`  
**Partition Key:** `UserId`  
**Purpose:** Stores authentication tokens (refresh tokens, etc.)

## Blog Entity Configuration

### BlogPost
```csharp
modelBuilder.Entity<Entities.BlogPost>(entity =>
{
    entity.ToContainer("BlogPosts");
    entity.HasPartitionKey(p => p.PartitionKey);
    
    entity.Property(p => p.Tags);
    entity.Property(p => p.CategoryIds);
    entity.Property(p => p.CategoryNames);
    entity.Property(p => p.MediaUrls);
});
```

**Container:** `BlogPosts`  
**Partition Key:** `PartitionKey` (custom property from `BaseEntity`)  
**List Properties:** Tags, CategoryIds, CategoryNames, MediaUrls

## CosmosDB Container Structure

After the application runs and creates the database, you'll see these containers in CosmosDB:

```
VilogDb (Database)
??? Users                 (Identity - User accounts)
??? Roles                 (Identity - Application roles)
??? UserClaims            (Identity - User claims)
??? UserRoles             (Identity - User-role relationships)
??? UserLogins            (Identity - External logins)
??? RoleClaims            (Identity - Role claims)
??? UserTokens            (Identity - Authentication tokens)
??? BlogPosts             (Application - Blog content)
```

## Partition Key Strategy

### Why Partition Keys Matter
CosmosDB uses partition keys to:
- Distribute data across physical partitions
- Route queries efficiently
- Scale horizontally

### Chosen Strategy

**Identity Entities:**
- **Users/Roles:** Partitioned by `Id` (natural key)
- **Related Entities:** Partitioned by `UserId` or `RoleId` (co-location)

**Benefit:** Related data (user + claims + roles) can be queried efficiently within a single partition.

**Blog Entities:**
- **BlogPosts:** Partitioned by `PartitionKey` (flexible strategy)

**Benefit:** Allows custom partitioning strategies (by category, author, date, etc.)

## Concurrency Control

### How _etag Works

1. **Read:** CosmosDB returns current `_etag` value
2. **Update:** Client sends `_etag` with update request
3. **Validation:** CosmosDB rejects update if `_etag` doesn't match
4. **Conflict:** Client receives 412 Precondition Failed

### In Code

```csharp
// EF Core automatically handles _etag
var user = await userManager.FindByIdAsync(userId);
user.Email = "newemail@example.com";
await userManager.UpdateAsync(user); // _etag checked automatically
```

If another process modified the user between read and update, `UpdateAsync` will throw a `DbUpdateConcurrencyException`.

## Database Initialization

### Automatic Creation (Configured ?)
The application automatically creates the database and containers on startup:

```csharp
// In Program.cs
await EnsureDatabaseCreatedAsync(app);

static async Task EnsureDatabaseCreatedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Ensuring CosmosDB database and containers are created...");
        await dbContext.Database.EnsureCreatedAsync();
        logger.LogInformation("CosmosDB database and containers are ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while ensuring the database was created.");
        throw;
    }
}
```

**On First Run:**
1. Application starts
2. Database initialization runs
3. Database `VilogDb` is created (if missing)
4. All 8 containers are created (if missing):
   - Users
   - Roles
   - UserClaims
   - UserRoles
   - UserLogins
   - RoleClaims
   - UserTokens
   - BlogPosts
5. Application continues startup

**Subsequent Runs:**
- Initialization runs but finds existing resources
- No changes are made
- Fast startup continues

### Manual Creation
You can also create the database manually using the CosmosDB Data Explorer at:
```
https://localhost:8081/_explorer/index.html
```

However, with automatic initialization configured, this is not necessary.

## Best Practices

### 1. Don't Mix Container Strategies
? **Good:** Each entity type in its own container  
? **Bad:** Multiple entity types in one container

### 2. Choose Partition Keys Carefully
? **Good:** Partition by frequently queried field  
? **Bad:** Partition by random GUID (poor distribution)

### 3. Co-locate Related Data
? **Good:** UserClaims partitioned by UserId (query with user)  
? **Bad:** UserClaims partitioned by ClaimType (scattered across partitions)

### 4. Use _etag for Concurrency
? **Good:** `.IsETagConcurrency()` on `ConcurrencyStamp`  
? **Bad:** Custom concurrency token implementation

## Troubleshooting

### Error: Index Definitions Not Supported
**Error Message:**
```
InvalidOperationException: The entity type 'IdentityRole' has an index defined over 
properties 'NormalizedName'. The Azure Cosmos DB provider for EF Core currently does 
not support index definitions.
```

**Solution:** Ensure `RemoveIdentityIndexes()` is called in `OnModelCreating()` **after** `base.OnModelCreating(builder)` to remove all indexes added by Identity.

**Root Cause:** ASP.NET Core Identity adds indexes automatically, but CosmosDB doesn't support EF Core index definitions. CosmosDB manages indexing differently through its own indexing policies.

### Error: Container Already Exists
**Solution:** Delete the container in Data Explorer or change container name

### Error: Partition Key Mismatch
**Solution:** Ensure the partition key path matches the configured property

### Error: _etag Concurrency Conflict
**Solution:** Reload the entity and retry the update (optimistic concurrency pattern)

### Error: Invalid Partition Key Value
**Solution:** Ensure the partition key property is set before saving

## Testing Identity with CosmosDB

### Create a Test User
```csharp
var user = new ApplicationUser
{
    UserName = "testuser",
    Email = "test@example.com"
};

var result = await userManager.CreateAsync(user, "Password123!");
```

### Verify in Data Explorer
1. Open `https://localhost:8081/_explorer/index.html`
2. Navigate to `VilogDb` ? `Users`
3. You should see the created user with:
   - `id`: User's ID
   - `_etag`: CosmosDB's concurrency token
   - Other Identity properties

## Performance Considerations

### Query Optimization
- Always include partition key in queries when possible
- Use `.WithPartitionKey()` for single-partition queries

```csharp
var user = await dbContext.Users
    .WithPartitionKey(userId)
    .FirstOrDefaultAsync(u => u.Id == userId);
```

### Indexing
CosmosDB automatically indexes all properties by default. For production, consider:
- Custom indexing policies (configured at container level, not in EF Core)
- Excluding large properties from index
- Composite indexes for common query patterns

**Important:** Unlike SQL Server, you **cannot** define indexes in EF Core for CosmosDB. All indexing is managed through CosmosDB's indexing policy, which can be configured:
- Through Azure Portal
- Via Azure SDK
- In container creation code

**Default Behavior:** All properties are automatically indexed, which works well for most scenarios.

**Example Custom Indexing Policy** (configured outside EF Core):
```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    {
      "path": "/*"
    }
  ],
  "excludedPaths": [
    {
      "path": "/Content/*"
    },
    {
      "path": "/Markdown/*"
    }
  ]
}
```

## Migration from SQL Server

If migrating from SQL Server:

1. **No Migrations:** CosmosDB doesn't use EF migrations
2. **Container Design:** Plan container structure upfront
3. **Partition Keys:** Choose based on access patterns
4. **Relationships:** CosmosDB doesn't enforce foreign keys
5. **Queries:** Some LINQ operations may not translate

## Additional Resources

- [EF Core CosmosDB Provider](https://learn.microsoft.com/en-us/ef/core/providers/cosmos/)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [CosmosDB Best Practices](https://learn.microsoft.com/en-us/azure/cosmos-db/best-practice-dotnet)
- [Partition Key Selection](https://learn.microsoft.com/en-us/azure/cosmos-db/partitioning-overview)
