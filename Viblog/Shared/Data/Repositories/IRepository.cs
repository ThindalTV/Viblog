using System.Linq.Expressions;
using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;

namespace Viblog.Shared.Data.Repositories;

/// <summary>
/// Generic repository interface providing basic CRUD operations
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseEntity</typeparam>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>
    /// Get an entity by its ID
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <param name="partitionKey">The partition key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity or null if not found</returns>
    Task<TEntity?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entities with paging and optional sorting
    /// </summary>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="orderBy">Optional sort expression (e.g., e => e.CreatedAt)</param>
    /// <param name="ascending">Sort direction (true for ascending, false for descending)</param>
    /// <param name="includeDeleted">Whether to include soft-deleted items</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with entities</returns>
    Task<PagedResult<TEntity>> GetAllAsync<TKey>(
        PagingParameters pagingParameters,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        bool ascending = true,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find entities matching the specified predicate with paging and optional sorting
    /// </summary>
    /// <param name="predicate">Filter expression</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="orderBy">Optional sort expression (e.g., e => e.CreatedAt)</param>
    /// <param name="ascending">Sort direction (true for ascending, false for descending)</param>
    /// <param name="includeDeleted">Whether to include soft-deleted items</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with matching entities</returns>
    Task<PagedResult<TEntity>> FindAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        PagingParameters pagingParameters,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        bool ascending = true,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the first entity matching the predicate or null
    /// </summary>
    /// <param name="predicate">Filter expression</param>
    /// <param name="includeDeleted">Whether to include soft-deleted items</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The first matching entity or null</returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new entity
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add multiple entities
    /// </summary>
    /// <param name="entities">The entities to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing entity
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an entity (soft delete by default)
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <param name="partitionKey">The partition key</param>
    /// <param name="softDelete">Whether to perform soft delete (default) or hard delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(string id, string partitionKey, bool softDelete = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an entity (soft delete by default)
    /// </summary>
    /// <param name="entity">The entity to delete</param>
    /// <param name="softDelete">Whether to perform soft delete (default) or hard delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(TEntity entity, bool softDelete = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if any entity matches the predicate
    /// </summary>
    /// <param name="predicate">Filter expression</param>
    /// <param name="includeDeleted">Whether to include soft-deleted items</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entity matches</returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count entities matching the predicate
    /// </summary>
    /// <param name="predicate">Filter expression (optional)</param>
    /// <param name="includeDeleted">Whether to include soft-deleted items</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of matching entities</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save all pending changes to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities affected</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
