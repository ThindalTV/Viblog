# Quick Reference - Viblog Filesystem Storage

## Running the Application

```bash
# Clean start (deletes existing data)
Remove-Item -Recurse -Force ./Viblog/data -ErrorAction SilentlyContinue
dotnet run --project Viblog

# Normal start (keeps existing data)
dotnet run --project Viblog
```

## Configuration Location

`Viblog/appsettings.Development.json`

## Default Settings

| Setting | Value | Description |
|---------|-------|-------------|
| RootPath | `./data` | Where all data is stored |
| UseIndexing | `true` | Enable fast lookups |
| PrettyPrintJson | `true` | Human-readable JSON (dev only) |
| MaxIndexCacheSize | `500` | Entities cached in memory |

## Data Directory Structure

```
./Viblog/data/
??? entities/           # Entity JSON files
?   ??? BlogPost/
?       ??? 2024/      # Partitioned by year
?       ??? 2025/
?       ??? _index.json
??? files/             # Media files
```

## Seeded Data

**15 Blog Posts** automatically created on first run:
- 3 Featured posts (recent)
- 12 Regular posts (last 100 days)
- Multiple categories and tags
- Realistic view counts

## Common Tasks

### View All Posts
```bash
# Open in browser
https://localhost:5001/blog
```

### Inspect Data
```bash
# View index
Get-Content ./Viblog/data/entities/BlogPost/_index.json | ConvertFrom-Json

# Count posts
(Get-ChildItem ./Viblog/data/entities/BlogPost -Recurse -Filter *.json | Where-Object {$_.Name -ne '_index.json'}).Count
```

### Clear Data
```bash
Remove-Item -Recurse -Force ./Viblog/data
```

### Rebuild Index
```bash
# Delete index file
Remove-Item ./Viblog/data/entities/BlogPost/_index.json

# Restart app (index rebuilds automatically)
dotnet run --project Viblog
```

## Switching to CosmosDB

1. **Update project reference** in `Viblog.csproj`:
   ```xml
   <ProjectReference Include="..\Viblog.Data\Viblog.Data.CosmosDb\Viblog.Data.CosmosDb.csproj" />
   ```

2. **Update Program.cs**:
   ```csharp
   builder.Services.AddCosmosDbDataAccess(builder.Configuration, builder.Environment.IsDevelopment());
   ```

3. **Update appsettings**:
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

See `Viblog/Docs/SwitchingStorageProviders.md` for details.

## Docker Quick Start

```yaml
# docker-compose.yml
services:
  viblog:
    build: .
    ports:
      - "8080:8080"
    volumes:
      - viblog-data:/app/data
    environment:
      - FilesystemStorage__RootPath=/app/data

volumes:
  viblog-data:
```

```bash
docker-compose up -d
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| No posts showing | Check `./Viblog/data/entities/BlogPost/` exists |
| Slow queries | Set `UseIndexing: true` |
| Can't write files | Check directory permissions |
| Index corruption | Delete `_index.json` and restart |

## Performance Tips

**Development:**
- PrettyPrintJson: `true` (easier debugging)
- MaxIndexCacheSize: `500` (balanced)

**Production:**
- PrettyPrintJson: `false` (smaller files)
- MaxIndexCacheSize: `5000` (faster queries)

## File Locations

| File | Purpose |
|------|---------|
| `Viblog/Program.cs` | Service registration |
| `Viblog/appsettings.Development.json` | Configuration |
| `Viblog.Data.Filesystem/` | Storage implementation |
| `Viblog/data/` | Runtime data (gitignored) |

## Support

- ?? Full guide: `Viblog.Data.Filesystem/README.md`
- ?? Quick start: `Viblog.Data.Filesystem/QUICKSTART.md`
- ?? Switching providers: `Viblog/Docs/SwitchingStorageProviders.md`
- ?? Implementation summary: `Viblog.Data.Filesystem/IMPLEMENTATION_SUMMARY.md`
