using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Infrastructure.Facades;

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
    /// Get media items with optional MIME type filtering and paging
    /// </summary>
    /// <param name="mimeTypeFilter">Optional MIME type filter</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of media items</returns>
    Task<PagedResult<MediaItem>> GetMediaItemsAsync(
        string? mimeTypeFilter,
        PagingParameters pagingParameters,
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
    /// Bulk delete multiple media items
    /// </summary>
    /// <param name="items">Media items to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items successfully deleted</returns>
    Task<int> BulkDeleteAsync(
        IEnumerable<MediaItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search media items
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="pagingParameters">Paging parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of matching media items</returns>
    Task<PagedResult<MediaItem>> SearchAsync(
        string searchTerm,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get public URL for a media item
    /// </summary>
    /// <param name="mediaItem">Media item</param>
    /// <param name="expiration">Optional expiration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL</returns>
    Task<string> GetPublicUrlAsync(
        MediaItem mediaItem,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a media item by ID
    /// </summary>
    /// <param name="id">Media item ID</param>
    /// <param name="partitionKey">Partition key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Media item or null if not found</returns>
    Task<MediaItem?> GetByIdAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update media item metadata
    /// </summary>
    /// <param name="id">Media item ID</param>
    /// <param name="partitionKey">Partition key</param>
    /// <param name="title">New title</param>
    /// <param name="description">New description</param>
    /// <param name="altText">New alt text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated media item or null if not found</returns>
    Task<MediaItem?> UpdateMetadataAsync(
        string id,
        string partitionKey,
        string? title,
        string? description,
        string? altText,
        CancellationToken cancellationToken = default);
}
