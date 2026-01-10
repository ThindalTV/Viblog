# Filesystem Storage - Docker Configuration Guide

This guide explains how to configure and use filesystem-based storage with Docker for the Viblog application.

## Overview

The filesystem storage provider stores both entity data and media files on the local filesystem, making it ideal for:
- Local development without external dependencies
- Docker environments with persistent volumes
- Scenarios where CosmosDB/Blob Storage are not available

## Directory Structure

By default, all data is stored under the configured root path:

```
./data/                          # Root path (configurable)
??? entities/                    # Entity data (JSON files with indexes)
?   ??? BlogPost/               # Blog posts organized by partition key (year)
?   ?   ??? 2024/
?   ?   ?   ??? post-id-1.json
?   ?   ?   ??? post-id-2.json
?   ?   ??? 2025/
?   ?   ??? _index.json         # Index file for fast lookups
?   ??? MediaItem/              # Media metadata
?       ??? media/
?       ??? _index.json
??? files/                      # Actual media files
    ??? images/
    ?   ??? 2024/
    ?       ??? 12/
    ?           ??? example.jpg
    ??? documents/
```

## Configuration

### appsettings.json

Add the following configuration to your `appsettings.json`:

```json
{
  "FilesystemStorage": {
    "RootPath": "./data",
    "EntitiesDirectory": "entities",
    "FilesDirectory": "files",
    "UseIndexing": true,
    "IndexFileName": "_index.json",
    "MaxIndexCacheSize": 1000,
    "CompressEntities": false,
    "PrettyPrintJson": false
  }
}
```

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `RootPath` | `./data` | Root directory for all storage. **Mount this as a Docker volume!** |
| `EntitiesDirectory` | `entities` | Subdirectory for entity data (relative to RootPath) |
| `FilesDirectory` | `files` | Subdirectory for media files (relative to RootPath) |
| `UseIndexing` | `true` | Enable JSON index files for faster queries |
| `IndexFileName` | `_index.json` | Name of the index file in each entity directory |
| `MaxIndexCacheSize` | `1000` | Maximum items to cache in memory (0 = no caching) |
| `CompressEntities` | `false` | Enable gzip compression for JSON files (future feature) |
| `PrettyPrintJson` | `false` | Pretty-print JSON for debugging (increases file size) |

## Docker Setup

### Docker Compose

Here's an example `docker-compose.yml` configuration:

```yaml
version: '3.8'

services:
  viblog:
    image: viblog:latest
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - FilesystemStorage__RootPath=/app/data
    volumes:
      # Mount a named volume for persistent storage
      - viblog-data:/app/data
    restart: unless-stopped

volumes:
  # Define the named volume
  viblog-data:
    driver: local
```

### Dockerfile Considerations

Make sure your Dockerfile creates the data directory:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Create data directory
RUN mkdir -p /app/data

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# ... rest of your build steps ...

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Viblog.dll"]
```

### Volume Mounting Options

#### Option 1: Named Volume (Recommended for Production)
```bash
docker run -d \
  -p 8080:8080 \
  -v viblog-data:/app/data \
  viblog:latest
```

**Pros:**
- Docker manages the volume
- Survives container removal
- Better performance on Windows/Mac

**Cons:**
- Data not easily accessible from host

#### Option 2: Bind Mount (Better for Development)
```bash
docker run -d \
  -p 8080:8080 \
  -v /path/on/host/viblog-data:/app/data \
  viblog:latest
```

**Pros:**
- Easy access to files from host
- Can edit/backup directly
- Good for development

**Cons:**
- Path must exist on host
- Permissions issues on Linux

#### Option 3: Environment Variable Override
```bash
docker run -d \
  -p 8080:8080 \
  -e FilesystemStorage__RootPath=/custom/path \
  -v /host/custom:/custom/path \
  viblog:latest
```

## Service Registration

### Using Filesystem Storage

In your `Program.cs`, register the filesystem storage services:

```csharp
using Viblog.Data.Filesystem;

var builder = WebApplication.CreateBuilder(args);

// Register filesystem storage instead of CosmosDB
builder.Services.AddFilesystemDataAccess(builder.Configuration);

var app = builder.Build();
```

### Switching Between Storage Providers

You can conditionally switch between storage providers based on configuration:

```csharp
var storageProvider = builder.Configuration["StorageProvider"] ?? "CosmosDB";

if (storageProvider.Equals("Filesystem", StringComparison.OrdinalIgnoreCase))
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

Then in `appsettings.json`:

```json
{
  "StorageProvider": "Filesystem"
}
```

## Performance Considerations

### Index Files
- Enable `UseIndexing: true` for better read performance
- Index files are automatically maintained on create/update/delete
- Rebuild index if corrupted by deleting `_index.json` files

### Memory Usage
- Adjust `MaxIndexCacheSize` based on your dataset size
- Set to `0` to disable in-memory caching (slower but uses less RAM)

### File I/O
- Uses buffered streams (80KB buffer) for optimal performance
- Async operations throughout for better scalability
- Automatic cleanup of empty directories

## Backup and Restore

### Backup
Simply copy the entire data directory:

```bash
# Using Docker volume
docker run --rm \
  -v viblog-data:/source \
  -v /backup/location:/backup \
  alpine tar czf /backup/viblog-backup-$(date +%Y%m%d).tar.gz -C /source .

# Using bind mount
tar czf viblog-backup-$(date +%Y%m%d).tar.gz /path/to/viblog-data
```

### Restore
```bash
# Using Docker volume
docker run --rm \
  -v viblog-data:/target \
  -v /backup/location:/backup \
  alpine tar xzf /backup/viblog-backup-YYYYMMDD.tar.gz -C /target

# Using bind mount
tar xzf viblog-backup-YYYYMMDD.tar.gz -C /path/to/viblog-data
```

## Troubleshooting

### Permission Issues (Linux)
If you encounter permission denied errors:

```bash
# Find the UID/GID your container uses
docker run --rm viblog:latest id

# Set ownership on host
sudo chown -R 1000:1000 /path/to/viblog-data
```

Or use a user directive in your Dockerfile:

```dockerfile
RUN useradd -m -u 1000 viblog
USER viblog
```

### Index Corruption
If queries are slow or returning incorrect results:

```bash
# Stop the container
docker stop viblog

# Delete index files
docker run --rm -v viblog-data:/data alpine \
  find /data/entities -name "_index.json" -delete

# Restart (indexes will rebuild automatically)
docker start viblog
```

### Disk Space
Monitor disk usage of your volumes:

```bash
docker system df -v
```

## Migration from CosmosDB/Blob Storage

To migrate existing data:

1. Export data from CosmosDB to JSON files
2. Organize by entity type and partition key
3. Copy to the `entities` directory structure
4. Restart application to rebuild indexes

For media files:

1. Download from Blob Storage
2. Organize in the same structure as StoragePath metadata
3. Copy to the `files` directory
4. Update MediaItem records if paths changed

## Best Practices

1. **Always use volumes** - Never store data in the container layer
2. **Regular backups** - Automate backup of the data directory
3. **Monitor disk space** - Set up alerts for volume usage
4. **Enable indexing** - Unless memory is very constrained
5. **Pretty-print in dev only** - Keep `PrettyPrintJson: false` in production
6. **Separate volumes** - Consider separate volumes for entities and files for easier management

## Example: Complete Docker Compose Setup

```yaml
version: '3.8'

services:
  viblog:
    image: viblog:latest
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - StorageProvider=Filesystem
      - FilesystemStorage__RootPath=/app/data
      - FilesystemStorage__UseIndexing=true
      - FilesystemStorage__MaxIndexCacheSize=2000
    volumes:
      - viblog-data:/app/data
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  # Optional: Backup service
  backup:
    image: alpine:latest
    volumes:
      - viblog-data:/source:ro
      - ./backups:/backup
    command: >
      sh -c "while true; do
        tar czf /backup/viblog-\$(date +\%Y\%m\%d-\%H\%M).tar.gz -C /source . &&
        find /backup -name 'viblog-*.tar.gz' -mtime +7 -delete;
        sleep 86400;
      done"

volumes:
  viblog-data:
    driver: local
```
