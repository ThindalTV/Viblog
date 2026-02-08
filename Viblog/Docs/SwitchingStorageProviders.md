# Switching Storage Providers

This document explains how to switch between CosmosDB and Filesystem storage providers in the Viblog application.

## Current Configuration

The application is currently configured to use **Filesystem Storage** for development.

## Quick Switch Guide

### To Filesystem Storage (Current)

**Program.cs:**
```csharp
using Viblog.Data.Filesystem;

// ...

builder.Services.AddFilesystemDataAccess(builder.Configuration);

// ...

await Viblog.Data.Filesystem.Data.Seeders.BlogPostSeeder.SeedAsync(blogPostRepository, logger);
```

**Viblog.csproj:**
```xml
<ProjectReference Include="..\Viblog.Data\Viblog.Data.Filesystem\Viblog.Data.Filesystem.csproj" />
```

**appsettings.Development.json:**
```json
{
  "FilesystemStorage": {
    "RootPath": "./data",
    "UseIndexing": true,
    "PrettyPrintJson": true
  }
}
```

### To CosmosDB Storage

**Program.cs:**
```csharp
using Viblog.Data.CosmosDb;

// ...

builder.Services.AddCosmosDbDataAccess(builder.Configuration, builder.Environment.IsDevelopment());

// Ensure database exists
await app.Services.EnsureCosmosDbCreatedAsync();

// ...

await Viblog.Data.CosmosDb.Data.Seeders.BlogPostSeeder.SeedAsync(dbContext);
```

**Viblog.csproj:**
```xml
<ProjectReference Include="..\Viblog.Data\Viblog.Data.CosmosDb\Viblog.Data.CosmosDb.csproj" />
```

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "CosmosConnection": "AccountEndpoint=https://localhost:8081/;AccountKey=..."
  },
  "CosmosDb": {
    "DatabaseName": "ViblogDb"
  }
}
```

## Configuration-Based Switching (Recommended for Production)

For more flexibility, you can make the provider selection configuration-driven:

### 1. Add Configuration Setting

**appsettings.json:**
```json
{
  "DataStorage": {
    "Provider": "Filesystem" // or "CosmosDB"
  }
}
```

### 2. Update Program.cs

```csharp
var storageProvider = builder.Configuration["DataStorage:Provider"] ?? "Filesystem";

if (storageProvider.Equals("Filesystem", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddFilesystemDataAccess(builder.Configuration);
}
else if (storageProvider.Equals("CosmosDB", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddCosmosDbDataAccess(
        builder.Configuration,
        builder.Environment.IsDevelopment());
}
else
{
    throw new InvalidOperationException($"Unknown storage provider: {storageProvider}");
}

var app = builder.Build();

// Ensure database exists (only for CosmosDB)
if (storageProvider.Equals("CosmosDB", StringComparison.OrdinalIgnoreCase))
{
    await app.Services.EnsureCosmosDbCreatedAsync();
}

// Seed database
await SeedDatabaseAsync(app, storageProvider);

// ...

static async Task SeedDatabaseAsync(WebApplication app, string storageProvider)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Checking if database seeding is needed...");

    if (storageProvider.Equals("Filesystem", StringComparison.OrdinalIgnoreCase))
    {
        var repository = scope.ServiceProvider.GetRequiredService<IBlogPostRepository>();
        await Viblog.Data.Filesystem.Data.Seeders.BlogPostSeeder.SeedAsync(repository, logger);
    }
    else if (storageProvider.Equals("CosmosDB", StringComparison.OrdinalIgnoreCase))
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<Viblog.Data.CosmosDb.Data.ApplicationDbContext>();
        await Viblog.Data.CosmosDb.Data.Seeders.BlogPostSeeder.SeedAsync(dbContext);
    }

    logger.LogInformation("Database seeding check completed.");
}
```

### 3. Add Both Project References

**Viblog.csproj:**
```xml
<ItemGroup>
  <ProjectReference Include="..\Viblog.Data\Viblog.Data.CosmosDb\Viblog.Data.CosmosDb.csproj" />
  <ProjectReference Include="..\Viblog.Data\Viblog.Data.Filesystem\Viblog.Data.Filesystem.csproj" />
  <ProjectReference Include="..\Viblog.Infrastructure\Viblog.Infrastructure.csproj" />
</ItemGroup>
```

## Environment-Specific Providers

You can also use different providers for different environments:

**appsettings.Development.json:**
```json
{
  "DataStorage": {
    "Provider": "Filesystem"
  }
}
```

**appsettings.Production.json:**
```json
{
  "DataStorage": {
    "Provider": "CosmosDB"
  }
}
```

## Data Migration

When switching between providers, you'll need to migrate data:

### From CosmosDB to Filesystem

1. Export data from CosmosDB using the repository pattern
2. Create JSON files in the filesystem structure
3. Switch the provider configuration
4. Restart the application

### From Filesystem to CosmosDB

1. Read all entities from filesystem
2. Configure CosmosDB connection
3. Use the repository pattern to insert into CosmosDB
4. Switch the provider configuration
5. Restart the application

## Seeding Data

Both providers include seeders with the same sample data:
- 15 blog posts (3 featured, 12 regular)
- Posts spread across the last 100 days
- Various categories and tags
- Realistic view counts and metadata

The seeders automatically check if data exists before inserting, so they're safe to run multiple times.

## Troubleshooting

### Filesystem Storage Issues

- **Data not appearing**: Check that `./data` directory has proper permissions
- **Performance issues**: Ensure `UseIndexing: true` is set
- **Large datasets**: Increase `MaxIndexCacheSize` in configuration

### CosmosDB Issues

- **Connection failed**: Verify connection string and ensure emulator is running (development)
- **Containers not created**: Run `EnsureCosmosDbCreatedAsync()` before seeding
- **Partition key errors**: Ensure `SetPartitionKey()` is called before saving entities

## Best Practices

1. **Use environment variables** for sensitive configuration in production
2. **Keep seeders in sync** - both should create the same sample data
3. **Test migrations** in a staging environment before production
4. **Monitor performance** - CosmosDB has different performance characteristics than filesystem
5. **Backup before switching** - Export data before changing providers

## Docker Considerations

When using Filesystem storage in Docker:

```yaml
services:
  viblog:
    volumes:
      - viblog-data:/app/data
    environment:
      - DataStorage__Provider=Filesystem
      - FilesystemStorage__RootPath=/app/data

volumes:
  viblog-data:
```

When using CosmosDB in Docker:

```yaml
services:
  viblog:
    environment:
      - DataStorage__Provider=CosmosDB
      - ConnectionStrings__CosmosConnection=<your-connection-string>
      - CosmosDb__DatabaseName=ViblogDb
```
