namespace Viblog.Data.Filesystem.Configuration;

/// <summary>
/// Configuration options for filesystem-based storage
/// </summary>
public class FilesystemStorageOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "FilesystemStorage";

    /// <summary>
    /// Root directory for all filesystem storage
    /// Default: "./data"
    /// In Docker: Mount a volume to this path (e.g., -v /host/path:/app/data)
    /// </summary>
    public string RootPath { get; set; } = "./data";

    /// <summary>
    /// Directory for entity data storage (relative to RootPath)
    /// </summary>
    public string EntitiesDirectory { get; set; } = "entities";

    /// <summary>
    /// Directory for file/media storage (relative to RootPath)
    /// </summary>
    public string FilesDirectory { get; set; } = "files";

    /// <summary>
    /// Whether to use index files for faster entity lookups
    /// </summary>
    public bool UseIndexing { get; set; } = true;

    /// <summary>
    /// Index file name
    /// </summary>
    public string IndexFileName { get; set; } = "_index.json";

    /// <summary>
    /// Maximum items to cache in memory for index
    /// Set to 0 to disable in-memory caching
    /// </summary>
    public int MaxIndexCacheSize { get; set; } = 1000;

    /// <summary>
    /// Whether to compress entity JSON files
    /// </summary>
    public bool CompressEntities { get; set; } = false;

    /// <summary>
    /// Whether to pretty-print JSON (for debugging)
    /// </summary>
    public bool PrettyPrintJson { get; set; } = false;
}
