namespace Viblog.Shared.Helpers;

/// <summary>
/// Helper class for determining appropriate icons for media file types
/// </summary>
public static class MediaIconHelper
{
    // Extension to icon mappings
    private static readonly HashSet<string> CodeFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // .NET
        ".cs", ".vb", ".fs",
        // JavaScript/TypeScript
        ".js", ".ts", ".jsx", ".tsx",
        // Markup
        ".html", ".htm", ".xml", ".xaml",
        // Stylesheets
        ".css", ".scss", ".sass", ".less",
        // Data formats
        ".json", ".yaml", ".yml",
        // JVM languages
        ".java", ".kt", ".scala",
        // Scripting
        ".py", ".rb", ".php",
        // C/C++
        ".cpp", ".c", ".h", ".hpp",
        // Modern languages
        ".go", ".rs", ".swift",
        // Shell/SQL
        ".sql", ".sh", ".bat", ".ps1"
    };

    private static readonly HashSet<string> PresentationExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ppt", ".pptx", ".pps", ".ppsx"
    };

    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".rtf"
    };

    private static readonly HashSet<string> SpreadsheetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xls", ".xlsx", ".csv"
    };

    private static readonly HashSet<string> PdfExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf"
    };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".rar", ".7z", ".tar", ".gz"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg", ".m4a", ".flac", ".aac"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".log"
    };

    /// <summary>
    /// Get the icon path for a given MIME type and filename
    /// </summary>
    /// <param name="mimeType">The MIME type of the file</param>
    /// <param name="fileName">The filename (optional, used as fallback for extension-based detection)</param>
    /// <returns>Icon path or null for image types (which should display the actual image)</returns>
    public static string? GetFileTypeIcon(string mimeType, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            return GetIconByFileName(fileName) ?? "/icons/file-unknown.svg";
        }

        // Images should display the actual image, not an icon
        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // PDF documents
        if (mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "/icons/file-pdf.svg";
        }

        // Video files
        if (mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return "/icons/file-video.svg";
        }

        // Audio files
        if (mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return "/icons/file-audio.svg";
        }

        // Microsoft Word documents
        if (mimeType.Equals("application/msword", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase))
        {
            return "/icons/file-document.svg";
        }

        // Microsoft Excel spreadsheets
        if (mimeType.Equals("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Equals("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", StringComparison.OrdinalIgnoreCase))
        {
            return "/icons/file-spreadsheet.svg";
        }

        // Microsoft PowerPoint presentations
        if (mimeType.Equals("application/vnd.ms-powerpoint", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Equals("application/vnd.openxmlformats-officedocument.presentationml.presentation", StringComparison.OrdinalIgnoreCase))
        {
            return "/icons/file-presentation.svg";
        }

        // Archive files
        if (mimeType.Equals("application/zip", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Equals("application/x-rar-compressed", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Equals("application/x-7z-compressed", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Equals("application/gzip", StringComparison.OrdinalIgnoreCase))
        {
            return "/icons/file-archive.svg";
        }

        // Fallback to extension-based detection if MIME type doesn't match
        var fallbackIcon = GetIconByFileName(fileName);
        return fallbackIcon ?? "/icons/file-unknown.svg";
    }

    /// <summary>
    /// Get icon based on file extension
    /// </summary>
    /// <param name="fileName">The filename to extract extension from</param>
    /// <returns>Icon path or null if extension doesn't match any known types</returns>
    private static string? GetIconByFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var extension = Path.GetExtension(fileName);
        
        if (CodeFileExtensions.Contains(extension))
            return "/icons/file-code.svg";
        
        if (PresentationExtensions.Contains(extension))
            return "/icons/file-presentation.svg";
        
        if (DocumentExtensions.Contains(extension))
            return "/icons/file-document.svg";
        
        if (SpreadsheetExtensions.Contains(extension))
            return "/icons/file-spreadsheet.svg";
        
        if (PdfExtensions.Contains(extension))
            return "/icons/file-pdf.svg";
        
        if (ArchiveExtensions.Contains(extension))
            return "/icons/file-archive.svg";
        
        if (VideoExtensions.Contains(extension))
            return "/icons/file-video.svg";
        
        if (AudioExtensions.Contains(extension))
            return "/icons/file-audio.svg";
        
        if (TextExtensions.Contains(extension))
            return "/icons/file-text.svg";

        return null;
    }
}
