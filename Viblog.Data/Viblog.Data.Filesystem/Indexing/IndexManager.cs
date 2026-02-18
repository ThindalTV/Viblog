using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Data.Filesystem.Configuration;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Indexing;

namespace Viblog.Data.Filesystem.Indexing;

/// <summary>
/// Filesystem-based implementation of index manager for fast entity lookups without scanning all files
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public class IndexManager<TEntity> : IIndexManager<TEntity> where TEntity : BaseEntity
{
    private readonly string _indexFilePath;
    private readonly string _entityDirectory;
    private readonly FilesystemStorageOptions _options;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private readonly ConcurrentDictionary<string, IndexEntry> _indexCache = new();
    private bool _cacheLoaded;

    public IndexManager(
        string entityDirectory,
        IOptions<FilesystemStorageOptions> options,
        ILogger logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentException.ThrowIfNullOrWhiteSpace(entityDirectory);

        _entityDirectory = entityDirectory;
        _indexFilePath = Path.Combine(entityDirectory, _options.IndexFileName);
    }

    /// <summary>
    /// Load index from file into memory cache
    /// </summary>
    public async Task LoadIndexAsync(CancellationToken cancellationToken = default)
    {
        if (_cacheLoaded || !_options.UseIndexing)
            return;

        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (_cacheLoaded)
                return;

            if (!File.Exists(_indexFilePath))
            {
                _cacheLoaded = true;
                return;
            }

            var json = await File.ReadAllTextAsync(_indexFilePath, cancellationToken);
            var entries = JsonSerializer.Deserialize<List<IndexEntry>>(json);

            if (entries is not null)
            {
                foreach (var entry in entries)
                {
                    _indexCache[GetCacheKey(entry.Id, entry.PartitionKey)] = entry;
                }
            }

            _cacheLoaded = true;
            _logger.LogInformation("Loaded {Count} index entries for {EntityType}", _indexCache.Count, typeof(TEntity).Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load index for {EntityType}, will rebuild", typeof(TEntity).Name);
            _cacheLoaded = true;
        }
        finally
        {
            _indexLock.Release();
        }
    }

    /// <summary>
    /// Add or update an entity in the index
    /// </summary>
    public async Task UpsertAsync(TEntity entity, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (!_options.UseIndexing)
            return;

        var entry = new IndexEntry
        {
            Id = entity.Id,
            PartitionKey = entity.GroupKey,
            FileName = fileName,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted
        };

        _indexCache[GetCacheKey(entity.Id, entity.GroupKey)] = entry;

        // Persist to disk
        await PersistIndexAsync(cancellationToken);
    }

    /// <summary>
    /// Remove an entity from the index
    /// </summary>
    public async Task RemoveAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        if (!_options.UseIndexing)
            return;

        var key = GetCacheKey(id, partitionKey);
        _indexCache.TryRemove(key, out _);

        await PersistIndexAsync(cancellationToken);
    }

    /// <summary>
    /// Find an index entry by ID and partition key
    /// </summary>
    public IndexEntry? FindEntry(string id, string partitionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        if (!_options.UseIndexing)
            return null;

        var key = GetCacheKey(id, partitionKey);
        return _indexCache.TryGetValue(key, out var entry) ? entry : null;
    }

    /// <summary>
    /// Get all index entries
    /// </summary>
    public IEnumerable<IndexEntry> GetAllEntries()
    {
        return _indexCache.Values;
    }

    /// <summary>
    /// Rebuild index from all entity files in directory
    /// </summary>
    public async Task RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.UseIndexing)
            return;

        _logger.LogInformation("Rebuilding index for {EntityType}", typeof(TEntity).Name);

        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            _indexCache.Clear();

            if (!Directory.Exists(_entityDirectory))
                return;

            var jsonFiles = Directory.GetFiles(_entityDirectory, "*.json", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(_options.IndexFileName, StringComparison.OrdinalIgnoreCase));

            foreach (var file in jsonFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var entity = JsonSerializer.Deserialize<TEntity>(json);

                    if (entity is not null)
                    {
                        var fileName = Path.GetRelativePath(_entityDirectory, file);
                        var entry = new IndexEntry
                        {
                            Id = entity.Id,
                            PartitionKey = entity.GroupKey,
                            FileName = fileName,
                            CreatedAt = entity.CreatedAt,
                            UpdatedAt = entity.UpdatedAt,
                            IsDeleted = entity.IsDeleted
                        };

                        _indexCache[GetCacheKey(entity.Id, entity.GroupKey)] = entry;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to index file {File}", file);
                }
            }

            await PersistIndexAsync(cancellationToken);
            _logger.LogInformation("Rebuilt index with {Count} entries for {EntityType}", _indexCache.Count, typeof(TEntity).Name);
        }
        finally
        {
            _indexLock.Release();
        }
    }

    /// <summary>
    /// Persist index cache to disk
    /// </summary>
    private async Task PersistIndexAsync(CancellationToken cancellationToken)
    {
        if (!_options.UseIndexing)
            return;

        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            var entries = _indexCache.Values.OrderBy(e => e.CreatedAt).ToList();
            
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = _options.PrettyPrintJson
            };

            var json = JsonSerializer.Serialize(entries, jsonOptions);

            var directory = Path.GetDirectoryName(_indexFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_indexFilePath, json, cancellationToken);
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private static string GetCacheKey(string id, string partitionKey) => $"{partitionKey}|{id}";
}
