namespace Viblog.Infrastructure.Data.Indexing;

/// <summary>
/// Index entry for fast entity lookups
/// </summary>
public record IndexEntry
{
    /// <summary>
    /// Entity ID
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Partition key for the entity
    /// </summary>
    public string PartitionKey { get; init; } = string.Empty;

    /// <summary>
    /// Storage identifier (file name, blob path, etc.)
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// When the entity was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the entity was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Whether the entity is soft-deleted
    /// </summary>
    public bool IsDeleted { get; init; }
}
