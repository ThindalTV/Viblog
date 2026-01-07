using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Infrastructure.Shared.Services;

/// <summary>
/// Service interface for media management operations
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Upload a media file with automatic metadata extraction
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
    /// Upload a media file with custom metadata
    /// </summary>
    /// <param name="fileName">Original filename</param>
    /// <param name="fileStream">File content stream</param>
    /// <param name="mimeType">MIME type</param>
    /// <param name="folderPath">Virtual folder path</param>
    /// <param name="title">Custom title</param>
    /// <param name="description">Custom description</param>
    /// <param name="altText">Custom alt text</param>
    /// <param name="uploadedBy">User who uploaded the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created media item</returns>
    Task<MediaItem> UploadAsync(
        string fileName,
        Stream fileStream,
        string mimeType,
        string folderPath,
        string? title,
        string? description,
        string? altText,
        string? uploadedBy = null,
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
    /// Delete a media item (soft delete)
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
    /// Get public URL for a media item with optional expiration
    /// </summary>
    /// <param name="mediaItem">The media item</param>
    /// <param name="expiration">Optional expiration time for time-limited access</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL</returns>
    Task<string> GetPublicUrlAsync(
        MediaItem mediaItem,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update metadata for a media item
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
