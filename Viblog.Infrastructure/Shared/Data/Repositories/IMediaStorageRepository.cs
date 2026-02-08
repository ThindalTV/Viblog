namespace Viblog.Infrastructure.Shared.Data.Repositories;

/// <summary>
/// Interface for media storage operations supporting multiple backend providers
/// </summary>
public interface IMediaStorageRepository
{
    /// <summary>
    /// Upload a file to storage
    /// </summary>
    /// <param name="fileName">Original filename</param>
    /// <param name="fileStream">File content stream</param>
    /// <param name="mimeType">MIME type of the file</param>
    /// <param name="folderPath">Virtual folder path for organization</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage result with path, URL, and file size</returns>
    Task<MediaStorageResult> UploadAsync(
        string fileName,
        Stream fileStream,
        string mimeType,
        string folderPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a file from storage
    /// </summary>
    /// <param name="storagePath">Storage path of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing the file content</returns>
    Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    /// <param name="storagePath">Storage path of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a public URL for accessing the file
    /// </summary>
    /// <param name="storagePath">Storage path of the file</param>
    /// <param name="expiration">Optional expiration time for time-limited access</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL (may include SAS token if time-limited)</returns>
    Task<string> GetPublicUrlAsync(
        string storagePath,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a file exists in storage
    /// </summary>
    /// <param name="storagePath">Storage path to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if file exists, false otherwise</returns>
    Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the size of a file in storage
    /// </summary>
    /// <param name="storagePath">Storage path of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File size in bytes, or null if file doesn't exist</returns>
    Task<long?> GetFileSizeAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
}
