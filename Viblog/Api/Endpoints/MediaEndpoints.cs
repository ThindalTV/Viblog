using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Facades;
using Viblog.Shared.Configuration;

namespace Viblog.Api.Endpoints;

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
        group.MapPost("/bulk-move", BulkMoveAsync)
            .WithName("BulkMoveMedia")
            .WithDescription("Move multiple media items to a different folder")
            .RequireAuthorization("Admin");

        group.MapPost("/bulk-delete", BulkDeleteAsync)
            .WithName("BulkDeleteMedia")
            .WithDescription("Delete multiple media items")
            .RequireAuthorization("Admin");

        // Folder endpoints
        group.MapGet("/folders", GetFoldersAsync)
            .WithName("GetMediaFolders")
            .WithDescription("Get all folder paths");

        return group;
    }

    /// <summary>
    /// Upload a file to the media library
    /// </summary>
    private static async Task<Results<Ok<MediaItem>, BadRequest<string>>> UploadFileAsync(
        IFormFile file,
        [FromQuery] string? folderPath,
        [FromQuery] string? title,
        [FromQuery] string? description,
        [FromQuery] string? altText,
        IMediaFacade mediaFacade,
        IOptions<MediaLibrarySettings> settings,
        ILogger<IMediaFacade> logger)
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
                folderPath ?? "/");

            // Update metadata if provided
            if (!string.IsNullOrEmpty(title))
                result.Title = title;
            if (!string.IsNullOrEmpty(description))
                result.Description = description;
            if (!string.IsNullOrEmpty(altText))
                result.AltText = altText;

            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return TypedResults.BadRequest($"Upload failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get a single media item by ID
    /// </summary>
    private static async Task<Results<Ok<MediaItem>, NotFound>> GetMediaItemAsync(
        string id,
        IMediaFacade mediaFacade)
    {
        // Find item by searching all items (not optimal but works for now)
        // TODO: Add GetByIdAsync to IMediaFacade
        var items = await mediaFacade.GetMediaItemsAsync(null, null, new PagingParameters { PageNumber = 1, PageSize = 1000 });
        var item = items.Items.FirstOrDefault(i => i.Id == id);
        return item != null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    /// <summary>
    /// Get media items with optional filtering and pagination
    /// </summary>
    private static async Task<Ok<PagedResult<MediaItem>>> GetMediaItemsAsync(
        [FromQuery] string? folderPath = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        IMediaFacade mediaFacade = default!)
    {
        var paging = new PagingParameters { PageNumber = page, PageSize = pageSize };
        var result = await mediaFacade.GetMediaItemsAsync(folderPath, searchTerm, paging);
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Update metadata for a media item
    /// </summary>
    private static async Task<Results<Ok<MediaItem>, NotFound, BadRequest<string>>> UpdateMetadataAsync(
        string id,
        [FromBody] UpdateMetadataRequest request,
        IMediaFacade mediaFacade,
        ILogger<IMediaFacade> logger)
    {
        try
        {
            // Find the item first
            var items = await mediaFacade.GetMediaItemsAsync(null, null, new PagingParameters { PageNumber = 1, PageSize = 1000 });
            var item = items.Items.FirstOrDefault(i => i.Id == id);
            
            if (item == null)
            {
                return TypedResults.NotFound();
            }

            item.Title = request.Title;
            item.Description = request.Description;
            item.AltText = request.AltText;

            // Note: MediaFacade doesn't have UpdateMetadataAsync yet - this will need to be added
            // For now, this is a placeholder

            return TypedResults.Ok(item);
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
        IMediaFacade mediaFacade,
        ILogger<IMediaFacade> logger)
    {
        try
        {
            // Find the item first
            var items = await mediaFacade.GetMediaItemsAsync(null, null, new PagingParameters { PageNumber = 1, PageSize = 1000 });
            var item = items.Items.FirstOrDefault(i => i.Id == id);
            
            if (item == null)
            {
                return TypedResults.NotFound();
            }

            await mediaFacade.BulkDeleteAsync(new List<MediaItem> { item });

            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting item: {Id}", id);
            return TypedResults.BadRequest($"Delete failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Move multiple items to a new folder
    /// </summary>
    private static async Task<Results<Ok<int>, BadRequest<string>>> BulkMoveAsync(
        [FromBody] BulkMoveRequest request,
        IMediaFacade mediaFacade,
        ILogger<IMediaFacade> logger)
    {
        try
        {
            // Find all items
            var allItems = await mediaFacade.GetMediaItemsAsync(null, null, new PagingParameters { PageNumber = 1, PageSize = 1000 });
            var items = allItems.Items.Where(i => request.ItemIds.Contains(i.Id)).ToList();

            if (items.Count == 0)
            {
                return TypedResults.BadRequest("No valid items found to move");
            }

            var count = await mediaFacade.BulkMoveAsync(items, request.TargetFolderPath);

            return TypedResults.Ok(count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error moving items to folder: {Folder}", request.TargetFolderPath);
            return TypedResults.BadRequest($"Move failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete multiple items
    /// </summary>
    private static async Task<Results<Ok<int>, BadRequest<string>>> BulkDeleteAsync(
        [FromBody] BulkDeleteRequest request,
        IMediaFacade mediaFacade,
        ILogger<IMediaFacade> logger)
    {
        try
        {
            // Find all items
            var allItems = await mediaFacade.GetMediaItemsAsync(null, null, new PagingParameters { PageNumber = 1, PageSize = 1000 });
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

    /// <summary>
    /// Get all folder paths
    /// </summary>
    private static async Task<Ok<List<string>>> GetFoldersAsync(
        IMediaFacade mediaFacade)
    {
        var folders = await mediaFacade.GetAllFolderPathsAsync();
        return TypedResults.Ok(folders);
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
/// Request model for bulk move operation
/// </summary>
public record BulkMoveRequest(
    List<string> ItemIds,
    string TargetFolderPath);

/// <summary>
/// Request model for bulk delete operation
/// </summary>
public record BulkDeleteRequest(
    List<string> ItemIds);
