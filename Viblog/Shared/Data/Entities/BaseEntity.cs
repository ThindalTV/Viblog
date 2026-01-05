namespace Viblog.Shared.Data.Entities;

/// <summary>
/// Base entity class providing common properties for all entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Partition key for CosmosDB
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the entity was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when the entity was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Timestamp when the entity was deleted (if applicable)
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
