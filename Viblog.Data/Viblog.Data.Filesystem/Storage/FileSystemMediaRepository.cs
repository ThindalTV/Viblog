using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.Filesystem.Storage;

/// <summary>
/// File system implementation of media storage repository
/// </summary>
public class FileSystemMediaRepository : IMediaStorageRepository
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<FileSystemMediaRepository> _logger;

    public FileSystemMediaRepository(
        IConfiguration configuration,
        ILogger<FileSystemMediaRepository> logger)
    {
        _logger = logger;

        _basePath = configuration["MediaStorage:FileSystem:BasePath"]
            ?? throw new InvalidOperationException("FileSystem BasePath is not configured");

        _baseUrl = configuration["MediaStorage:FileSystem:BaseUrl"]
            ?? throw new InvalidOperationException("FileSystem BaseUrl is not configured");

        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created media storage directory: {BasePath}", _basePath);
        }
    }

    /// <inheritdoc/>
    public async Task<MediaStorageResult> UploadAsync(
        string fileName,
        Stream fileStream,
        string mimeType,
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate storage path with date-based structure: yyyy/MM/filename
            var now = DateTimeOffset.UtcNow;
            var dateFolder = Path.Combine(now.Year.ToString(), now.Month.ToString("00"));
            var uniqueFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
            
            // Build full directory path
            var directoryPath = _basePath;
            if (!string.IsNullOrWhiteSpace(folderPath) && folderPath != "/")
            {
                directoryPath = Path.Combine(directoryPath, folderPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            }
            directoryPath = Path.Combine(directoryPath, dateFolder);

            // Create directory structure
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                _logger.LogInformation("Created directory: {DirectoryPath}", directoryPath);
            }

            // Full file path
            var fullPath = Path.Combine(directoryPath, uniqueFileName);

            // Write file to disk
            await using (var fileStreamWriter = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await fileStream.CopyToAsync(fileStreamWriter, cancellationToken);
            }

            // Get file size
            var fileInfo = new FileInfo(fullPath);
            var fileSize = fileInfo.Length;

            // Build storage path (relative to base path)
            var storagePath = Path.GetRelativePath(_basePath, fullPath).Replace(Path.DirectorySeparatorChar, '/');

            // Generate public URL
            var publicUrl = $"{_baseUrl.TrimEnd('/')}/{storagePath}";

            _logger.LogInformation("Uploaded file to file system: {StoragePath}", storagePath);

            return new MediaStorageResult
            {
                StoragePath = storagePath,
                PublicUrl = publicUrl,
                FileSize = fileSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to file system: {FileName}", fileName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, storagePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {storagePath}", fullPath);
            }

            var memoryStream = new MemoryStream();
            await using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
            }
            
            memoryStream.Position = 0;
            
            _logger.LogInformation("Downloaded file from file system: {StoragePath}", storagePath);
            
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from file system: {StoragePath}", storagePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, storagePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file from file system: {StoragePath}", storagePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from file system: {StoragePath}", storagePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> GetPublicUrlAsync(
        string storagePath,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // File system storage doesn't support time-limited URLs
            // Always return the base public URL
            var publicUrl = $"{_baseUrl.TrimEnd('/')}/{storagePath}";
            
            if (expiration.HasValue)
            {
                _logger.LogWarning("FileSystem storage does not support time-limited URLs. Returning permanent URL for: {StoragePath}", storagePath);
            }

            return Task.FromResult(publicUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate public URL for file: {StoragePath}", storagePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, storagePath.Replace('/', Path.DirectorySeparatorChar));
            return Task.FromResult(File.Exists(fullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if file exists: {StoragePath}", storagePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<long?> GetFileSizeAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, storagePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
            {
                return Task.FromResult<long?>(null);
            }

            var fileInfo = new FileInfo(fullPath);
            return Task.FromResult<long?>(fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file size: {StoragePath}", storagePath);
            throw;
        }
    }
}
