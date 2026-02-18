# IndexManager Architecture Refactoring

## Problem Statement

The `IndexManager` class was previously tightly coupled to the Filesystem implementation with no abstraction layer. Additionally, cross-cutting concerns like Auditing and Authentication were placed under the Filesystem project, which violated separation of concerns.

### Issues Identified:
1. **No abstraction** - `IndexManager` had no interface, making it impossible to swap implementations
2. **Tight coupling** - Filesystem-specific logic couldn't be replaced for CosmosDB or other storage providers
3. **Inconsistent architecture** - `IAuditLogService` and `IAuthenticationProvider` already existed as abstractions, but `IndexManager` didn't follow the same pattern
4. **Poor organization** - Auditing and Authentication living under Filesystem project instead of being provider-agnostic

## Solution

### 1. Created Shared Interfaces

**Location:** `Viblog.Infrastructure\Shared\Data\Indexing\`

- **`IIndexManager<TEntity>`** - Interface defining index management operations
- **`IndexEntry`** - Record moved to shared infrastructure (previously duplicated in Filesystem project)

```csharp
public interface IIndexManager<TEntity> where TEntity : BaseEntity
{
    Task LoadIndexAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(TEntity entity, string fileName, CancellationToken cancellationToken = default);
    Task RemoveAsync(string id, string partitionKey, CancellationToken cancellationToken = default);
    IndexEntry? FindEntry(string id, string partitionKey);
    IEnumerable<IndexEntry> GetAllEntries();
    Task RebuildIndexAsync(CancellationToken cancellationToken = default);
}
```

### 2. Updated Filesystem Implementation

**Location:** `Viblog.Data\Viblog.Data.Filesystem\Indexing\IndexManager.cs`

- Implements `IIndexManager<TEntity>` interface
- Removed duplicate `IndexEntry` record (now using shared version)
- Refactored `RebuildIndexAsync` to use constructor-injected `_entityDirectory` instead of requiring it as a parameter
- Added `_entityDirectory` field to store the directory path

**Registration:** Updated `FilesystemServiceExtensions.cs`
```csharp
services.AddSingleton(typeof(IIndexManager<>), typeof(IndexManager<>));
```

### 3. Created CosmosDB Implementation

**Location:** `Viblog.Data\Viblog.Data.CosmosDb\Indexing\CosmosDbIndexManager.cs`

- Implements `IIndexManager<TEntity>` interface
- No-op implementation since CosmosDB has native indexing
- Logs warnings if methods are called (for debugging purposes)

**Registration:** Updated `CosmosDbServiceExtensions.cs`
```csharp
services.AddSingleton(typeof(IIndexManager<>), typeof(CosmosDbIndexManager<>));
```

## Architecture Benefits

### 1. **Separation of Concerns**
- Indexing abstractions live in `Viblog.Infrastructure.Shared`
- Implementation details live in their respective data provider projects
- Follows the same pattern as `IAuditLogService` and `IAuthenticationProvider`

### 2. **Dependency Injection**
- `IIndexManager<TEntity>` can be injected via DI
- Easy to mock for unit testing
- Swappable implementations based on configuration

### 3. **Storage Provider Agnostic**
- CosmosDB doesn't need file-based indexing (native indexes)
- Filesystem needs explicit index files
- Both implement the same interface
- Future storage providers (Azure Table Storage, SQL Server, etc.) can implement the interface

### 4. **Consistent with Existing Patterns**
```
Viblog.Infrastructure.Shared
├── Auditing
│   └── IAuditLogService.cs ✓
├── Authentication
│   ├── IAuthenticationProvider.cs ✓
│   └── IUserManagementService.cs ✓
├── Data
│   ├── Indexing
│   │   ├── IIndexManager.cs ✓ NEW
│   │   └── IndexEntry.cs ✓ NEW
│   └── Repositories
│       └── IRepository.cs ✓
```

## Migration Guide

### For Existing Code Using IndexManager

**Before:**
```csharp
var indexManager = new IndexManager<BlogPost>(entityDir, options, logger);
await indexManager.RebuildIndexAsync(entityDir, cancellationToken);
```

**After:**
```csharp
// Injected via DI
private readonly IIndexManager<BlogPost> _indexManager;

public MyService(IIndexManager<BlogPost> indexManager)
{
    _indexManager = indexManager;
}

// entityDirectory no longer needed as parameter
await _indexManager.RebuildIndexAsync(cancellationToken);
```

### For New Implementations

1. Implement `IIndexManager<TEntity>`
2. Register in your service extension class
3. Follow the storage provider's natural indexing mechanism

## Testing Recommendations

### Unit Tests
- Mock `IIndexManager<TEntity>` using your preferred mocking framework
- Test filesystem implementation with in-memory file system
- Test CosmosDB no-op behavior

### Integration Tests
- Verify filesystem index files are created correctly
- Verify CosmosDB queries work without explicit indexing
- Test switching between implementations via configuration

## Next Steps

### Recommended Actions:
1. ✅ **Move Auditing** - `AuditLogService` should follow this pattern (interface in Shared, implementations in providers)
2. ✅ **Move Authentication** - Already has interface, just needs to be consistently applied
3. 📋 **Create IMediaStorageRepository implementations** - Follow this same pattern for media storage
4. 📋 **Repository pattern consistency** - Ensure all repositories follow the Display-Facade-Repository pattern
5. 📋 **Unit test coverage** - Add tests for `IIndexManager` implementations

## Files Changed

### Created:
- `Viblog.Infrastructure\Shared\Data\Indexing\IIndexManager.cs`
- `Viblog.Infrastructure\Shared\Data\Indexing\IndexEntry.cs`
- `Viblog.Data\Viblog.Data.CosmosDb\Indexing\CosmosDbIndexManager.cs`

### Modified:
- `Viblog.Data\Viblog.Data.Filesystem\Indexing\IndexManager.cs`
- `Viblog.Data\Viblog.Data.Filesystem\FilesystemServiceExtensions.cs`
- `Viblog.Data\Viblog.Data.CosmosDb\CosmosDbServiceExtensions.cs`

## Conclusion

The IndexManager now follows proper architectural patterns:
- **Interface-based design** for testability and flexibility
- **Dependency injection** for loose coupling
- **Provider-specific implementations** for appropriate behavior
- **Consistent with existing patterns** in the codebase

This refactoring makes the codebase more maintainable, testable, and extensible for future storage providers.
