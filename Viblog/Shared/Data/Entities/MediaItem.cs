namespace Viblog.Shared.Data.Entities;

/// <summary>
/// Represents a media item in the media library with storage and metadata information
/// </summary>
public class MediaItem : BaseEntity
{
    /// <summary>
    /// Original filename of the uploaded file
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File extension (e.g., .jpg, .pdf, .mp4)
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type of the file (e.g., image/jpeg, application/pdf)
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Storage path where the file is stored in the backend
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Public URL for accessing the file
    /// </summary>
    public string PublicUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL for preview image or icon
    /// </summary>
    public string? PreviewUrl { get; set; }

    /// <summary>
    /// Virtual folder path for organization (e.g., /images/blog or /documents)
    /// </summary>
    public string FolderPath { get; set; } = "/";

    /// <summary>
    /// User-friendly title for the media item
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Description of the media item
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Alternative text for accessibility (primarily for images)
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Width in pixels (for images and videos)
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Height in pixels (for images and videos)
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Additional metadata stored as key-value pairs
    /// </summary>
    public Dictionary<string, string> AdditionalMetadata { get; set; } = new();

    /// <summary>
    /// Number of times this media item is referenced
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Timestamp when the media was last accessed
    /// </summary>
    public DateTimeOffset? LastAccessedAt { get; set; }

    /// <summary>
    /// User who uploaded the media
    /// </summary>
    public string? UploadedBy { get; set; }

    /// <summary>
    /// Current status of the media item
    /// </summary>
    public MediaStatus Status { get; set; } = MediaStatus.Uploading;

    /// <summary>
    /// Error message if upload or processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Updates the partition key based on creation date (format: yyyy-MM)
    /// </summary>
    public void UpdatePartitionKey()
    {
        PartitionKey = CreatedAt.ToString("yyyy-MM");
    }
}
