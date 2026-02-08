namespace Viblog.Shared.Configuration;

/// <summary>
/// Configuration settings for the Media Library feature
/// </summary>
public class MediaLibrarySettings
{
    public const string SectionName = "MediaLibrary";

    public StorageSettings Storage { get; set; } = new();
    public UploadSettings Upload { get; set; } = new();
    public ThumbnailSettings Thumbnails { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();
}

/// <summary>
/// Storage provider configuration
/// </summary>
public class StorageSettings
{
    /// <summary>
    /// Storage provider type (BlobStorage, FileSystem, etc.)
    /// </summary>
    public string Provider { get; set; } = "BlobStorage";

    /// <summary>
    /// Container/bucket name for media storage
    /// </summary>
    public string ContainerName { get; set; } = "media";

    /// <summary>
    /// CDN base URL for serving media files
    /// </summary>
    public string CdnBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use CDN for public URLs
    /// </summary>
    public bool EnableCdn { get; set; } = false;
}

/// <summary>
/// Upload restrictions and limits
/// </summary>
public class UploadSettings
{
    /// <summary>
    /// Maximum file size in megabytes
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 50;

    /// <summary>
    /// Maximum number of concurrent uploads
    /// </summary>
    public int MaxConcurrentUploads { get; set; } = 3;

    /// <summary>
    /// Allowed file extensions (e.g., ".jpg", ".pdf")
    /// </summary>
    public List<string> AllowedFileTypes { get; set; } = new();

    /// <summary>
    /// Allowed MIME types
    /// </summary>
    public List<string> AllowedMimeTypes { get; set; } = new();

    /// <summary>
    /// Get max file size in bytes
    /// </summary>
    public long MaxFileSizeBytes => MaxFileSizeMB * 1024L * 1024L;

    /// <summary>
    /// Check if file extension is allowed
    /// </summary>
    public bool IsFileTypeAllowed(string extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        return AllowedFileTypes.Contains(extension.ToLowerInvariant());
    }

    /// <summary>
    /// Check if MIME type is allowed
    /// </summary>
    public bool IsMimeTypeAllowed(string mimeType)
    {
        ArgumentNullException.ThrowIfNull(mimeType);
        return AllowedMimeTypes.Contains(mimeType.ToLowerInvariant());
    }
}

/// <summary>
/// Thumbnail generation settings
/// </summary>
public class ThumbnailSettings
{
    /// <summary>
    /// Whether to generate thumbnails for images
    /// </summary>
    public bool GenerateThumbnails { get; set; } = true;

    /// <summary>
    /// Maximum thumbnail width in pixels
    /// </summary>
    public int MaxWidth { get; set; } = 300;

    /// <summary>
    /// Maximum thumbnail height in pixels
    /// </summary>
    public int MaxHeight { get; set; } = 300;

    /// <summary>
    /// JPEG quality for thumbnails (1-100)
    /// </summary>
    public int Quality { get; set; } = 85;
}

/// <summary>
/// Performance and caching settings
/// </summary>
public class PerformanceSettings
{
    /// <summary>
    /// Whether to enable response caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache duration in minutes
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;
}
