namespace Viblog.Infrastructure.Data.Entities;

/// <summary>
/// Categories for media types based on MIME type and file extension
/// </summary>
public enum MediaTypeCategory
{
    /// <summary>
    /// Image files (JPEG, PNG, GIF, WebP, SVG, etc.)
    /// </summary>
    Image,

    /// <summary>
    /// Video files (MP4, WebM, AVI, MOV, etc.)
    /// </summary>
    Video,

    /// <summary>
    /// Audio files (MP3, WAV, OGG, FLAC, etc.)
    /// </summary>
    Audio,

    /// <summary>
    /// Document files (PDF, DOCX, TXT, etc.)
    /// </summary>
    Document,

    /// <summary>
    /// Code/text files (JS, CSS, HTML, JSON, XML, etc.)
    /// </summary>
    Code,

    /// <summary>
    /// Archive files (ZIP, RAR, 7Z, TAR, etc.)
    /// </summary>
    Archive,

    /// <summary>
    /// Other/unknown file types
    /// </summary>
    Other
}
