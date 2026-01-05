using Viblog.Shared.Data.Common;
using Viblog.Shared.Data.Entities;

namespace Viblog.Shared.Facades;

/// <summary>
/// Facade interface for media library operations
/// </summary>
public interface IMediaFacade
{
    /// <summary>
    /// Upload a single media file
    /// </summary>
    /// <param name="fileName">Original filename</param>
    /// <param name="fileStream">File content stream</param>
    /// <param name="mimeType">MIME type</param>
    /// <param name="folderPath">Virtual folder path</param>
    /// <param name="uploadedBy">User who uploaded the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created media item</returns>
    Task<MediaItem> UploadAsync(
        string fileName,
        Stream fileStream,
        string mimeType,
        string folderPath,
        string? uploadedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload multiple media files in bulk
    /// </summary>
    /// <param name="files">Collection of files to upload</param>
    /// <param name="folderPath">Virtual folder path</param>
    /// <param name="uploadedBy">User who uploaded the files</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of created media items</returns>
    Task<List<MediaItem>> BulkUploadAsync(
        IEnumerable<(string FileName, Stream FileStream, string MimeType)> files,
        string folderPath,
        string? uploadedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get media items with folder filtering and paging
    /// </summary>
    /// <param name="folderPath">Optional folder path filter</param>
    /// <param name="mimeTypeFilter">Optional MIME type filter</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of media items</returns>
    Task<PagedResult<MediaItem>> GetMediaItemsAsync(
        string? folderPath = null,
        string? mimeTypeFilter = null,
        PagingParameters? pagingParameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Move a media item to a different folder
    /// </summary>
    /// <param name="id">Media item ID</param>
    /// <param name="partitionKey">Partition key</param>
    /// <param name="newFolderPath">New folder path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated media item or null if not found</returns>
    Task<MediaItem?> MoveToFolderAsync(
        string id,
        string partitionKey,
        string newFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Move multiple media items to a different folder in bulk
    /// </summary>
    /// <param name="items">Media items to move</param>
    /// <param name="newFolderPath">New folder path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items successfully moved</returns>
    Task<int> BulkMoveAsync(
        IEnumerable<MediaItem> items,
        string newFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a media item
    /// </summary>
    /// <param name="id">Media item ID</param>
    /// <param name="partitionKey">Partition key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete multiple media items in bulk
    /// </summary>
    /// <param name="items">Media items to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items successfully deleted</returns>
    Task<int> BulkDeleteAsync(
        IEnumerable<MediaItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search media items by filename, title, or description
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of matching media items</returns>
    Task<PagedResult<MediaItem>> SearchAsync(
        string searchTerm,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all unique folder paths from the media library
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unique folder paths</returns>
    Task<List<string>> GetAllFolderPathsAsync(
        CancellationToken cancellationToken = default);
}
