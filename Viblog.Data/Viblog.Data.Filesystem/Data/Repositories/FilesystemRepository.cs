using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Data.Filesystem.Configuration;
using Viblog.Data.Filesystem.Indexing;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.Filesystem.Data.Repositories;

/// <summary>
/// Filesystem-based generic repository implementation with JSON storage and indexing
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseEntity</typeparam>
public class FilesystemRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly string _entityDirectory;
    protected readonly FilesystemStorageOptions _options;
    protected readonly ILogger<FilesystemRepository<TEntity>> _logger;
    protected readonly IndexManager<TEntity> _indexManager;
    protected readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    public FilesystemRepository(
        IOptions<FilesystemStorageOptions> options,
        ILogger<FilesystemRepository<TEntity>> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var rootPath = Path.GetFullPath(_options.RootPath);
        _entityDirectory = Path.Combine(rootPath, _options.EntitiesDirectory, typeof(TEntity).Name);

        // Ensure directory exists
        if (!Directory.Exists(_entityDirectory))
        {
            Directory.CreateDirectory(_entityDirectory);
        }

        _indexManager = new IndexManager<TEntity>(_entityDirectory, options, logger);
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = _options.PrettyPrintJson,
            PropertyNameCaseInsensitive = true
        };

        // Load index asynchronously on first access
        _ = _indexManager.LoadIndexAsync();
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        await _indexManager.LoadIndexAsync(cancellationToken);

        var entry = _indexManager.FindEntry(id, partitionKey);
        if (entry is null)
        {
            // Fallback: scan directory
            var filePath = GetEntityFilePath(id, partitionKey);
            if (!File.Exists(filePath))
                return null;

            return await ReadEntityFromFileAsync(filePath, cancellationToken);
        }

        var fullPath = Path.Combine(_entityDirectory, entry.FileName);
        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Index entry exists but file not found: {FilePath}", fullPath);
            return null;
        }

        var entity = await ReadEntityFromFileAsync(fullPath, cancellationToken);
        
        // Filter out soft-deleted by default
        if (entity is not null && entity.IsDeleted)
            return null;

        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<TEntity>> GetAllAsync<TKey>(
        PagingParameters pagingParameters,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        bool ascending = true,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        await _indexManager.LoadIndexAsync(cancellationToken);

        var entities = await LoadAllEntitiesAsync(includeDeleted, cancellationToken);
        
        return ApplyPagingAndSorting(entities.AsQueryable(), pagingParameters, orderBy, ascending);
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<TEntity>> FindAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        PagingParameters pagingParameters,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        bool ascending = true,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(pagingParameters);

        await _indexManager.LoadIndexAsync(cancellationToken);

        var entities = await LoadAllEntitiesAsync(includeDeleted, cancellationToken);
        var filtered = entities.AsQueryable().Where(predicate);

        return ApplyPagingAndSorting(filtered, pagingParameters, orderBy, ascending);
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        await _indexManager.LoadIndexAsync(cancellationToken);

        var entities = await LoadAllEntitiesAsync(includeDeleted, cancellationToken);
        return entities.AsQueryable().FirstOrDefault(predicate);
    }

    /// <inheritdoc/>
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await SaveEntityAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (var entity in entities)
        {
            entity.CreatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await SaveEntityAsync(entity, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await SaveEntityAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task DeleteAsync(string id, string partitionKey, bool softDelete = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        var entity = await GetByIdAsync(id, partitionKey, cancellationToken);
        if (entity is not null)
        {
            await DeleteAsync(entity, softDelete, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public virtual async Task DeleteAsync(TEntity entity, bool softDelete = true, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (softDelete)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTimeOffset.UtcNow;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await SaveEntityAsync(entity, cancellationToken);
        }
        else
        {
            var filePath = GetEntityFilePath(entity.Id, entity.GroupKey);
            
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    await _indexManager.RemoveAsync(entity.Id, entity.GroupKey, cancellationToken);
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        await _indexManager.LoadIndexAsync(cancellationToken);

        var entities = await LoadAllEntitiesAsync(includeDeleted, cancellationToken);
        return entities.AsQueryable().Any(predicate);
    }

    /// <inheritdoc/>
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        await _indexManager.LoadIndexAsync(cancellationToken);

        var entities = await LoadAllEntitiesAsync(includeDeleted, cancellationToken);
        var query = entities.AsQueryable();

        return predicate is null ? query.Count() : query.Count(predicate);
    }

    /// <inheritdoc/>
    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Filesystem operations are immediate, so this is a no-op
        return Task.FromResult(0);
    }

    /// <summary>
    /// Save an entity to the filesystem and update the index
    /// </summary>
    protected virtual async Task SaveEntityAsync(TEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var filePath = GetEntityFilePath(entity.Id, entity.GroupKey);
        var directory = Path.GetDirectoryName(filePath);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(entity, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            var fileName = Path.GetRelativePath(_entityDirectory, filePath);
            await _indexManager.UpsertAsync(entity, fileName, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Read an entity from a JSON file
    /// </summary>
    protected virtual async Task<TEntity?> ReadEntityFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<TEntity>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read entity from {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Load all entities from the filesystem
    /// </summary>
    protected virtual async Task<List<TEntity>> LoadAllEntitiesAsync(bool includeDeleted, CancellationToken cancellationToken)
    {
        var entities = new List<TEntity>();

        // Use index for faster loading
        var entries = _indexManager.GetAllEntries();
        
        foreach (var entry in entries)
        {
            if (!includeDeleted && entry.IsDeleted)
                continue;

            var filePath = Path.Combine(_entityDirectory, entry.FileName);
            if (!File.Exists(filePath))
                continue;

            var entity = await ReadEntityFromFileAsync(filePath, cancellationToken);
            if (entity is not null && (includeDeleted || !entity.IsDeleted))
            {
                entities.Add(entity);
            }
        }

        return entities;
    }

    /// <summary>
    /// Get the file path for an entity based on ID and partition key
    /// </summary>
    protected virtual string GetEntityFilePath(string id, string partitionKey)
    {
        // Organize by partition key for better directory structure
        var sanitizedPartitionKey = SanitizeFileName(partitionKey);
        var sanitizedId = SanitizeFileName(id);
        return Path.Combine(_entityDirectory, sanitizedPartitionKey, $"{sanitizedId}.json");
    }

    /// <summary>
    /// Apply paging and sorting to a queryable collection
    /// </summary>
    protected virtual PagedResult<TEntity> ApplyPagingAndSorting<TKey>(
        IQueryable<TEntity> query,
        PagingParameters pagingParameters,
        Expression<Func<TEntity, TKey>>? orderBy,
        bool ascending)
    {
        var totalCount = query.Count();

        // Apply sorting
        if (orderBy is not null)
        {
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }
        else
        {
            // Default sort by CreatedAt descending
            query = query.OrderByDescending(e => e.CreatedAt);
        }

        // Apply paging
        var items = query
            .Skip((pagingParameters.PageNumber - 1) * pagingParameters.PageSize)
            .Take(pagingParameters.PageSize)
            .ToList();

        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagingParameters.PageNumber,
            PageSize = pagingParameters.PageSize
        };
    }

    /// <summary>
    /// Sanitize a string for use in file names
    /// </summary>
    protected static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}
