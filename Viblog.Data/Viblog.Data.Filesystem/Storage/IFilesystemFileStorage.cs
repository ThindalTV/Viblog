namespace Viblog.Data.Filesystem.Storage;

/// <summary>
/// Service for storing and retrieving media files on the filesystem
/// </summary>
public interface IFilesystemFileStorage
{
    /// <summary>
    /// Save a file to the filesystem
    /// </summary>
    /// <param name="relativePath">Relative path where the file should be stored</param>
    /// <param name="content">File content as a stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Full path where the file was saved</returns>
    Task<string> SaveFileAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read a file from the filesystem
    /// </summary>
    /// <param name="relativePath">Relative path to the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content as a stream, or null if file doesn't exist</returns>
    Task<Stream?> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from the filesystem
    /// </summary>
    /// <param name="relativePath">Relative path to the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file was deleted, false if it didn't exist</returns>
    Task<bool> DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a file exists
    /// </summary>
    /// <param name="relativePath">Relative path to the file</param>
    /// <returns>True if the file exists</returns>
    Task<bool> FileExistsAsync(string relativePath);

    /// <summary>
    /// Get file size in bytes
    /// </summary>
    /// <param name="relativePath">Relative path to the file</param>
    /// <returns>File size in bytes, or null if file doesn't exist</returns>
    Task<long?> GetFileSizeAsync(string relativePath);

    /// <summary>
    /// Copy a file to a new location
    /// </summary>
    /// <param name="sourceRelativePath">Source file relative path</param>
    /// <param name="destinationRelativePath">Destination file relative path</param>
    /// <param name="overwrite">Whether to overwrite if destination exists</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if copied successfully</returns>
    Task<bool> CopyFileAsync(
        string sourceRelativePath,
        string destinationRelativePath,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Move a file to a new location
    /// </summary>
    /// <param name="sourceRelativePath">Source file relative path</param>
    /// <param name="destinationRelativePath">Destination file relative path</param>
    /// <param name="overwrite">Whether to overwrite if destination exists</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if moved successfully</returns>
    Task<bool> MoveFileAsync(
        string sourceRelativePath,
        string destinationRelativePath,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all files in a directory
    /// </summary>
    /// <param name="relativeDirectoryPath">Relative directory path</param>
    /// <param name="searchPattern">Search pattern (e.g., "*.jpg")</param>
    /// <param name="recursive">Whether to search recursively</param>
    /// <returns>List of relative file paths</returns>
    Task<List<string>> ListFilesAsync(
        string relativeDirectoryPath = "",
        string searchPattern = "*",
        bool recursive = false);

    /// <summary>
    /// Get the absolute path for a relative path
    /// </summary>
    /// <param name="relativePath">Relative path</param>
    /// <returns>Absolute path</returns>
    string GetAbsolutePath(string relativePath);
}
