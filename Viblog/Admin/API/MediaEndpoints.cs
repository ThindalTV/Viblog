using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Facades;
using Viblog.Shared.Configuration;

namespace Viblog.Admin.API;

/// <summary>
/// API endpoints for media library management
/// </summary>
public static class MediaEndpoints
{
    /// <summary>
    /// Maps all media-related API endpoints
    /// </summary>
    public static RouteGroupBuilder MapMediaEndpoints(this RouteGroupBuilder group)
    {
        // Upload endpoints
        group.MapPost("/upload", UploadFileAsync)
            .WithName("UploadMedia")
            .WithDescription("Upload a media file")
            .RequireAuthorization("Admin")
            .DisableAntiforgery(); // Required for file uploads

        group.MapPost("/upload/remove", RemoveUploadedFileAsync)
            .WithName("RemoveUploadedMedia")
            .WithDescription("Remove a recently uploaded file")
            .RequireAuthorization("Admin")
            .DisableAntiforgery();

        // CRUD endpoints
        group.MapGet("/{id}", GetMediaItemAsync)
            .WithName("GetMediaItem")
            .WithDescription("Get a media item by ID");

        group.MapGet("/", GetMediaItemsAsync)
            .WithName("GetMediaItems")
            .WithDescription("Get media items with optional filtering");

        group.MapPut("/{id}/metadata", UpdateMetadataAsync)
            .WithName("UpdateMediaMetadata")
            .WithDescription("Update media item metadata")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id}", DeleteMediaItemAsync)
            .WithName("DeleteMediaItem")
            .WithDescription("Delete a media item")
            .RequireAuthorization("Admin");

        // Bulk operations
        group.MapPost("/bulk-delete", BulkDeleteAsync)
            .WithName("BulkDeleteMedia")
            .WithDescription("Delete multiple media items")
            .RequireAuthorization("Admin");

        return group;
    }

    /// <summary>
    /// Upload a file to the media library (Telerik Upload compatible)
    /// </summary>
    private static async Task<Results<Ok<MediaItem>, BadRequest<string>>> UploadFileAsync(
        [FromForm] IFormFile file,
        [FromQuery] string? folderPath,
        [FromQuery] string? uploadedBy,
        [FromQuery] string? title,
        [FromQuery] string? description,
        [FromQuery] string? altText,
        [FromServices] IMediaFacade mediaFacade,
        [FromServices] IOptions<MediaLibrarySettings> settings,
        [FromServices] ILogger<IMediaFacade> logger)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return TypedResults.BadRequest("No file uploaded");
            }

            var uploadSettings = settings.Value.Upload;

            // Check file size
            if (file.Length > uploadSettings.MaxFileSizeBytes)
            {
                return TypedResults.BadRequest($"File size exceeds maximum allowed size of {uploadSettings.MaxFileSizeMB}MB");
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName);
            if (!uploadSettings.IsFileTypeAllowed(extension))
            {
                return TypedResults.BadRequest($"File type '{extension}' is not allowed");
            }

            // Check MIME type
            if (!uploadSettings.IsMimeTypeAllowed(file.ContentType))
            {
                return TypedResults.BadRequest($"MIME type '{file.ContentType}' is not allowed");
            }

            // Upload file
            using var stream = file.OpenReadStream();
            var result = await mediaFacade.UploadAsync(
                file.FileName,
                stream,
                file.ContentType,
                folderPath ?? DateTimeOffset.UtcNow.ToString("yyyyMM"));

            // Update metadata if provided
            if (!string.IsNullOrEmpty(title))
                result.Title = title;
            if (!string.IsNullOrEmpty(description))
                result.Description = description;
            if (!string.IsNullOrEmpty(altText))
                result.AltText = altText;

            logger.LogInformation("File uploaded successfully: {FileName} by {UploadedBy}", file.FileName, uploadedBy ?? "Anonymous");

            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return TypedResults.BadRequest($"Upload failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove a file from the media library (Telerik Upload compatible)
    /// </summary>
    private static async Task<Results<Ok, NotFound, BadRequest<string>>> RemoveUploadedFileAsync(
        [FromForm] string file,
        [FromServices] IMediaFacade mediaFacade,
        [FromServices] ILogger<IMediaFacade> logger)
    {
        try
        {
            if (string.IsNullOrEmpty(file))
            {
                return TypedResults.BadRequest("No file name provided");
            }

            // The file parameter from Telerik Upload contains the original filename
            // We need to find the media item by filename and delete it
            logger.LogInformation("Remove request for file: {FileName}", file);

            // Search for the file by name
            var allItems = await mediaFacade.GetMediaItemsAsync(null, new PagingParameters { PageNumber = 1, PageSize = 1000 }, default);
            var item = allItems.Items.FirstOrDefault(i => i.FileName == file);

            if (item == null)
            {
                logger.LogWarning("File not found for removal: {FileName}", file);
                return TypedResults.NotFound();
            }

            var success = await mediaFacade.DeleteAsync(item.Id, item.GroupKey);

            if (success)
            {
                logger.LogInformation("File removed successfully: {FileName}", file);
                return TypedResults.Ok();
            }

            return TypedResults.BadRequest($"Failed to remove file: {file}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing file: {FileName}", file);
            return TypedResults.BadRequest($"Remove failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get a single media item by ID
    /// </summary>
    private static async Task<Results<Ok<MediaItem>, NotFound>> GetMediaItemAsync(
        string id,
        [FromServices] IMediaFacade mediaFacade)
    {
        // Use GetByIdAsync which is now available on IMediaFacade
        // Note: We need to know the partition key - for now we'll search
        var items = await mediaFacade.GetMediaItemsAsync(null, new PagingParameters { PageNumber = 1, PageSize = 1000 }, default);
        var item = items.Items.FirstOrDefault(i => i.Id == id);
        return item != null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    /// <summary>
    /// Get media items with optional filtering and pagination
    /// </summary>
    private static async Task<Ok<PagedResult<MediaItem>>> GetMediaItemsAsync(
        [FromQuery] string? mimeTypeFilter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromServices] IMediaFacade mediaFacade = default!)
    {
        var paging = new PagingParameters { PageNumber = page, PageSize = pageSize };
        var result = await mediaFacade.GetMediaItemsAsync(mimeTypeFilter, paging, default);
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Update metadata for a media item
    /// </summary>
    private static async Task<Results<Ok<MediaItem>, NotFound, BadRequest<string>>> UpdateMetadataAsync(
        string id,
        [FromBody] UpdateMetadataRequest request,
        [FromServices] IMediaFacade mediaFacade,
        [FromServices] ILogger<IMediaFacade> logger)
    {
        try
        {
            // Find the item first to get the partition key
            var items = await mediaFacade.GetMediaItemsAsync(null, new PagingParameters { PageNumber = 1, PageSize = 1000 }, default);
            var item = items.Items.FirstOrDefault(i => i.Id == id);
            
            if (item == null)
            {
                return TypedResults.NotFound();
            }

            var updated = await mediaFacade.UpdateMetadataAsync(
                id,
                item.GroupKey,
                request.Title,
                request.Description,
                request.AltText);

            return updated != null ? TypedResults.Ok(updated) : TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating metadata for item: {Id}", id);
            return TypedResults.BadRequest($"Update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a media item
    /// </summary>
    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> DeleteMediaItemAsync(
        string id,
        [FromServices] IMediaFacade mediaFacade,
        [FromServices] ILogger<IMediaFacade> logger)
    {
        try
        {
            // Find the item first to get the partition key
            var items = await mediaFacade.GetMediaItemsAsync(null, new PagingParameters { PageNumber = 1, PageSize = 1000 }, default);
            var item = items.Items.FirstOrDefault(i => i.Id == id);
            
            if (item == null)
            {
                return TypedResults.NotFound();
            }

            var success = await mediaFacade.DeleteAsync(id, item.GroupKey);

            return success ? TypedResults.NoContent() : TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting item: {Id}", id);
            return TypedResults.BadRequest($"Delete failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete multiple items
    /// </summary>
    private static async Task<Results<Ok<int>, BadRequest<string>>> BulkDeleteAsync(
        [FromBody] BulkDeleteRequest request,
        [FromServices] IMediaFacade mediaFacade,
        [FromServices] ILogger<IMediaFacade> logger)
    {
        try
        {
            // Find all items
            var allItems = await mediaFacade.GetMediaItemsAsync(null, new PagingParameters { PageNumber = 1, PageSize = 1000 }, default);
            var items = allItems.Items.Where(i => request.ItemIds.Contains(i.Id)).ToList();

            if (items.Count == 0)
            {
                return TypedResults.BadRequest("No valid items found to delete");
            }

            var count = await mediaFacade.BulkDeleteAsync(items);

            return TypedResults.Ok(count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting items");
            return TypedResults.BadRequest($"Delete failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Request model for updating metadata
/// </summary>
public record UpdateMetadataRequest(
    string? Title,
    string? Description,
    string? AltText);

/// <summary>
/// Request model for bulk delete operation
/// </summary>
public record BulkDeleteRequest(
    List<string> ItemIds);
