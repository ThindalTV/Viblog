using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Data.Filesystem.Configuration;

namespace Viblog.Data.Filesystem.Storage;

/// <summary>
/// Filesystem-based file storage implementation
/// </summary>
public class FilesystemFileStorage : IFilesystemFileStorage
{
    private readonly string _filesRootPath;
    private readonly FilesystemStorageOptions _options;
    private readonly ILogger<FilesystemFileStorage> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public FilesystemFileStorage(
        IOptions<FilesystemStorageOptions> options,
        ILogger<FilesystemFileStorage> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var rootPath = Path.GetFullPath(_options.RootPath);
        _filesRootPath = Path.Combine(rootPath, _options.FilesDirectory);

        // Ensure directory exists
        if (!Directory.Exists(_filesRootPath))
        {
            Directory.CreateDirectory(_filesRootPath);
            _logger.LogInformation("Created filesystem storage directory: {Path}", _filesRootPath);
        }
    }

    /// <inheritdoc/>
    public async Task<string> SaveFileAsync(string relativePath, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(content);

        var fullPath = GetAbsolutePath(relativePath);
        var directory = Path.GetDirectoryName(fullPath);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fileStream = new FileStream(
                fullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920, // 80 KB buffer for better performance
                useAsync: true);

            await content.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);

            _logger.LogDebug("Saved file to {Path}", fullPath);
            return fullPath;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Stream?> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var fullPath = GetAbsolutePath(relativePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found: {Path}", fullPath);
            return null;
        }

        try
        {
            // Return a buffered stream for better performance
            var fileStream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            return fileStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {Path}", fullPath);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var fullPath = GetAbsolutePath(relativePath);

        if (!File.Exists(fullPath))
        {
            return false;
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            File.Delete(fullPath);
            _logger.LogDebug("Deleted file: {Path}", fullPath);

            // Clean up empty parent directories
            await CleanupEmptyDirectoriesAsync(Path.GetDirectoryName(fullPath));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Path}", fullPath);
            return false;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public Task<bool> FileExistsAsync(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var fullPath = GetAbsolutePath(relativePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    /// <inheritdoc/>
    public Task<long?> GetFileSizeAsync(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var fullPath = GetAbsolutePath(relativePath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<long?>(null);
        }

        var fileInfo = new FileInfo(fullPath);
        return Task.FromResult<long?>(fileInfo.Length);
    }

    /// <inheritdoc/>
    public async Task<bool> CopyFileAsync(
        string sourceRelativePath,
        string destinationRelativePath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRelativePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRelativePath);

        var sourcePath = GetAbsolutePath(sourceRelativePath);
        var destPath = GetAbsolutePath(destinationRelativePath);

        if (!File.Exists(sourcePath))
        {
            _logger.LogWarning("Source file not found: {Path}", sourcePath);
            return false;
        }

        if (File.Exists(destPath) && !overwrite)
        {
            _logger.LogWarning("Destination file already exists: {Path}", destPath);
            return false;
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var destDirectory = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            File.Copy(sourcePath, destPath, overwrite);
            _logger.LogDebug("Copied file from {Source} to {Destination}", sourcePath, destPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file from {Source} to {Destination}", sourcePath, destPath);
            return false;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> MoveFileAsync(
        string sourceRelativePath,
        string destinationRelativePath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRelativePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRelativePath);

        var sourcePath = GetAbsolutePath(sourceRelativePath);
        var destPath = GetAbsolutePath(destinationRelativePath);

        if (!File.Exists(sourcePath))
        {
            _logger.LogWarning("Source file not found: {Path}", sourcePath);
            return false;
        }

        if (File.Exists(destPath) && !overwrite)
        {
            _logger.LogWarning("Destination file already exists: {Path}", destPath);
            return false;
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var destDirectory = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            if (File.Exists(destPath) && overwrite)
            {
                File.Delete(destPath);
            }

            File.Move(sourcePath, destPath);
            _logger.LogDebug("Moved file from {Source} to {Destination}", sourcePath, destPath);

            // Clean up empty source directory
            await CleanupEmptyDirectoriesAsync(Path.GetDirectoryName(sourcePath));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving file from {Source} to {Destination}", sourcePath, destPath);
            return false;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public Task<List<string>> ListFilesAsync(
        string relativeDirectoryPath = "",
        string searchPattern = "*",
        bool recursive = false)
    {
        var fullPath = string.IsNullOrWhiteSpace(relativeDirectoryPath)
            ? _filesRootPath
            : GetAbsolutePath(relativeDirectoryPath);

        if (!Directory.Exists(fullPath))
        {
            return Task.FromResult(new List<string>());
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(fullPath, searchPattern, searchOption);

        // Convert to relative paths
        var relativePaths = files
            .Select(f => Path.GetRelativePath(_filesRootPath, f))
            .ToList();

        return Task.FromResult(relativePaths);
    }

    /// <inheritdoc/>
    public string GetAbsolutePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        // Normalize path separators
        relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar)
                                   .Replace('/', Path.DirectorySeparatorChar);

        // Remove leading separator if present
        relativePath = relativePath.TrimStart(Path.DirectorySeparatorChar);

        return Path.Combine(_filesRootPath, relativePath);
    }

    /// <summary>
    /// Clean up empty directories recursively up to the root
    /// </summary>
    private async Task CleanupEmptyDirectoriesAsync(string? directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        // Don't delete the root files directory
        if (directoryPath.Equals(_filesRootPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            // Check if directory is empty
            if (!Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                Directory.Delete(directoryPath);
                _logger.LogDebug("Deleted empty directory: {Path}", directoryPath);

                // Recursively clean parent
                var parentDirectory = Path.GetDirectoryName(directoryPath);
                await CleanupEmptyDirectoriesAsync(parentDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning up directory: {Path}", directoryPath);
        }
    }
}
