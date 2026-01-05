# Vilog Media Manager: Complete Guide

## Overview

The Vilog Media Manager is a comprehensive media asset management system designed to handle all types of media files including images, videos, PDFs, and other documents. It provides a flexible, storage-agnostic architecture that allows administrators to upload, organize, preview, and manage media assets efficiently while supporting multiple storage backends.

**Implementation Plan:** [Media Manager Implementation Plan](../Plans/mediamanager.md)

**Key Design Principles:**
- Path-based organization (no separate folder entities)
- No tagging system (organize by paths instead)
- Storage-agnostic metadata repository
- Full-sized images with CSS rescaling (no thumbnail generation)

## Table of Contents

1. [Media Manager Lifecycle](#media-manager-lifecycle)
2. [Media Item Structure](#media-item-structure)
3. [Storage Architecture](#storage-architecture)
4. [Upload and Management](#upload-and-management)
5. [Preview and Display](#preview-and-display)
6. [Technical Implementation](#technical-implementation)
7. [API Reference](#api-reference)

---

## Media Manager Lifecycle

### Media States

Media items in Vilog can exist in several states throughout their lifecycle:

#### 1. Uploading
- **Status**: `Uploading`
- File transfer in progress
- Progress tracking available
- Can be cancelled
- Temporary storage until complete

#### 2. Available
- **Status**: `Available`
- Upload complete
- Ready for use in content
- Can be previewed
- Can be moved or renamed

#### 3. In Use
- **Status**: `InUse`
- Referenced by one or more blog posts
- Tracks usage count
- Prevents accidental deletion
- Can still be moved/renamed

#### 4. Deleted
- **Status**: `Deleted`
- Soft-deleted from media library
- Not visible in UI
- Can be restored (within retention period)
- Hard-deleted after retention period

### Workflow

```
Upload ? Available ? In Use
           ?           ?
           ?????????????
                 ?
              Deleted
```

**Typical Workflow:**
1. **Upload** files via drag-and-drop or file picker
2. **Available** immediately after upload (metadata extracted)
3. **Organize** by moving to different folder paths
4. **Use** in blog posts or pages
5. **Delete** when no longer needed

---

## Media Item Structure

### Core Properties

#### Id
- **Required**: Yes (auto-generated)
- **Type**: String (GUID)
- **Purpose**: Unique identifier
- **Usage**: References in content, tracking

#### FileName
- **Required**: Yes
- **Type**: String
- **Purpose**: Original file name
- **Example**: `"blazor-architecture.png"`
- **Constraints**: Must be valid file name

#### FileExtension
- **Required**: Yes (auto-detected)
- **Type**: String
- **Purpose**: File type identifier
- **Example**: `".png"`, `".pdf"`, `".mp4"`
- **Usage**: MIME type detection, icon selection

#### FileSize
- **Required**: Yes (from storage)
- **Type**: Long (bytes)
- **Purpose**: Storage tracking, display
- **Display**: Formatted as "2.5 MB", "1.2 GB"
- **Validation**: Maximum file size limits

#### MimeType
- **Required**: Yes (auto-detected)
- **Type**: String
- **Purpose**: Content type specification
- **Example**: `"image/png"`, `"application/pdf"`, `"video/mp4"`
- **Usage**: Browser rendering, download headers

### Storage Properties

#### StoragePath
- **Type**: String
- **Purpose**: Storage-provider specific location identifier
- **Examples**:
  - Blob Storage: `"2024/03/abc123-image.png"` (blob name)
  - File System: `"2024/03/abc123-image.png"` (relative path)
  - SQL Server: `"abc123"` (row ID)
- **Security**: Never exposed to public
- **Note**: Format and meaning determined by storage provider

#### PublicUrl
- **Type**: String
- **Purpose**: Public-facing URL for accessing the file
- **Example**: `"https://cdn.vilog.com/media/2024/03/abc123-image.png"`
- **Usage**: Serving content to users
- **Generation**: Created by storage provider on upload

#### PreviewUrl
- **Type**: String (nullable)
- **Purpose**: URL to preview image (full-sized for images, icon for others)
- **Examples**:
  - Image: Same as PublicUrl (rescaled in display via CSS/HTML)
  - PDF: Icon URL (`"/icons/file-pdf.svg"`)
  - Video: Icon URL (`"/icons/file-video.svg"`)
  - Unknown: Icon URL (`"/icons/file-unknown.svg"`)
- **Note**: For images, PreviewUrl equals PublicUrl; display size controlled by UI

### Organization Properties

#### FolderPath
- **Type**: String
- **Purpose**: Full path representing logical folder structure
- **Example**: `"/images/blog/2024/"`, `"/documents/pdfs/"`
- **Default**: `"/"` (root)
- **Usage**: Path-based hierarchical organization
- **Note**: Folders are derived from paths, no separate folder entities

### Metadata Properties

#### Title
- **Type**: String (nullable)
- **Purpose**: Descriptive name (alternative to filename)
- **Example**: `"Blazor Architecture Diagram"`
- **Usage**: Display in media library, search

#### Description
- **Type**: String (nullable)
- **Purpose**: Detailed information about the media
- **Usage**: Alt text for images, tooltips, search

#### AltText
- **Type**: String (nullable)
- **Purpose**: Accessibility text for images
- **SEO Impact**: Important for image search
- **Best Practice**: Descriptive, concise (125 chars)

#### Width & Height
- **Type**: Integer (nullable)
- **Purpose**: Image/video dimensions
- **Auto-detected**: For images and videos
- **Usage**: Responsive rendering, layout

#### AdditionalMetadata
- **Type**: Dictionary<string, string>
- **Purpose**: Storage-provider specific or file-type specific metadata
- **Examples**:
  - Image: `{ "ColorSpace": "sRGB", "DPI": "72" }`
  - Video: `{ "Duration": "180", "Codec": "H.264", "Bitrate": "2500" }`
  - PDF: `{ "PageCount": "42", "Author": "John Doe" }`
  - Audio: `{ "Duration": "245", "Bitrate": "320", "Artist": "..." }`
- **Note**: All values stored as strings; parse as needed

### Usage Tracking

#### UsageCount
- **Type**: Integer
- **Default**: 0
- **Purpose**: Number of times referenced in content
- **Auto-updated**: When used in posts/pages
- **Usage**: Prevents deletion of active media

#### LastAccessedAt
- **Type**: DateTimeOffset (nullable)
- **Purpose**: Track file access
- **Usage**: Archive candidates, cleanup

#### UploadedBy
- **Type**: String (User ID)
- **Purpose**: Track who uploaded
- **Usage**: Audit trail, permissions (future)

### Status Properties

#### Status
- **Type**: Enum
- **Values**: `Uploading`, `Available`, `InUse`, `Deleted`
- **Purpose**: Lifecycle state tracking
- **See**: [Media States](#media-states)

#### ErrorMessage
- **Type**: String (nullable)
- **Purpose**: Error information if upload/processing failed
- **Usage**: User feedback, debugging

### Audit Fields (from BaseEntity)

#### PartitionKey
- **Type**: String
- **Purpose**: CosmosDB partitioning
- **Strategy**: Year-month of upload (e.g., `"2024-03"`)
- **Auto-set**: In `UpdatePartitionKey()` method

#### CreatedAt
- **Type**: DateTimeOffset
- **Auto-set**: On upload
- **Purpose**: Upload timestamp

#### UpdatedAt
- **Type**: DateTimeOffset
- **Auto-update**: On any change
- **Purpose**: Last modified tracking

#### IsDeleted / DeletedAt
- **Purpose**: Soft delete support
- **See**: [Media States](#media-states)

---

## Storage Architecture

### Storage Provider Abstraction

The Media Manager uses a clean separation between **storage operations** (physical file management) and **metadata operations** (database records). This follows Vilog's repository pattern while allowing pluggable storage backends.

### Architecture Layers

```
????????????????????????
?  Blazor Components   ?  Display Layer
????????????????????????
          ?
????????????????????????
?   MediaFacade        ?  Business Logic (orchestrates both layers)
????????????????????????
          ?
    ?????????????
    ?           ?
??????????  ????????????????
?Metadata?  ? MediaService ?  Services
?Repository  ?              ?
??????????  ????????????????
    ?           ?
??????????  ??????????????????
?CosmosDB?  ?Storage Provider?  Data Access
??????????  ?  (IMediaStorage?
            ?    Repository) ?
            ??????????????????
                 ?
            ??????????????????
            ?Physical Storage?  Storage
            ??????????????????
```

**Key Points:**
- **MediaFacade**: Main entry point for media library management features (browse, organize, search)
- **MediaService**: Reusable service for uploading media from anywhere in the application (e.g., blog post editor, profile pictures)
- **IMediaMetadataRepository**: Manages MediaItem metadata in CosmosDB
- **IMediaStorageRepository**: Provider-specific file storage operations

**MediaService Usage Examples:**
```csharp
// From MediaFacade (media library feature)
var result = await _mediaFacade.UploadAsync(...);

// From BlogPostEditor (inline image upload with metadata)
var mediaItem = await _mediaService.UploadAsync(stream, "post-image.png", "image/png", "/posts/my-post/");

// From ProfileService (profile picture upload)
var mediaItem = await _mediaService.UploadAsync(stream, "profile-pic.jpg", "image/jpeg", "/profile-pictures/");
```

### Key Concepts

- **Storage Provider**: A module or service that physically stores media files. Examples include cloud storage (e.g., AWS S3, Azure Blob Storage), local file systems, and database BLOBs.
- **Storage Path**: The path or identifier used by the storage provider to locate a media file. This is usually opaque to the application and is only used by the storage provider interface.
- **Public URL**: A URL that can be used to directly access a media file over the internet. This is typically generated by the storage provider when a file is uploaded.
- **Preview URL**: For images, equals the PublicUrl (rescaled in display via CSS). For other file types, points to a static icon file.

### IMediaMetadataRepository Interface

**Purpose**: Manages MediaItem entities in CosmosDB (metadata only, no file content)

**Inherits**: `IRepository<MediaItem>`

```csharp
public interface IMediaMetadataRepository : IRepository<MediaItem>
{
    // Get items in a specific folder path
    Task<PagedResult<MediaItem>> GetItemsInFolderAsync(
        string folderPath,
        PagingParameters pagingParameters,
        MediaItemFilter? filter = null,
        CancellationToken cancellationToken = default);
    
    // Get items by type
    Task<PagedResult<MediaItem>> GetItemsByTypeAsync(
        MediaType mediaType,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);
    
    // Search media items (by filename, title, description)
    Task<PagedResult<MediaItem>> SearchAsync(
        string searchTerm,
        PagingParameters pagingParameters,
        MediaItemFilter? filter = null,
        CancellationToken cancellationToken = default);
    
    // Get by storage path (for provider operations)
    Task<MediaItem?> GetByStoragePathAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
    
    // Get items in use (referenced by content)
    Task<PagedResult<MediaItem>> GetItemsInUseAsync(
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);
    
    // Get unused items (candidates for cleanup)
    Task<PagedResult<MediaItem>> GetUnusedItemsAsync(
        int olderThanDays,
        PagingParameters pagingParameters,
        CancellationToken cancellationToken = default);
    
    // Get all unique folder paths (for folder browser)
    Task<List<string>> GetAllFolderPathsAsync(
        CancellationToken cancellationToken = default);
    
    // Update usage count
    Task UpdateUsageCountAsync(
        string id,
        string partitionKey,
        int delta,
        CancellationToken cancellationToken = default);
}
```

### IMediaStorageRepository Interface

**Purpose**: Manages physical file storage operations only (no metadata)

**Note**: This is storage-provider specific. Different implementations for Blob/FileSystem/SQL.

```csharp
public interface IMediaStorageRepository
{
    /// <summary>
    /// Upload a file and return storage path and public URL
    /// </summary>
    Task<MediaStorageResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Download a file from storage
    /// </summary>
    Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a file from storage
    /// </summary>
    Task<bool> DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Move a file within storage
    /// </summary>
    Task<string> MoveAsync(
        string currentPath,
        string newPath,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a public URL for accessing the file
    /// </summary>
    Task<string> GetPublicUrlAsync(
        string storagePath,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get file size without downloading
    /// </summary>
    Task<long> GetFileSizeAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
}
```

**Return Type**:
```csharp
public class MediaStorageResult
{
    public string StoragePath { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
```

### Storage Repository Implementations

#### 1. BlobStorageRepository

**Use Case**: Cloud-hosted, scalable, CDN integration

**Configuration**:
```json
{
  "MediaStorage": {
    "Provider": "BlobStorage",
    "BlobStorage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;...",
      "ContainerName": "vilog-media",
      "CdnUrl": "https://cdn.vilog.com"
    }
  }
}
```

**Features**:
- Unlimited scalability
- Built-in redundancy
- CDN integration
- Blob lifecycle management
- SAS token support for secure access

**Implementation**: `BlobStorageRepository : IMediaStorageRepository`

**Location**: `Vilog.Shared.Data.Repositories.Storage`

#### 2. FileSystemStorageRepository

**Use Case**: Local development, self-hosted, simple deployments

**Configuration**:
```json
{
  "MediaStorage": {
    "Provider": "FileSystem",
    "FileSystem": {
      "BasePath": "C:\\Vilog\\Media",
      "BaseUrl": "https://vilog.local/media"
    }
  }
}
```

**Features**:
- No external dependencies
- Direct file access
- Easy backup
- File system permissions

**Limitations**:
- No built-in CDN
- Limited scalability
- Server-bound storage

**Implementation**: `FileSystemStorageRepository : IMediaStorageRepository`

**Location**: `Vilog.Shared.Data.Repositories.Storage`

#### 3. SqlServerStorageRepository

**Use Case**: All-in-one database solution, enterprise compliance

**Configuration**:
```json
{
  "MediaStorage": {
    "Provider": "SqlServer",
    "SqlServer": {
      "ConnectionString": "Server=...;Database=VilogMedia;",
      "TableName": "MediaFiles",
      "MaxFileSize": 10485760
    }
  }
}
```

**Features**:
- FILESTREAM support for large files
- Transactional consistency
- Built-in backup
- Role-based security

**Limitations**:
- Database size considerations
- Performance at scale
- Backup complexity

**Implementation**: `SqlServerStorageRepository : IMediaStorageRepository`

**Location**: `Vilog.Shared.Data.Repositories.Storage`

### Storage Provider Selection

**Registration in Program.cs**:
```csharp
// Register metadata repository (always CosmosDB)
builder.Services.AddScoped<IMediaMetadataRepository, MediaMetadataRepository>();

// Register storage repository (based on configuration)
builder.Services.AddMediaStorage(configuration);
```

**Extension Method**:
```csharp
public static IServiceCollection AddMediaStorage(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    var provider = configuration["MediaStorage:Provider"];
    
    return provider switch
    {
        "BlobStorage" => services.AddScoped<IMediaStorageRepository, BlobStorageRepository>(),
        "FileSystem" => services.AddScoped<IMediaStorageRepository, FileSystemStorageRepository>(),
        "SqlServer" => services.AddScoped<IMediaStorageRepository, SqlServerStorageRepository>(),
        _ => throw new InvalidOperationException($"Unknown storage provider: {provider}")
    };
}
```

### Key Design Principles

1. **Storage Path is Provider-Agnostic**: 
   - Stored as opaque string in `MediaItem.StoragePath`
   - Format determined by storage provider
   - Never parsed by application code

2. **Metadata Repository Owns MediaItem**:
   - All entity operations through `IMediaMetadataRepository`
   - Standard repository pattern with CosmosDB
   - Follows existing Vilog patterns

3. **Storage Repository Owns Physical Files**:
   - All file operations through `IMediaStorageRepository`
   - Implementation-specific storage paths
   - No database operations

4. **MediaService Wraps Both Repositories**:
   - Provides simplified interface for uploading media with metadata
   - Can be used independently from MediaFacade
   - Used by other features (blog post editor, profile pictures, etc.)
   - Creates MediaItem with metadata, not just file upload

5. **Facade Orchestrates Both**:
   - `MediaFacade` coordinates metadata + storage
   - Ensures consistency between layers
   - Handles transactions and error recovery

6. **No Storage-Specific Data in Entity**:
   - `MediaItem` contains only generic properties
   - `AdditionalMetadata` dictionary for provider-specific data
   - All providers use same entity model

---

## Media Service

### IMediaService Interface

**Purpose**: Simplified media upload interface that can be used throughout the application. Unlike MediaFacade, this is focused on quick uploads from anywhere (blog posts, profiles, etc.) with automatic metadata creation.

**Location**: `Vilog.Shared.Services`

```csharp
public interface IMediaService
{
    /// <summary>
    /// Upload a file with automatic metadata creation
    /// </summary>
    Task<MediaItem> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folderPath = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Upload a file with custom metadata
    /// </summary>
    Task<MediaItem> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folderPath,
        MediaUploadMetadata? metadata,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a media item by ID
    /// </summary>
    Task<MediaItem?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a media item (soft delete)
    /// </summary>
    Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a time-limited public URL for a file
    /// </summary>
    Task<string> GetPublicUrlAsync(
        string id,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update media item metadata
    /// </summary>
    Task<MediaItem> UpdateMetadataAsync(
        string id,
        MediaMetadataUpdate update,
        CancellationToken cancellationToken = default);
}

public class MediaUploadMetadata
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AltText { get; set; }
}
```

### MediaService Implementation

```csharp
public class MediaService : IMediaService
{
    private readonly IMediaStorageRepository _storageRepository;
    private readonly IMediaMetadataRepository _metadataRepository;
    private readonly IMetadataExtractorService _metadataExtractor;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IMediaStorageRepository storageRepository,
        IMediaMetadataRepository metadataRepository,
        IMetadataExtractorService metadataExtractor,
        ILogger<MediaService> logger)
    {
        _storageRepository = storageRepository;
        _metadataRepository = metadataRepository;
        _metadataExtractor = metadataExtractor;
        _logger = logger;
    }

    public async Task<MediaItem> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folderPath = null,
        CancellationToken cancellationToken = default)
    {
        return await UploadAsync(fileStream, fileName, contentType, folderPath, null, cancellationToken);
    }

    public async Task<MediaItem> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folderPath,
        MediaUploadMetadata? metadata,
        CancellationToken cancellationToken = default)
    {
        // 1. Upload file to storage
        var storageResult = await _storageRepository.UploadAsync(
            fileStream, fileName, contentType, cancellationToken);
        
        // 2. Extract metadata (dimensions, duration, etc.)
        fileStream.Position = 0; // Reset stream
        var extractedMetadata = await _metadataExtractor.ExtractMetadataAsync(
            fileStream, contentType, cancellationToken);
        
        // 3. Determine preview URL
        string? previewUrl = contentType.StartsWith("image/")
            ? storageResult.PublicUrl
            : MediaIconHelper.GetFileTypeIcon(contentType);
        
        // 4. Create MediaItem entity
        var mediaItem = new MediaItem
        {
            FileName = fileName,
            FileExtension = Path.GetExtension(fileName),
            FileSize = storageResult.FileSize,
            MimeType = contentType,
            StoragePath = storageResult.StoragePath,
            PublicUrl = storageResult.PublicUrl,
            PreviewUrl = previewUrl,
            FolderPath = folderPath ?? "/",
            Title = metadata?.Title,
            Description = metadata?.Description,
            AltText = metadata?.AltText,
            Width = GetIntMetadata(extractedMetadata, "Width"),
            Height = GetIntMetadata(extractedMetadata, "Height"),
            AdditionalMetadata = extractedMetadata,
            Status = MediaStatus.Available
        };
        mediaItem.UpdatePartitionKey();
        
        // 5. Save to metadata repository
        await _metadataRepository.AddAsync(mediaItem);
        await _metadataRepository.SaveChangesAsync(cancellationToken);
        
        return mediaItem;
    }

    public async Task<MediaItem?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        // Assume partition key is based on creation date (year-month)
        // In practice, you might need to query without partition key or maintain an index
        return await _metadataRepository.GetByIdAsync(id, GetPartitionKey(id), cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item == null) return false;
        
        // Soft delete
        item.Status = MediaStatus.Deleted;
        item.DeletedAt = DateTimeOffset.UtcNow;
        
        await _metadataRepository.UpdateAsync(item);
        await _metadataRepository.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    public async Task<string> GetPublicUrlAsync(
        string id,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item == null)
            throw new NotFoundException($"Media item {id} not found");
        
        if (expiresIn.HasValue)
        {
            return await _storageRepository.GetPublicUrlAsync(
                item.StoragePath, expiresIn, cancellationToken);
        }
        
        return item.PublicUrl;
    }

    public async Task<MediaItem> UpdateMetadataAsync(
        string id,
        MediaMetadataUpdate update,
        CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item == null)
            throw new NotFoundException($"Media item {id} not found");
        
        if (update.Title != null) item.Title = update.Title;
        if (update.Description != null) item.Description = update.Description;
        if (update.AltText != null) item.AltText = update.AltText;
        
        await _metadataRepository.UpdateAsync(item);
        await _metadataRepository.SaveChangesAsync(cancellationToken);
        
        return item;
    }
    
    private static int? GetIntMetadata(Dictionary<string, string> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value) && int.TryParse(value, out var intValue)
            ? intValue
            : null;
    }
    
    private static string GetPartitionKey(string id)
    {
        // This is a simplified version - in practice you'd need proper partition key resolution
        return DateTime.UtcNow.ToString("yyyy-MM");
    }
}
```

### Usage Examples

#### Blog Post Inline Image Upload

```csharp
public class BlogPostEditor
{
    private readonly IMediaService _mediaService;

    public async Task<string> HandleImagePasteAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        // Upload image with automatic metadata creation
        var mediaItem = await _mediaService.UploadAsync(
            imageStream,
            fileName,
            "image/png",
            $"/posts/{CurrentPostSlug}/images/",
            new MediaUploadMetadata
            {
                Description = $"Image for blog post: {CurrentPostTitle}"
            },
            cancellationToken);
        
        // Return URL for markdown insertion
        return mediaItem.PublicUrl;
    }
}
```

#### Profile Picture Upload

```csharp
public class ProfileService
{
    private readonly IMediaService _mediaService;
    private readonly IUserRepository _userRepository;

    public async Task<string> UploadProfilePictureAsync(
        string userId,
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        // Upload to profile-pictures folder with metadata
        var mediaItem = await _mediaService.UploadAsync(
            imageStream,
            "profile-pic.jpg",
            "image/jpeg",
            "/profile-pictures/",
            new MediaUploadMetadata
            {
                Title = $"Profile picture for {userId}",
                AltText = "User profile picture"
            },
            cancellationToken);
        
        // Update user profile
        var user = await _userRepository.GetByIdAsync(userId, userId);
        user.ProfilePictureUrl = mediaItem.PublicUrl;
        user.ProfilePictureMediaId = mediaItem.Id;
        await _userRepository.UpdateAsync(user);
        
        return mediaItem.PublicUrl;
    }
}
```

#### Category Image Upload

```csharp
public class CategoryService
{
    private readonly IMediaService _mediaService;
    private readonly ICategoryRepository _categoryRepository;

    public async Task<Category> SetCategoryImageAsync(
        string categoryId,
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, categoryId);
        
        // Upload category banner image
        var mediaItem = await _mediaService.UploadAsync(
            imageStream,
            fileName,
            "image/jpeg",
            $"/categories/{category.Slug}/",
            new MediaUploadMetadata
            {
                Title = $"{category.Name} banner image",
                AltText = $"Banner for {category.Name} category"
            },
            cancellationToken);
        
        category.BannerImageUrl = mediaItem.PublicUrl;
        category.BannerImageMediaId = mediaItem.Id;
        await _categoryRepository.UpdateAsync(category);
        
        return category;
    }
}
```

#### Creating Blank Folders

Folders in Vilog are path-based and derived from media items. To support creating blank folders in the UI, we use a UI-only approach where folders are managed in component state until files are uploaded to them.

**UI-Only Folders Approach**

Manage folders only in the UI state without persisting until files are uploaded:

```razor
@* MediaLibrary.razor *@
@code {
    private List<string> _uiOnlyFolders = new();
    private List<string> _persistedFolders = new();
    
    protected override async Task OnInitializedAsync()
    {
        // Load persisted folders from media items
        _persistedFolders = await _metadataRepository.GetAllFolderPathsAsync();
    }
    
    private async Task CreateNewFolderAsync()
    {
        var newFolderName = await PromptForFolderNameAsync();
        var newFolderPath = $"{CurrentPath.TrimEnd('/')}/{newFolderName}/";
        
        // Add to UI-only folders (not persisted yet)
        _uiOnlyFolders.Add(newFolderPath);
        
        // Optionally enter rename mode immediately
        await EnterRenameMode(newFolderPath);
    }
    
    private async Task OnFileUploadedToFolder(string folderPath, MediaItem mediaItem)
    {
        // When file is uploaded to UI-only folder, it becomes persisted
        if (_uiOnlyFolders.Contains(folderPath))
        {
            _uiOnlyFolders.Remove(folderPath);
            _persistedFolders.Add(folderPath);
        }
    }
    
    private async Task DeleteEmptyFolderAsync(string folderPath)
    {
        // If UI-only folder, just remove from list
        if (_uiOnlyFolders.Contains(folderPath))
        {
            _uiOnlyFolders.Remove(folderPath);
            return;
        }
        
        // If persisted folder, check if it's truly empty
        var items = await _metadataRepository.GetItemsInFolderAsync(
            folderPath,
            new PagingParameters { PageNumber = 1, PageSize = 1 });
        
        if (items.TotalCount == 0)
        {
            // Folder is empty - just remove from UI
            // (it will disappear from persisted folders on next load)
            _persistedFolders.Remove(folderPath);
        }
        else
        {
            // Show error - folder contains files
            await ShowErrorAsync($"Cannot delete folder '{folderPath}' - it contains {items.TotalCount} file(s)");
        }
    }
    
    private List<string> GetAllFolders()
    {
        // Combine UI-only and persisted folders for display
        return _persistedFolders.Concat(_uiOnlyFolders).Distinct().OrderBy(f => f).ToList();
    }
    
    private bool IsUiOnlyFolder(string folderPath)
    {
        return _uiOnlyFolders.Contains(folderPath);
    }
}
```

**Folder Display Component:**

```razor
@* FolderCard.razor *@
<div class="folder-card @(IsUiOnly ? "folder-card--ui-only" : "")">
    <div class="folder-icon">
        <i class="icon-folder"></i>
    </div>
    <div class="folder-name">
        @FolderName
        @if (IsUiOnly)
        {
            <span class="badge badge--warning">New</span>
        }
    </div>
    <div class="folder-actions">
        <button @onclick="OnOpen">Open</button>
        <button @onclick="OnRename">Rename</button>
        <button @onclick="OnDelete">Delete</button>
    </div>
</div>

@code {
    [Parameter] public string FolderPath { get; set; } = string.Empty;
    [Parameter] public bool IsUiOnly { get; set; }
    [Parameter] public EventCallback<string> OnFolderOpen { get; set; }
    [Parameter] public EventCallback<string> OnFolderDelete { get; set; }
    
    private string FolderName => FolderPath.TrimEnd('/').Split('/').Last();
}
```

**Benefits:**
- Simple implementation - no database writes for empty folders
- Folders disappear on page refresh if no files added (expected behavior)
- Clear visual indicator (badge) for newly created folders
- No cleanup needed - UI-only folders are ephemeral by design
- Fast folder creation - no server round-trip

**User Experience:**
1. User clicks "New Folder" button
2. Folder appears with "New" badge and enters rename mode
3. User can upload files directly to the new folder
4. Once files are uploaded, folder becomes persisted and badge disappears
5. If user refreshes page before uploading files, UI-only folder disappears (expected)

**Edge Cases:**

```csharp
// Handle page refresh - UI-only folders are lost (expected)
protected override async Task OnInitializedAsync()
{
    _uiOnlyFolders.Clear(); // Fresh start
    _persistedFolders = await _metadataRepository.GetAllFolderPathsAsync();
}

// Handle folder rename for UI-only folders
private async Task RenameFolderAsync(string oldPath, string newPath)
{
    if (_uiOnlyFolders.Contains(oldPath))
    {
        // Just update the UI-only list
        _uiOnlyFolders.Remove(oldPath);
        _uiOnlyFolders.Add(newPath);
    }
    else
    {
        // Update all media items with this folder path
        var items = await _metadataRepository.GetItemsInFolderAsync(
            oldPath, 
            new PagingParameters { PageNumber = 1, PageSize = int.MaxValue });
        
        foreach (var item in items.Items)
        {
            item.FolderPath = newPath;
            await _metadataRepository.UpdateAsync(item);
        }
        await _metadataRepository.SaveChangesAsync();
        
        // Update persisted folders list
        _persistedFolders.Remove(oldPath);
        _persistedFolders.Add(newPath);
    }
}

// Handle drag-and-drop upload to UI-only folder
private async Task OnFilesDroppedAsync(string folderPath, IReadOnlyList<IBrowserFile> files)
{
    foreach (var file in files)
    {
        var mediaItem = await _mediaService.UploadAsync(
            file.OpenReadStream(),
            file.Name,
            file.ContentType,
            folderPath);
        
        // Promote UI-only folder to persisted
        await OnFileUploadedToFolder(folderPath, mediaItem);
    }
}
```

---

## Upload and Management

### Services

#### MetadataExtractorService

**Purpose**: Extract basic metadata from media files

**Methods**:
```csharp
public interface IMetadataExtractorService
{
    /// <summary>
    /// Extract metadata from a file stream
    /// </summary>
    Task<Dictionary<string, string>> ExtractMetadataAsync(
        Stream fileStream,
        string mimeType,
        CancellationToken cancellationToken = default);
}
```

**Extracted Metadata**:
- **Images**: Width, height, color space, DPI
- **Videos**: Duration (seconds), resolution, codec, bitrate
- **PDFs**: Page count, author, creation date
- **Audio**: Duration, bitrate, artist, album (if available)
- **Documents**: Author, creation date, page count (if applicable)

**Storage**: All metadata stored in `MediaItem.AdditionalMetadata` dictionary as string key-value pairs

#### Icon Helpers

Static helper methods for determining file type icons:

```csharp
public static class MediaIconHelper
{
    public static string? GetFileTypeIcon(string mimeType) => mimeType switch
    {
        string m when m.StartsWith("image/") => null, // Use actual image
        "application/pdf" => "/icons/file-pdf.svg",
        string m when m.StartsWith("video/") => "/icons/file-video.svg",
        string m when m.StartsWith("audio/") => "/icons/file-audio.svg",
        "application/msword" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" 
            => "/icons/file-document.svg",
        "application/vnd.ms-excel" or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" 
            => "/icons/file-spreadsheet.svg",
        "application/zip" or "application/x-rar-compressed" 
            => "/icons/file-archive.svg",
        _ => "/icons/file-unknown.svg"
    };
}
```

### Uploading a File

```csharp
// 1. User drops file in upload zone
var file = e.Files.First();

// 2. Facade orchestrates upload
var result = await _mediaFacade.UploadAsync(
    fileStream: file.Stream,
    fileName: file.Name,
    contentType: file.ContentType,
    folderPath: currentFolderPath,  // e.g., "/images/blog/2024/"
    options: new MediaUploadOptions
    {
        ExtractMetadata = true
    });

// Inside MediaFacade.UploadAsync:

// 3. Upload file to storage provider
var storageResult = await _storageRepository.UploadAsync(
    fileStream, fileName, contentType);

// 4. Extract metadata (dimensions, duration, etc.)
var metadata = await _metadataExtractor.ExtractMetadataAsync(
    fileStream, contentType);

// 5. Determine preview URL
string? previewUrl = contentType.StartsWith("image/")
    ? storageResult.PublicUrl  // Use full image for images
    : MediaIconHelper.GetFileTypeIcon(contentType);  // Use icon for others

// 6. Create MediaItem entity
var mediaItem = new MediaItem
{
    FileName = fileName,
    FileExtension = Path.GetExtension(fileName),
    FileSize = storageResult.FileSize,
    MimeType = contentType,
    StoragePath = storageResult.StoragePath,
    PublicUrl = storageResult.PublicUrl,
    PreviewUrl = previewUrl,
    FolderPath = folderPath ?? "/",
    Width = GetIntMetadata(metadata, "Width"),
    Height = GetIntMetadata(metadata, "Height"),
    AdditionalMetadata = metadata,
    Status = MediaStatus.Available,
    UploadedBy = currentUserId
};
mediaItem.UpdatePartitionKey();

// 7. Save to metadata repository
await _metadataRepository.AddAsync(mediaItem);
await _metadataRepository.SaveChangesAsync();
```

### Moving Files Between Folders

```csharp
// 1. User selects items and target folder path
var selectedIds = new[] { "item1", "item2", "item3" };
var targetFolderPath = "/images/archive/"

// 2. Facade orchestrates move
var movedItems = await _mediaFacade.BulkMoveAsync(
    selectedIds, targetFolderPath);

// Inside MediaFacade.BulkMoveAsync:

// 3. Get all items
var items = await _metadataRepository.GetByIdsAsync(selectedIds);

// 4. For each item, update folder path
foreach (var item in items)
{
    // Update folder path
    item.FolderPath = targetFolderPath;
    
    // Note: Storage path doesn't change - files stay in same physical location
    // Only the logical folder assignment in metadata changes
    
    await _metadataRepository.UpdateAsync(item);
}
await _metadataRepository.SaveChangesAsync();
```

### Displaying Media Library

```csharp
// 1. Component loads current folder path
var folderPath = RouteData.Values["folderPath"]?.ToString() ?? "/";

// 2. Get all folder paths for navigation (breadcrumbs/tree)
var allFolderPaths = await _metadataRepository.GetAllFolderPathsAsync();
var breadcrumbs = BuildBreadcrumbs(folderPath, allFolderPaths);

// 3. Fetch media items in current folder with paging
var items = await _mediaFacade.GetMediaItemsAsync(
    folderPath,
    new PagingParameters { PageNumber = 1, PageSize = 50 },
    filter);

// 4. Render grid
<div class="media-library">
    <Breadcrumbs Path="@folderPath" AllPaths="@allFolderPaths" />
    
    <div class="media-grid">
        @foreach (var item in items.Items)
        {
            <MediaCard MediaItem="@item">
                @* Preview: full image (rescaled) for images, icon for others *@
                <img src="@item.PreviewUrl" 
                     alt="@(item.AltText ?? item.FileName)" 
                     loading="lazy"
                     class="media-preview @(item.MimeType.StartsWith("image/") ? "media-preview--image" : "media-preview--icon")" />
                <div class="media-info">
                    <span class="filename">@item.FileName</span>
                    <span class="filesize">@FormatFileSize(item.FileSize)</span>
                    <span class="filetype">@item.FileExtension</span>
                </div>
            </MediaCard>
        }
    </div>
</div>
```

**CSS for rescaling**:
```css
.media-preview {
    display: block;
    max-width: 100%;
    height: auto;
}

.media-preview--image {
    /* Full images rescaled to fit container */
    width: 300px;
    height: 300px;
    object-fit: cover;
}

.media-preview--icon {
    /* Icons shown at natural size */
    width: 64px;
    height: 64px;
    object-fit: contain;
}
```

---

## Preview and Display

### Preview Types

#### Image Preview

**Supported Formats**: JPEG, PNG, GIF, WebP, SVG

**Features**:
- Full-sized image displayed (rescaled with CSS)
- Lazy loading
- Lightbox view (click to enlarge)

**Implementation**:
```razor
<div class="media-preview media-preview--image">
    <img src="@item.PreviewUrl" 
         alt="@item.AltText"
         loading="lazy"
         @onclick="() => ShowLightbox(item)" />
</div>
```

#### Video Preview

**Supported Formats**: MP4, WebM, MOV

**Features**:
- File type icon (no thumbnail)
- Duration display (from AdditionalMetadata)
- Resolution info (from AdditionalMetadata)
- Click to play in modal

**Implementation**:
```razor
<div class="media-preview media-preview--video">
    <img src="@item.PreviewUrl" alt="Video icon" />
    <span class="duration">@FormatDuration(item.AdditionalMetadata.GetValueOrDefault("Duration"))</span>
</div>

@* When clicked, show modal with video player *@
<div class="video-player-modal">
    <video controls>
        <source src="@item.PublicUrl" type="@item.MimeType" />
    </video>
</div>
```

#### PDF Preview

**Features**:
- PDF icon
- Page count display (from AdditionalMetadata)
- Download button
- Optional: PDF.js viewer integration (future)

**Implementation**:
```razor
<div class="media-preview media-preview--pdf">
    <img src="@item.PreviewUrl" alt="PDF icon" />
    <span class="page-count">@item.AdditionalMetadata.GetValueOrDefault("PageCount") pages</span>
    <a href="@item.PublicUrl" download class="download-btn">Download</a>
</div>
```

#### Document Preview

**Supported Formats**: DOC, DOCX, XLS, XLSX, TXT

**Features**:
- File type icon
- File type badge
- Download button
- Quick info (size, modified date)

**Implementation**:
```razor
<div class="media-preview media-preview--document">
    <img src="@item.PreviewUrl" alt="@item.FileExtension icon" />
    <span class="file-type-badge">@item.FileExtension.ToUpper()</span>
    <span class="file-size">@FormatFileSize(item.FileSize)</span>
    <a href="@item.PublicUrl" download class="download-btn">Download</a>
</div>
```

#### Generic File Preview

**For unsupported types**:
- Generic file icon
- File name display
- File size and date
- Download button

**Implementation**:
```razor
<div class="media-preview media-preview--generic">
    <img src="@item.PreviewUrl" alt="File icon" />
    <span class="filename">@item.FileName</span>
    <span class="file-size">@FormatFileSize(item.FileSize)</span>
    <a href="@item.PublicUrl" download class="download-btn">Download</a>
</div>
```

**Icon Set**: 
```
/icons/
  file-pdf.svg
  file-video.svg
  file-audio.svg
  file-document.svg
  file-spreadsheet.svg
  file-archive.svg
  file-unknown.svg
```

### Media Library Grid

**Layout**:
- Grid view (default)
- List view
- Sortable by name, size, date, type

**Grid View**:
```
???????????????????????????????????????????
? [Thumbnail] ? [Thumbnail] ? [Thumbnail] ?
?   Filename  ?   Filename  ?   Filename  ?
?   100 KB    ?   2.5 MB    ?   1.2 MB    ?
???????????????????????????????????????????
```

**List View**:
```
[Icon] filename.jpg        Image    100 KB    2024-03-15
[Icon] video.mp4           Video    25 MB     2024-03-14
[Icon] document.pdf        PDF      500 KB    2024-03-13
```

**Features**:
- Sortable columns (name, size, date, type)
- Multi-select (Ctrl/Shift)
- Context menu (right-click)
- Quick actions (hover)
- Infinite scroll or pagination

### Media Picker Component

**Purpose**: Select media from library for use in posts/pages

**Features**:
- Modal dialog
- Path-based navigation
- Search and filter
- Preview before selection
- Multi-select (galleries)
- Upload from picker

**Usage**:
```razor
<MediaPicker 
    OnSelect="@HandleMediaSelected"
    AllowMultiple="false"
    FileTypes="@(new[] { "image/*" })" />
```

**Return Value**:
```csharp
public class MediaSelection
{
    public string MediaId { get; set; }
    public string PublicUrl { get; set; }
    public string AltText { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
```

---

## Technical Implementation

### Entity Models

#### MediaItem Entity

**Class**: `MediaItem : BaseEntity`
**Location**: `Vilog.Shared.Data.Entities`

**Complete Definition**:
```csharp
public class MediaItem : BaseEntity
{
    // Core Properties
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    
    // Storage Properties (provider-agnostic)
    public string StoragePath { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string? PreviewUrl { get; set; }
    
    // Organization - path-based only
    public string FolderPath { get; set; } = "/";
    
    // Metadata
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AltText { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    
    // Additional metadata (provider-specific or file-type specific)
    // Examples: { "Duration": "180", "PageCount": "42", "ColorSpace": "sRGB" }
    public Dictionary<string, string> AdditionalMetadata { get; set; } = new();
    
    // Usage Tracking
    public int UsageCount { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }
    public string? UploadedBy { get; set; }
    
    // Status
    public MediaStatus Status { get; set; } = MediaStatus.Available;
    public string? ErrorMessage { get; set; }
    
    // Partition Strategy
    public void UpdatePartitionKey()
    {
        // Partition by upload year-month for efficient queries
        PartitionKey = CreatedAt.ToString("yyyy-MM");
    }
}

public enum MediaStatus
{
    Uploading,
    Available,
    InUse,
    Deleted
}
```

### Facades

#### MediaFacade

**Purpose**: Media item operations and business logic

**Methods**:
```csharp
public interface IMediaFacade
{
    // Upload
    Task<MediaItem> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folderPath = null,
        MediaUploadOptions? options = null,
        CancellationToken cancellationToken = default);
    
    // Bulk upload
    Task<List<MediaUploadResult>> BulkUploadAsync(
        List<MediaUploadRequest> requests,
        CancellationToken cancellationToken = default);
    
    // Get items
    Task<PagedResult<MediaItem>> GetMediaItemsAsync(
        string? folderPath,
        PagingParameters pagingParameters,
        MediaItemFilter? filter = null,
        CancellationToken cancellationToken = default);
    
    // Get single item
    Task<MediaItem?> GetMediaItemAsync(
        string id,
        CancellationToken cancellationToken = default);
    
    // Update metadata
    Task<MediaItem> UpdateMetadataAsync(
        string id,
        MediaMetadataUpdate update,
        CancellationToken cancellationToken = default);
    
    // Move to different folder path
    Task<MediaItem> MoveToFolderAsync(
        string id,
        string targetFolderPath,
        CancellationToken cancellationToken = default);
    
    // Bulk move
    Task<List<MediaItem>> BulkMoveAsync(
        List<string> ids,
        string targetFolderPath,
        CancellationToken cancellationToken = default);
    
    // Delete
    Task DeleteAsync(
        string id,
        bool permanent = false,
        CancellationToken cancellationToken = default);
    
    // Bulk delete
    Task BulkDeleteAsync(
        List<string> ids,
        bool permanent = false,
        CancellationToken cancellationToken = default);
    
    // Search
    Task<PagedResult<MediaItem>> SearchAsync(
        string searchTerm,
        MediaItemFilter? filter = null,
        PagingParameters? pagingParameters = null,
        CancellationToken cancellationToken = default);
    
    // Get public URL
    Task<string> GetPublicUrlAsync(
        string id,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default);
    
    // Get all unique folder paths for navigation
    Task<List<string>> GetAllFolderPathsAsync(
        CancellationToken cancellationToken = default);
}
```

### Performance Optimizations

#### Lazy Loading
- Thumbnail images load on scroll
- Metadata loads on hover/click

#### Caching
- Public URLs cached (1 hour)
- Folder paths cached (5 min)
- Metadata cached per item

#### Batch Operations
- Bulk upload in parallel (5 concurrent max)
- Bulk move in transaction
- Bulk delete batched (100 items)

#### Pagination
- Default: 50 items per page
- Infinite scroll option
- Virtual scrolling for large lists

#### Image Optimization
- Full images rescaled via CSS
- Lazy loading with IntersectionObserver
- CDN delivery

---

## Architectural Decisions

### Why Path-Based Organization Instead of Folder Entities?

**Simplified Approach**:
- Folders derived from `FolderPath` string property
- No separate `MediaFolder` entity or repository
- No denormalized folder names or counts

**Benefits**:
- Simpler data model (one entity instead of two)
- No folder hierarchy to maintain
- Fast folder navigation (just string operations)
- No synchronization between media items and folders
- Easier to reorganize (just update path strings)

**Folder Operations**:
```csharp
// Get all unique folder paths
var folderPaths = await _repository.GetAllFolderPathsAsync();

// Build folder tree from paths
var folderTree = BuildTreeFromPaths(folderPaths);

// Move item to new folder
item.FolderPath = "/new/folder/path/";
await _repository.UpdateAsync(item);
```

**Alternative Considered**: Separate MediaFolder entities with parent/child relationships
- **Rejected**: Adds complexity, denormalization, synchronization overhead
- **Current approach**: Simple string paths that can be parsed for navigation

### Why No Tags?

**Path-Based Organization Sufficient**:
- Folders provide hierarchical organization
- Search provides discovery across all metadata
- No need for cross-cutting categorization

**Benefits**:
- Simpler entity model
- No tag management UI
- No tag cloud maintenance
- Fewer queries (no tag joins)

**Alternative**: Use `Description` or `AdditionalMetadata` for keywords
- Can still search by description content
- Flexible without formal tag system

### Why Two Repositories?

**Separation of Concerns**:
- **Metadata Repository**: Manages MediaItem entities in CosmosDB (searchable, queryable)
- **Storage Repository**: Manages physical files (upload, download, delete)

**Benefits**:
- Switch storage providers without changing entity model
- Storage operations isolated from database operations
- Clean repository pattern following Vilog conventions
- Each repository has single responsibility

### Why Not Store Files in CosmosDB?

**CosmosDB is optimized for**:
- JSON documents (metadata)
- Queries and indexes
- Fast lookups

**Not optimized for**:
- Large binary files
- Streaming content
- CDN delivery

**Solution**: Store metadata in CosmosDB, files in appropriate storage (Blob/FileSystem/SQL)

### Why AdditionalMetadata Dictionary?

**Storage-Agnostic Design**:
- Different file types have different metadata (video has duration, PDF has page count)
- Different storage providers may add provider-specific data
- Strongly-typed properties would limit flexibility

**Benefits**:
- Single entity model works for all file types
- No need for inheritance hierarchy
- Easy to add new metadata without schema changes
- Optional values don't bloat entity

### Why PreviewUrl Instead of Thumbnail Generation?

**Simplified Approach**:
- **Images**: Use full-sized image URL (rescaled in display via CSS)
- **Other files**: Use static file type icons

**Benefits**:
- Consistent API (PreviewUrl always exists)
- No thumbnail generation required
- Icons don't require processing
- Zero upload-time image processing
- Reduces server load and complexity
- Browser handles image rescaling efficiently

**Display Strategy**:
```css
/* Full images rescaled via CSS */
.media-preview--image {
    width: 300px;
    height: 300px;
    object-fit: cover;
}
```

**Alternative Considered**: Generate and store thumbnails on upload
- **Rejected**: Adds processing time, storage overhead, complexity
- **Current approach**: Let browser do the rescaling (modern browsers handle this efficiently)
- **Future**: Can add CDN-based image resizing if performance requires it

---

## API Reference

### Upload Endpoints

#### POST /api/media/upload

**Purpose**: Upload single file

**Request**: `multipart/form-data`
```
Content-Type: multipart/form-data
file: <binary>
folderPath: "/images/blog/"
```

**Response**:
```json
{
  "id": "abc123",
  "fileName": "image.jpg",
  "publicUrl": "https://cdn.vilog.com/media/image.jpg",
  "previewUrl": "https://cdn.vilog.com/media/image.jpg",
  "fileSize": 102400,
  "mimeType": "image/jpeg",
  "status": "Available",
  "folderPath": "/images/blog/"
}
```

#### POST /api/media/upload/bulk

**Purpose**: Upload multiple files

**Request**: `multipart/form-data` (multiple files)

**Response**: Array of upload results

### Media Item Endpoints

#### GET /api/media/items

**Purpose**: List media items

**Query Parameters**:
- `folderPath`: Filter by folder path
- `type`: Filter by media type (`image`, `video`, `document`)
- `search`: Search term
- `page`: Page number
- `pageSize`: Items per page
- `sort`: Sort field
- `order`: Sort order (`asc`, `desc`)

#### GET /api/media/items/{id}

**Purpose**: Get single media item

#### PUT /api/media/items/{id}

**Purpose**: Update metadata

**Request**:
```json
{
  "title": "Updated Title",
  "description": "Updated description",
  "altText": "Updated alt text"
}
```

#### DELETE /api/media/items/{id}

**Purpose**: Delete media item

**Query Parameters**:
- `permanent`: Hard delete (default: `false`)

#### POST /api/media/items/{id}/move

**Purpose**: Move to different folder path

**Request**:
```json
{
  "targetFolderPath": "/images/archive/"
}
```

### Folder Endpoints

#### GET /api/media/folders

**Purpose**: Get all unique folder paths

**Response**:
```json
{
  "folderPaths": [
    "/",
    "/images/",
    "/images/blog/",
    "/images/blog/2024/",
    "/documents/",
    "/documents/pdfs/"
  ]
}
```

### Bulk Operation Endpoints

#### POST /api/media/items/bulk/move

**Purpose**: Move multiple items

**Request**:
```json
{
  "itemIds": ["id1", "id2", "id3"],
  "targetFolderPath": "/images/archive/"
}
```

#### DELETE /api/media/items/bulk

**Purpose**: Delete multiple items

### Search Endpoint

#### GET /api/media/search

**Purpose**: Search media items

**Query Parameters**:
- `q`: Search query
- `type`: Media type filter
- `folderPath`: Folder filter
- `page`: Page number
- `pageSize`: Items per page

---

## Best Practices

### Organization

1. **Use descriptive folder paths**: `/images/blog-posts/2024/` not `/img/`
2. **Keep paths consistent**: Follow a naming convention
3. **Organize by purpose**: Not just by file type
4. **Regular cleanup**: Archive or delete unused media

### File Naming

1. **Descriptive names**: `blazor-architecture-diagram.png` not `img001.png`
2. **Lowercase with hyphens**: `my-file-name.jpg` not `My File Name.jpg`
3. **No special characters**: Avoid spaces, &, #, etc.
4. **Include version**: `logo-v2.png` for iterations
5. **Date prefix for archives**: `2024-03-15-screenshot.png`

### Metadata

1. **Always add alt text**: For accessibility and SEO
2. **Use descriptive titles**: Searchable, meaningful
3. **Fill descriptions**: Especially for reusable media
4. **Review and update**: Keep metadata current

### Storage

1. **Choose appropriate provider**: Based on scale and budget
2. **Monitor storage costs**: Track usage and optimize
3. **Regular backups**: Automated backup strategy
4. **CDN for public media**: Improve performance
5. **Archive old media**: Move to cold storage

### Performance

1. **Optimize images**: Before upload (compression, sizing)
2. **Use appropriate formats**: WebP for web, PNG for transparency
3. **Lazy loading**: For media-heavy pages
4. **Pagination**: Don't load entire library

### Security

1. **Validate file types**: Server-side validation
2. **Sanitize filenames**: Prevent path traversal
3. **Access control**: Permissions for sensitive media (future)
4. **Secure URLs**: Time-limited SAS tokens for private media

---

## Summary

The Vilog Media Manager provides a simplified, performant media asset management system with:

- **Path-based organization** (no separate folder entities)
- **No tagging system** (organize by paths, search by metadata)
- **Flexible storage backends** (Blob Storage, File System, SQL Server)
- **Full-sized images with CSS rescaling** (no thumbnail generation)
- **Storage-agnostic metadata** in CosmosDB
- **Clean architecture** following Display-Facade-Repository pattern
- **Performance optimizations** with caching and lazy loading

Whether you're uploading media, organizing assets, or integrating media into blog posts, the Media Manager provides the tools needed for efficient media asset management with minimal complexity.

For technical support or feature requests, please refer to the main [Vilog Architecture Guide](./general.md).
