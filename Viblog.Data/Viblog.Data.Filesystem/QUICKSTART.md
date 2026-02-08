# Viblog.Data.Filesystem

Filesystem-based storage provider for Viblog, offering a lightweight alternative to CosmosDB and Azure Blob Storage for local development and Docker deployments.

## Features

? **Entity Storage**: JSON-based storage with automatic indexing for fast queries  
? **File Storage**: Direct filesystem storage for media files  
? **Docker-Ready**: Easily configurable with volume mounts  
? **High Performance**: Optimized with in-memory index caching and buffered I/O  
? **Repository Pattern**: Drop-in replacement for CosmosDB repositories  
? **Automatic Cleanup**: Removes empty directories and maintains indexes  

## Quick Start

### 1. Install the Package

Add a project reference to `Viblog.Data.Filesystem`:

```xml
<ItemGroup>
  <ProjectReference Include="..\Viblog.Data\Viblog.Data.Filesystem\Viblog.Data.Filesystem.csproj" />
</ItemGroup>
```

### 2. Configure in appsettings.json

```json
{
  "StorageProvider": "Filesystem",
  "FilesystemStorage": {
    "RootPath": "./data",
    "UseIndexing": true,
    "MaxIndexCacheSize": 1000
  }
}
```

### 3. Register Services

```csharp
using Viblog.Data.Filesystem;

var builder = WebApplication.CreateBuilder(args);

// Register filesystem storage
builder.Services.AddFilesystemDataAccess(builder.Configuration);

var app = builder.Build();
```

### 4. Use in Docker

**docker-compose.yml:**
```yaml
services:
  viblog:
    image: viblog:latest
    volumes:
      - viblog-data:/app/data
    environment:
      - FilesystemStorage__RootPath=/app/data

volumes:
  viblog-data:
```

## Architecture

### Storage Structure

```
./data/
??? entities/          # Entity data (JSON + indexes)
?   ??? BlogPost/
?   ?   ??? 2024/
?   ?   ?   ??? post-id.json
?   ?   ??? _index.json
?   ??? MediaItem/
?       ??? _index.json
??? files/            # Media files
    ??? images/
        ??? 2024/12/
            ??? photo.jpg
```

### Index Files

Each entity type has an `_index.json` file containing:
- Entity ID and partition key
- File location
- Metadata for filtering (created date, deleted status)

This enables fast queries without scanning all files.

## Performance Optimization

### Indexing
- **Enabled by default** - Provides O(1) lookups by ID
- **In-memory cache** - Configurable size for frequently accessed entities
- **Automatic maintenance** - Kept in sync with entity changes

### File I/O
- **80KB buffered streams** - Optimized for typical file sizes
- **Async operations** - Non-blocking I/O throughout
- **Concurrent read support** - Multiple readers with FileShare.Read

### Memory Usage
- Configure `MaxIndexCacheSize` based on available RAM
- Typical memory usage: ~100-200 bytes per cached entity
- Set to `0` to disable caching (uses less memory, slower queries)

## API Reference

### IFilesystemFileStorage

File storage service for media files:

```csharp
public interface IFilesystemFileStorage
{
    Task<string> SaveFileAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Stream?> ReadFileAsync(string relativePath, CancellationToken ct);
    Task<bool> DeleteFileAsync(string relativePath, CancellationToken ct);
    Task<bool> FileExistsAsync(string relativePath);
    Task<long?> GetFileSizeAsync(string relativePath);
    Task<bool> CopyFileAsync(string source, string dest, bool overwrite, CancellationToken ct);
    Task<bool> MoveFileAsync(string source, string dest, bool overwrite, CancellationToken ct);
    Task<List<string>> ListFilesAsync(string directory, string pattern, bool recursive);
    string GetAbsolutePath(string relativePath);
}
```

### Repository Implementations

All standard repository interfaces are implemented:
- `IRepository<TEntity>` - Generic CRUD operations
- `IBlogPostRepository` - Blog-specific queries
- `IMediaMetadataRepository` - Media library operations

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `RootPath` | `./data` | Root storage directory |
| `EntitiesDirectory` | `entities` | Subdirectory for entity JSON files |
| `FilesDirectory` | `files` | Subdirectory for media files |
| `UseIndexing` | `true` | Enable index files for performance |
| `IndexFileName` | `_index.json` | Index file name |
| `MaxIndexCacheSize` | `1000` | Max items in memory cache |
| `CompressEntities` | `false` | Compress JSON files (future) |
| `PrettyPrintJson` | `false` | Pretty-print for debugging |

## Docker Deployment

See [README.md](./README.md) for comprehensive Docker setup, including:
- Volume mounting strategies
- Environment variable configuration
- Backup and restore procedures
- Troubleshooting guide

## Switching Storage Providers

Conditional registration based on configuration:

```csharp
var useFilesystem = builder.Configuration
    .GetValue<bool>("UseFilesystemStorage");

if (useFilesystem)
{
    builder.Services.AddFilesystemDataAccess(builder.Configuration);
}
else
{
    builder.Services.AddCosmosDbDataAccess(
        builder.Configuration,
        builder.Environment.IsDevelopment());
}
```

## Development vs Production

**Development (appsettings.Development.json):**
```json
{
  "FilesystemStorage": {
    "RootPath": "./data",
    "PrettyPrintJson": true,
    "MaxIndexCacheSize": 500
  }
}
```

**Production (appsettings.json):**
```json
{
  "FilesystemStorage": {
    "RootPath": "/app/data",
    "UseIndexing": true,
    "MaxIndexCacheSize": 5000,
    "PrettyPrintJson": false
  }
}
```

## Limitations

- **No distributed transactions** - Each operation is atomic but no cross-entity transactions
- **File-based locking** - Write operations are serialized per repository
- **No built-in replication** - Use filesystem-level replication if needed
- **Limited query optimization** - Complex queries load all entities into memory

## When to Use

**? Good fit for:**
- Local development
- Small to medium datasets (< 100k entities)
- Docker deployments with persistent volumes
- Scenarios without cloud dependencies
- Testing and CI/CD pipelines

**? Not recommended for:**
- Multi-instance deployments (without shared storage)
- Very large datasets (millions of entities)
- High-concurrency write scenarios
- Distributed systems requiring strong consistency

## Migration

### From CosmosDB
1. Export data from CosmosDB to JSON
2. Organize by entity type and partition key
3. Copy to `entities` directory structure
4. Restart to rebuild indexes

### To CosmosDB
1. Use repository interfaces (same API)
2. Change service registration
3. CosmosDB will create collections on first use

## Contributing

This is part of the Viblog project. See the main repository for contribution guidelines.

## License

Same as the parent Viblog project.
