namespace Viblog.Infrastructure.Shared.Services;

/// <summary>
/// Service for extracting metadata from media files
/// </summary>
public interface IMetadataExtractorService
{
    /// <summary>
    /// Extract metadata from a media file stream
    /// </summary>
    /// <param name="fileStream">The file stream to extract metadata from</param>
    /// <param name="mimeType">The MIME type of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of metadata key-value pairs (all values as strings)</returns>
    Task<Dictionary<string, string>> ExtractMetadataAsync(
        Stream fileStream,
        string mimeType,
        CancellationToken cancellationToken = default);
}
