using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Indexing;

namespace Viblog.Shared.Data.Sources.CosmosDb.Indexing;

/// <summary>
/// CosmosDB-based implementation of index manager
/// CosmosDB has built-in indexing, so this is primarily a no-op adapter
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public class CosmosDbIndexManager<TEntity> : IIndexManager<TEntity> where TEntity : BaseEntity
{
    private readonly ILogger _logger;

    public CosmosDbIndexManager(ILogger<CosmosDbIndexManager<TEntity>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// CosmosDB doesn't need manual index loading
    /// </summary>
    public Task LoadIndexAsync(CancellationToken cancellationToken = default)
    {
        // CosmosDB handles indexing natively
        return Task.CompletedTask;
    }

    /// <summary>
    /// CosmosDB doesn't need manual index updates
    /// </summary>
    public Task UpsertAsync(TEntity entity, string fileName, CancellationToken cancellationToken = default)
    {
        // CosmosDB handles indexing natively
        return Task.CompletedTask;
    }

    /// <summary>
    /// CosmosDB doesn't need manual index removal
    /// </summary>
    public Task RemoveAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        // CosmosDB handles indexing natively
        return Task.CompletedTask;
    }

    /// <summary>
    /// CosmosDB doesn't provide direct index entry access
    /// </summary>
    public IndexEntry? FindEntry(string id, string partitionKey)
    {
        // CosmosDB handles indexing natively - not needed for queries
        _logger.LogWarning("FindEntry called on CosmosDB implementation - this is a no-op");
        return null;
    }

    /// <summary>
    /// CosmosDB doesn't provide direct index enumeration
    /// </summary>
    public IEnumerable<IndexEntry> GetAllEntries()
    {
        // CosmosDB handles indexing natively - not needed for queries
        return Enumerable.Empty<IndexEntry>();
    }

    /// <summary>
    /// CosmosDB doesn't need manual index rebuilding
    /// </summary>
    public Task RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RebuildIndex called on CosmosDB - indexes are managed automatically");
        return Task.CompletedTask;
    }
}
