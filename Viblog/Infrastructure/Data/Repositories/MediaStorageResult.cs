namespace Viblog.Infrastructure.Data.Repositories;

/// <summary>
/// Result returned from media storage operations
/// </summary>
public class MediaStorageResult
{
    /// <summary>
    /// Path where the file is stored in the storage backend
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Public URL for accessing the file
    /// </summary>
    public string PublicUrl { get; set; } = string.Empty;

    /// <summary>
    /// Size of the stored file in bytes
    /// </summary>
    public long FileSize { get; set; }
}
