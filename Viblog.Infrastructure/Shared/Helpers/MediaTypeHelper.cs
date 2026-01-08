using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Shared.Helpers;

/// <summary>
/// Helper class for categorizing media types and managing folder structures
/// </summary>
public static class MediaTypeHelper
{
    private static readonly Dictionary<string, MediaTypeCategory> _mimeTypeCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        { "image/jpeg", MediaTypeCategory.Image },
        { "image/jpg", MediaTypeCategory.Image },
        { "image/png", MediaTypeCategory.Image },
        { "image/gif", MediaTypeCategory.Image },
        { "image/webp", MediaTypeCategory.Image },
        { "image/svg+xml", MediaTypeCategory.Image },
        { "image/bmp", MediaTypeCategory.Image },
        { "image/tiff", MediaTypeCategory.Image },
        { "image/x-icon", MediaTypeCategory.Image },
        { "image/avif", MediaTypeCategory.Image },

        // Videos
        { "video/mp4", MediaTypeCategory.Video },
        { "video/webm", MediaTypeCategory.Video },
        { "video/ogg", MediaTypeCategory.Video },
        { "video/avi", MediaTypeCategory.Video },
        { "video/mpeg", MediaTypeCategory.Video },
        { "video/quicktime", MediaTypeCategory.Video },
        { "video/x-msvideo", MediaTypeCategory.Video },
        { "video/x-matroska", MediaTypeCategory.Video },

        // Audio
        { "audio/mpeg", MediaTypeCategory.Audio },
        { "audio/mp3", MediaTypeCategory.Audio },
        { "audio/wav", MediaTypeCategory.Audio },
        { "audio/ogg", MediaTypeCategory.Audio },
        { "audio/webm", MediaTypeCategory.Audio },
        { "audio/flac", MediaTypeCategory.Audio },
        { "audio/aac", MediaTypeCategory.Audio },
        { "audio/x-m4a", MediaTypeCategory.Audio },

        // Documents
        { "application/pdf", MediaTypeCategory.Document },
        { "application/msword", MediaTypeCategory.Document },
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", MediaTypeCategory.Document },
        { "application/vnd.ms-excel", MediaTypeCategory.Document },
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", MediaTypeCategory.Document },
        { "application/vnd.ms-powerpoint", MediaTypeCategory.Document },
        { "application/vnd.openxmlformats-officedocument.presentationml.presentation", MediaTypeCategory.Document },
        { "text/plain", MediaTypeCategory.Document },
        { "text/rtf", MediaTypeCategory.Document },
        { "application/rtf", MediaTypeCategory.Document },

        // Code
        { "text/javascript", MediaTypeCategory.Code },
        { "application/javascript", MediaTypeCategory.Code },
        { "text/css", MediaTypeCategory.Code },
        { "text/html", MediaTypeCategory.Code },
        { "application/json", MediaTypeCategory.Code },
        { "application/xml", MediaTypeCategory.Code },
        { "text/xml", MediaTypeCategory.Code },
        { "application/typescript", MediaTypeCategory.Code },
        { "text/x-csharp", MediaTypeCategory.Code },
        { "text/x-python", MediaTypeCategory.Code },
        { "text/x-java", MediaTypeCategory.Code },

        // Archives
        { "application/zip", MediaTypeCategory.Archive },
        { "application/x-zip-compressed", MediaTypeCategory.Archive },
        { "application/x-rar-compressed", MediaTypeCategory.Archive },
        { "application/x-7z-compressed", MediaTypeCategory.Archive },
        { "application/x-tar", MediaTypeCategory.Archive },
        { "application/gzip", MediaTypeCategory.Archive },
        { "application/x-gzip", MediaTypeCategory.Archive }
    };

    private static readonly Dictionary<string, MediaTypeCategory> _extensionCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        { ".jpg", MediaTypeCategory.Image },
        { ".jpeg", MediaTypeCategory.Image },
        { ".png", MediaTypeCategory.Image },
        { ".gif", MediaTypeCategory.Image },
        { ".webp", MediaTypeCategory.Image },
        { ".svg", MediaTypeCategory.Image },
        { ".bmp", MediaTypeCategory.Image },
        { ".tiff", MediaTypeCategory.Image },
        { ".ico", MediaTypeCategory.Image },
        { ".avif", MediaTypeCategory.Image },

        // Videos
        { ".mp4", MediaTypeCategory.Video },
        { ".webm", MediaTypeCategory.Video },
        { ".ogv", MediaTypeCategory.Video },
        { ".avi", MediaTypeCategory.Video },
        { ".mov", MediaTypeCategory.Video },
        { ".mkv", MediaTypeCategory.Video },
        { ".mpeg", MediaTypeCategory.Video },
        { ".mpg", MediaTypeCategory.Video },

        // Audio
        { ".mp3", MediaTypeCategory.Audio },
        { ".wav", MediaTypeCategory.Audio },
        { ".ogg", MediaTypeCategory.Audio },
        { ".oga", MediaTypeCategory.Audio },
        { ".flac", MediaTypeCategory.Audio },
        { ".aac", MediaTypeCategory.Audio },
        { ".m4a", MediaTypeCategory.Audio },

        // Documents
        { ".pdf", MediaTypeCategory.Document },
        { ".doc", MediaTypeCategory.Document },
        { ".docx", MediaTypeCategory.Document },
        { ".xls", MediaTypeCategory.Document },
        { ".xlsx", MediaTypeCategory.Document },
        { ".ppt", MediaTypeCategory.Document },
        { ".pptx", MediaTypeCategory.Document },
        { ".txt", MediaTypeCategory.Document },
        { ".rtf", MediaTypeCategory.Document },

        // Code
        { ".js", MediaTypeCategory.Code },
        { ".css", MediaTypeCategory.Code },
        { ".html", MediaTypeCategory.Code },
        { ".htm", MediaTypeCategory.Code },
        { ".json", MediaTypeCategory.Code },
        { ".xml", MediaTypeCategory.Code },
        { ".ts", MediaTypeCategory.Code },
        { ".cs", MediaTypeCategory.Code },
        { ".py", MediaTypeCategory.Code },
        { ".java", MediaTypeCategory.Code },
        { ".cpp", MediaTypeCategory.Code },
        { ".c", MediaTypeCategory.Code },
        { ".h", MediaTypeCategory.Code },

        // Archives
        { ".zip", MediaTypeCategory.Archive },
        { ".rar", MediaTypeCategory.Archive },
        { ".7z", MediaTypeCategory.Archive },
        { ".tar", MediaTypeCategory.Archive },
        { ".gz", MediaTypeCategory.Archive },
        { ".tgz", MediaTypeCategory.Archive }
    };

    private static readonly Dictionary<MediaTypeCategory, string> _categoryFolders = new()
    {
        { MediaTypeCategory.Image, "images" },
        { MediaTypeCategory.Video, "videos" },
        { MediaTypeCategory.Audio, "audio" },
        { MediaTypeCategory.Document, "documents" },
        { MediaTypeCategory.Code, "code" },
        { MediaTypeCategory.Archive, "archives" },
        { MediaTypeCategory.Other, "other" }
    };

    /// <summary>
    /// Get the media type category from MIME type and filename
    /// </summary>
    /// <param name="mimeType">MIME type of the file</param>
    /// <param name="fileName">Filename with extension</param>
    /// <returns>Media type category</returns>
    public static MediaTypeCategory GetCategory(string mimeType, string fileName)
    {
        // Try MIME type first
        if (!string.IsNullOrWhiteSpace(mimeType) && _mimeTypeCategories.TryGetValue(mimeType, out var category))
        {
            return category;
        }

        // Try wildcards for MIME type (e.g., "image/*")
        if (!string.IsNullOrWhiteSpace(mimeType))
        {
            var parts = mimeType.Split('/');
            if (parts.Length == 2)
            {
                var type = parts[0].ToLowerInvariant();
                if (type == "image") return MediaTypeCategory.Image;
                if (type == "video") return MediaTypeCategory.Video;
                if (type == "audio") return MediaTypeCategory.Audio;
            }
        }

        // Try file extension
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var extension = Path.GetExtension(fileName);
            if (!string.IsNullOrWhiteSpace(extension) && _extensionCategories.TryGetValue(extension, out category))
            {
                return category;
            }
        }

        return MediaTypeCategory.Other;
    }

    /// <summary>
    /// Get the folder name for a media type category
    /// </summary>
    /// <param name="category">Media type category</param>
    /// <returns>Folder name (e.g., "images", "videos")</returns>
    public static string GetFolderName(MediaTypeCategory category)
    {
        return _categoryFolders.TryGetValue(category, out var folder) ? folder : "other";
    }

    /// <summary>
    /// Check if a MIME type matches a category
    /// </summary>
    /// <param name="mimeType">MIME type to check</param>
    /// <param name="fileName">Filename with extension</param>
    /// <param name="category">Category to match against</param>
    /// <returns>True if the MIME type matches the category</returns>
    public static bool MatchesCategory(string mimeType, string fileName, MediaTypeCategory category)
    {
        return GetCategory(mimeType, fileName) == category;
    }

    /// <summary>
    /// Extract date folder from storage path (format: yyyyMM)
    /// </summary>
    /// <param name="storagePath">Storage path (e.g., "images/2025/01/file.jpg")</param>
    /// <returns>Date folder in yyyyMM format or null if not found</returns>
    public static string? ExtractDateFolder(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return null;
        }

        var parts = storagePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        // Expected format: category/yyyy/MM/filename
        if (parts.Length >= 3)
        {
            var year = parts[^3];
            var month = parts[^2];

            if (year.Length == 4 && int.TryParse(year, out _) &&
                month.Length == 2 && int.TryParse(month, out _))
            {
                return $"{year}{month}";
            }
        }

        return null;
    }

    /// <summary>
    /// Get display name for media type category
    /// </summary>
    /// <param name="category">Media type category</param>
    /// <returns>Display name</returns>
    public static string GetDisplayName(MediaTypeCategory category)
    {
        return category switch
        {
            MediaTypeCategory.Image => "Images",
            MediaTypeCategory.Video => "Videos",
            MediaTypeCategory.Audio => "Audio",
            MediaTypeCategory.Document => "Documents",
            MediaTypeCategory.Code => "Code Files",
            MediaTypeCategory.Archive => "Archives",
            MediaTypeCategory.Other => "Other Files",
            _ => category.ToString()
        };
    }
}
