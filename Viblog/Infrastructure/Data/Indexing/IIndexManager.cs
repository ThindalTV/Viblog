using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Infrastructure.Data.Indexing;

/// <summary>
/// Interface for managing entity indexes for fast lookups
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public interface IIndexManager<TEntity> where TEntity : BaseEntity
{
    /// <summary>
    /// Load index from storage into memory cache
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LoadIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add or update an entity in the index
    /// </summary>
    /// <param name="entity">The entity to index</param>
    /// <param name="fileName">The file name or storage identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpsertAsync(TEntity entity, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove an entity from the index
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="partitionKey">Partition key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string id, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find an index entry by ID and partition key
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="partitionKey">Partition key</param>
    /// <returns>Index entry if found, null otherwise</returns>
    IndexEntry? FindEntry(string id, string partitionKey);

    /// <summary>
    /// Get all index entries
    /// </summary>
    /// <returns>Collection of all index entries</returns>
    IEnumerable<IndexEntry> GetAllEntries();

    /// <summary>
    /// Rebuild index from source storage
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RebuildIndexAsync(CancellationToken cancellationToken = default);
}
