# Viblog Media Manager Implementation Plan

## Overview

This plan outlines the step-by-step implementation of the Viblog Media Manager, a comprehensive media asset management system with flexible storage backends, path-based organization, and document preview capabilities.

**Status**: ✅ **Phase 4 Complete - All UI Components Implemented**

**Related Documentation:**
- [Media Manager Intent Document](../intent/media-manager.md)
- [General Architecture Guide](../intent/general.md)
- [Setup & Configuration Guide](../MediaManagerSetup.md)
- [File Operations Implementation](../Step18-FileOperations.md)

---

## Implementation Phases

### Phase 1: Data Layer (Steps 1-6)
Foundation layer with entities, repositories, and data access

### Phase 2: Service Layer (Steps 7-10)
Business logic and orchestration services

### Phase 3: Infrastructure (Step 11)
Dependency injection and configuration

### Phase 4: UI Components (Steps 12-18)
Blazor components and user interface

### Phase 5: API & Configuration (Steps 19-22)
REST endpoints and application configuration

### Phase 6: Testing (Steps 23-26)
Comprehensive test coverage

---

## Detailed Steps

### Step 1: Create MediaItem Entity and MediaStatus Enum

**Location:** `Viblog.Shared.Data.Entities`

**Tasks:**
- Create `MediaItem` class inheriting from `BaseEntity`
- Add core properties (FileName, FileExtension, FileSize, MimeType)
- Add storage properties (StoragePath, PublicUrl, PreviewUrl)
- Add organization property (FolderPath with default "/")
- Add metadata properties (Title, Description, AltText, Width, Height)
- Add `AdditionalMetadata` dictionary for extensible metadata
- Add usage tracking (UsageCount, LastAccessedAt, UploadedBy)
- Add status properties (Status enum, ErrorMessage)
- Create `MediaStatus` enum with values: Uploading, Available, InUse, Deleted
- Implement `UpdatePartitionKey()` method for year-month partitioning

**Dependencies:** BaseEntity

**Validation:**
- Compile successfully
- Verify all required properties are non-nullable
- Test UpdatePartitionKey generates correct format (yyyy-MM)

---

### Step 2: Create IMediaStorageRepository Interface

**Location:** `Viblog.Shared.Data.Repositories`

**Tasks:**
- Define `IMediaStorageRepository` interface
- Add `UploadAsync` method returning `MediaStorageResult`
- Add `DownloadAsync` method returning Stream
- Add `DeleteAsync` method for file removal
- Add `MoveAsync` method for file relocation
- Add `GetPublicUrlAsync` with optional TimeSpan expiration
- Add `ExistsAsync` and `GetFileSizeAsync` helper methods
- Create `MediaStorageResult` class with StoragePath, PublicUrl, FileSize

**Dependencies:** None

**Validation:**
- Interface compiles
- All async methods have CancellationToken parameter
- Return types are appropriate for operations

---

### Step 3: Implement BlobStorageRepository

**Location:** `Viblog.Shared.Data.Repositories.Storage`

**Tasks:**
- Add `Azure.Storage.Blobs` NuGet package
- Create `BlobStorageRepository` implementing `IMediaStorageRepository`
- Inject `IConfiguration` for connection string and container name
- Implement `UploadAsync` with blob upload logic
- Generate storage path with date-based structure (yyyy/MM/filename)
- Implement CDN URL generation from configuration
- Implement SAS token generation for `GetPublicUrlAsync`
- Implement `DownloadAsync`, `DeleteAsync`, `MoveAsync`
- Add comprehensive error handling and logging

**Dependencies:** Azure.Storage.Blobs, IConfiguration

**Configuration Required:**
```json
{
  "MediaStorage": {
    "Provider": "BlobStorage",
    "BlobStorage": {
      "ConnectionString": "...",
      "ContainerName": "Viblog-media",
      "CdnUrl": "https://cdn.Viblog.com"
    }
  }
}
```

**Validation:**
- Can upload file to Azure Blob Storage
- PublicUrl generated correctly
- SAS tokens work for time-limited access

---

### Step 4: Implement FileSystemStorageRepository

**Location:** `Viblog.Shared.Data.Repositories.Storage`

**Tasks:**
- Create `FileSystemStorageRepository` implementing `IMediaStorageRepository`
- Inject `IConfiguration` for base path and base URL
- Implement `UploadAsync` with file system operations
- Create directory structure automatically (yyyy/MM)
- Generate public URLs by combining base URL with relative path
- Implement `DownloadAsync` using FileStream
- Implement `DeleteAsync`, `MoveAsync` with File.Move
- Add file locking and error handling

**Dependencies:** IConfiguration, System.IO

**Configuration Required:**
```json
{
  "MediaStorage": {
    "Provider": "FileSystem",
    "FileSystem": {
      "BasePath": "C:\\Viblog\\Media",
      "BaseUrl": "https://Viblog.local/media"
    }
  }
}
```

**Validation:**
- Files saved to correct directory structure
- Public URLs accessible
- Move and delete operations work correctly

---

### Step 5: Create IMediaMetadataRepository Interface

**Location:** `Viblog.Shared.Data.Repositories`

**Tasks:**
- Define `IMediaMetadataRepository` extending `IRepository<MediaItem>`
- Add `GetItemsInFolderAsync` with paging and filtering
- Add `GetItemsByTypeAsync` for type-based queries
- Add `SearchAsync` for full-text search on filename, title, description
- Add `GetByStoragePathAsync` for provider lookups
- Add `GetItemsInUseAsync` (where UsageCount > 0)
- Add `GetUnusedItemsAsync` with age parameter
- Add `GetAllFolderPathsAsync` for folder tree navigation
- Add `UpdateUsageCountAsync` for atomic counter updates

**Dependencies:** IRepository<MediaItem>, PagedResult, PagingParameters

**Validation:**
- Interface compiles
- All methods properly documented
- Filter and paging parameters consistent

---

### Step 6: Implement MediaMetadataRepository

**Location:** `Viblog.Shared.Data.Repositories`

**Tasks:**
- Create `MediaMetadataRepository` implementing `IMediaMetadataRepository`
- Use CosmosDB container for `MediaItem` entities
- Implement `GetItemsInFolderAsync` using LINQ where clause on FolderPath
- Implement `GetItemsByTypeAsync` using MimeType pattern matching
- Implement `SearchAsync` across FileName, Title, Description fields
- Implement `GetByStoragePathAsync` with cross-partition query
- Implement `GetAllFolderPathsAsync` using distinct on FolderPath
- Implement `UpdateUsageCountAsync` with optimistic concurrency
- Add partition key handling for efficient queries

**Dependencies:** CosmosDB client, IRepository base

**Validation:**
- Can query items by folder path
- Search works across all text fields
- Folder paths returned correctly
- Usage count updates are atomic

---

### Step 7: Create IMetadataExtractorService Interface and Implementation

**Location:** `Viblog.Shared.Services`

**Tasks:**
- Define `IMetadataExtractorService` interface
- Add `ExtractMetadataAsync` method accepting Stream and mimeType
- Create `MetadataExtractorService` implementation
- Add `SixLabors.ImageSharp` for image metadata extraction
- Add appropriate library for video metadata (e.g., MediaInfo)
- Add `PdfSharp` or similar for PDF metadata
- Extract width/height for images
- Extract duration, codec, bitrate for videos
- Extract page count, author for PDFs
- Return `Dictionary<string, string>` with all values as strings

**Dependencies:** SixLabors.ImageSharp, PDF library, Video library

**Validation:**
- Image dimensions extracted correctly
- Video duration extracted
- PDF page count extracted
- All metadata values are strings

---

### Step 8: Create MediaIconHelper Static Class

**Location:** `Viblog.Shared.Helpers`

**Tasks:**
- Create static `MediaIconHelper` class
- Implement `GetFileTypeIcon(string mimeType)` method
- Return `null` for image/* types (use actual image)
- Return `/icons/file-pdf.svg` for PDFs
- Return `/icons/file-video.svg` for video/*
- Return `/icons/file-audio.svg` for audio/*
- Return `/icons/file-document.svg` for Word documents
- Return `/icons/file-spreadsheet.svg` for Excel files
- Return `/icons/file-archive.svg` for ZIP/RAR
- Return `/icons/file-unknown.svg` as fallback

**Dependencies:** None

**Validation:**
- Correct icon returned for each MIME type
- Null returned for images
- Fallback works for unknown types

---

### Step 9: Create IMediaService Interface and Implementation

**Location:** `Viblog.Shared.Services`

**Tasks:**
- Define `IMediaService` interface
- Add `UploadAsync` methods (with and without custom metadata)
- Add `GetByIdAsync` for single item retrieval
- Add `DeleteAsync` for soft delete
- Add `GetPublicUrlAsync` with optional expiration
- Add `UpdateMetadataAsync` for metadata changes
- Create `MediaService` implementation
- Inject `IMediaStorageRepository`, `IMediaMetadataRepository`, `IMetadataExtractorService`
- Implement upload orchestration: storage ? metadata extraction ? entity creation
- Set PreviewUrl based on MIME type (image URL or icon)
- Update partition key before saving
- Implement soft delete by setting Status and DeletedAt

**Dependencies:** All repositories and services from previous steps

**Validation:**
- Upload creates both file and metadata
- Metadata extracted and stored
- Soft delete works correctly
- Public URLs generated properly

---

### Step 10: Create IMediaFacade Interface and Implementation

**Location:** `Viblog.Shared.Facades`

**Tasks:**
- Define `IMediaFacade` interface
- Add `UploadAsync` and `BulkUploadAsync` methods
- Add `GetMediaItemsAsync` with folder filtering
- Add `MoveToFolderAsync` and `BulkMoveAsync` methods
- Add `DeleteAsync` and `BulkDeleteAsync` methods
- Add `SearchAsync` for media search
- Add `GetAllFolderPathsAsync` for navigation
- Create `MediaFacade` implementation
- Inject `IMediaService` and repositories
- Implement bulk operations with parallel processing (max 5 concurrent)
- Add transaction handling for bulk moves
- Batch deletes in groups of 100

**Dependencies:** IMediaService, repositories

**Validation:**
- Bulk operations work efficiently
- Transactions maintain consistency
- Search functionality works
- Folder paths returned correctly

---

### Step 11: Create Storage Provider Registration Extension

**Location:** `Viblog.Shared.Extensions`

**Tasks:**
- Create `ServiceCollectionExtensions` class
- Add `AddMediaStorage` extension method
- Read `MediaStorage:Provider` from IConfiguration
- Register appropriate repository based on provider value
- Support "BlobStorage", "FileSystem", "SqlServer"
- Throw `InvalidOperationException` for unknown providers
- Update `Program.cs` to call extension method
- Register `IMediaMetadataRepository` as scoped
- Register `IMetadataExtractorService` as scoped
- Register `IMediaService` as scoped
- Register `IMediaFacade` as scoped

**Dependencies:** All repository implementations

**Validation:**
- Correct repository registered based on config
- All services registered in DI container
- Application starts successfully

---

### Step 12: Create MediaLibrary Blazor Component

**Location:** `Viblog/Components/Admin/Media`

**Tasks:**
- Create `MediaLibrary.razor` component
- Implement three-panel layout (folder tree, media grid, preview)
- Add CSS Grid layout with responsive columns
- Add `_uiOnlyFolders` and `_persistedFolders` state
- Implement `OnInitializedAsync` to load folder paths
- Add breadcrumb navigation
- Add header with Upload, New Folder, Search, View toggle buttons
- Implement folder tree panel (left 25%)
- Implement media grid panel (center 50-75%, expands when preview closed)
- Implement preview panel (right 0-33%, conditional)
- Add drag-and-drop upload zone

**Dependencies:** Telerik components, MediaFacade

**Validation:**
- Layout renders correctly
- Three panels resize properly
- State management works

---

### Step 13: Implement Folder Tree Panel with Telerik TreeView

**Location:** `Viblog/Components/Admin/Media/MediaLibrary.razor`

**Tasks:**
- Create `FolderNode` class (Path, Name, ParentPath, IsUiOnly)
- Implement `BuildFolderTree` method from folder paths
- Add Telerik TreeView component
- Configure tree bindings (TextField, ParentIdField, IdField)
- Implement `OnFolderSelected` event handler
- Implement `OnFolderContextMenu` event handler
- Add context menu with New Folder, Rename, Delete options
- Implement `CreateNewFolderAsync` adding to UI-only list
- Implement `RenameFolderAsync` for both UI-only and persisted
- Implement `DeleteEmptyFolderAsync` with validation
- Show "New" badge for UI-only folders

**Dependencies:** Telerik TreeView, folder state

**Validation:**
- Tree displays correctly
- Context menu works
- Folder creation adds to UI list
- Deletion validates empty folders

---

### Step 14: Implement Media Grid Panel with CSS Grid

**Location:** `Viblog/Components/Admin/Media/MediaLibrary.razor`

**Tasks:**
- Create `MediaItemDisplay` wrapper class
- Implement CSS Grid layout with auto-fill columns
- Add grid view mode (default, 200px min columns)
- Add list view mode (single column, horizontal layout)
- Implement item rendering (folders, images, icons)
- Add multi-select with Ctrl/Shift click handling
- Implement `OnItemClick` for selection management
- Implement `OnItemDoubleClick` for navigation/preview
- Implement `OnItemContextMenu` for right-click menu
- Add context menu with Move, Delete, Edit Metadata options
- Style selected items with border and background
- Add hover effects

**Dependencies:** MediaItemDisplay, CSS Grid

**Validation:**
- Grid displays items correctly
- Multi-select works
- Context menu appears
- Double-click navigates or previews

---

### Step 15: Implement List View Toggle and Sorting

**Location:** `Viblog/Components/Admin/Media/MediaLibrary.razor`

**Tasks:**
- Add `viewMode` state variable ("grid" or "list")
- Add Telerik ButtonGroup for view toggle
- Apply conditional CSS class based on viewMode
- Create sort controls with Telerik DropDownList
- Add sort options: name, size, date, type
- Add sort direction toggle button
- Implement `SortMediaItems` method
- Update grid when sort or direction changes
- Persist view mode in component state

**Dependencies:** Telerik ButtonGroup, DropDownList

**Validation:**
- View toggle works
- Sorting applies correctly
- Sort direction toggles
- UI updates on change

---

### Step 16: Implement Preview Panel for Documents

**Location:** `Viblog/Components/Admin/Media/MediaLibrary.razor`

**Tasks:**
- Add conditional rendering based on selected item
- Integrate Telerik PDFViewer for PDF files
- Integrate Telerik Spreadsheet for Excel files
- Convert Word documents to PDF using Telerik Document Processing
- Add preview header with filename and close button
- Add preview content area (100% height)
- Add preview footer with metadata display
- Add action buttons (Download, Edit Metadata, Delete)
- Implement `ShowPreviewAsync` to load document
- Implement `ClosePreview` to hide panel
- Download file stream for preview
- Handle loading states

**Dependencies:** Telerik PDFViewer, Spreadsheet, Document Processing

**Validation:**
- PDF preview works
- Excel preview works
- Word converted to PDF successfully
- Metadata displayed correctly

---

### Step 17: Create Upload Dialog Component

**Location:** `Viblog/Components/Admin/Media/UploadDialog.razor`

**Tasks:**
- Create modal dialog component
- Add Telerik FileSelect or InputFile
- Implement drag-and-drop zone
- Add file list with progress bars
- Show upload progress for each file
- Support bulk file selection
- Validate file types server-side
- Validate file sizes against limits
- Display errors for failed uploads
- Call MediaFacade.BulkUploadAsync
- Close dialog on completion
- Refresh parent component

**Dependencies:** Telerik Dialog, FileSelect, MediaFacade

**Validation:**
- Files upload successfully
- Progress shown correctly
- Validation works
- Bulk upload efficient

---

### Step 18: Implement File Operations (Move, Delete, Rename)

**Location:** `Viblog/Components/Admin/Media/MediaLibrary.razor`

**Tasks:**
- Create move dialog with folder tree picker
- Implement `BulkMoveAsync` calling facade
- Create delete confirmation dialog
- Implement soft delete calling facade
- Show success/error notifications
- Refresh media grid after operations
- Add undo support for delete (future)
- Implement rename inline edit or dialog
- Update UI optimistically
- Handle errors gracefully

**Dependencies:** Telerik Dialog, Notification, MediaFacade

**Validation:**
- Move dialog shows folders
- Delete confirms and executes
- UI refreshes after operations
- Errors displayed to user

---

### Step 19: Create API Controllers for Media Operations

**Location:** `Viblog/Controllers/Api`

**Tasks:**
- Create `MediaController` : ControllerBase
- Add `[ApiController]` and `[Route("api/media")]` attributes
- Inject `IMediaFacade`
- Add `POST /upload` endpoint (multipart/form-data)
- Add `POST /upload/bulk` endpoint
- Add `GET /items` endpoint with query parameters
- Add `GET /items/{id}` endpoint
- Add `PUT /items/{id}` endpoint for metadata
- Add `DELETE /items/{id}` endpoint with permanent flag
- Add `POST /items/{id}/move` endpoint
- Add `GET /folders` endpoint
- Add `POST /items/bulk/move` endpoint
- Add `DELETE /items/bulk` endpoint
- Add `GET /search` endpoint
- Add appropriate authorization attributes

**Dependencies:** ASP.NET Core, MediaFacade

**Validation:**
- All endpoints respond correctly
- File uploads work
- Query parameters parsed
- Authorization required

---

### Step 20: Add AppSettings Configuration for Storage Providers

**Location:** `Viblog/appsettings.json`, `Viblog/appsettings.Development.json`

**Tasks:**
- Add `MediaStorage` section to appsettings
- Configure `Provider` setting
- Add `BlobStorage` configuration section
- Add `FileSystem` configuration section
- Add `MaxFileSize` setting (e.g., 100MB)
- Add `AllowedExtensions` array
- Configure development settings for local FileSystem
- Configure production settings for BlobStorage
- Add connection strings
- Document all configuration options

**Dependencies:** None

**Validation:**
- Configuration loads correctly
- Provider selection works
- Connection strings valid
- File size limits enforced

---

### Step 21: Create Media Library Route and Navigation

**Location:** `Viblog/Program.cs`, Navigation components

**Tasks:**
- Add `/admin/media` route in routing configuration
- Add media library link to admin navigation menu
- Configure authorization for admin-only access
- Add route parameters for folder navigation (/admin/media/{*folderPath})
- Update breadcrumb component
- Add icon for media library in navigation
- Set active state for current route

**Dependencies:** Routing configuration

**Validation:**
- Route accessible at /admin/media
- Navigation link appears
- Authorization enforced
- Folder navigation works

---

### Step 22: Add Media Library Icons and Assets

**Location:** `Viblog/wwwroot/icons`

**Tasks:**
- Create `/wwwroot/icons` directory
- Add `file-pdf.svg` icon
- Add `file-video.svg` icon
- Add `file-audio.svg` icon
- Add `file-document.svg` icon (Word)
- Add `file-spreadsheet.svg` icon (Excel)
- Add `file-archive.svg` icon (ZIP/RAR)
- Add `file-unknown.svg` icon (generic)
- Optimize all SVGs for web delivery
- Add CSS for icon sizing and styling
- Ensure icons are accessible

**Dependencies:** SVG icons

**Validation:**
- Icons display correctly
- SVGs optimized
- Icons accessible
- Styling consistent

---

### Step 23: Write Unit Tests for MediaService

**Location:** `Viblog.Tests/Services`

**Tasks:**
- Create `MediaServiceTests` class
- Mock `IMediaStorageRepository`, `IMediaMetadataRepository`, `IMetadataExtractorService`
- Test `UploadAsync` with metadata extraction
- Test `UploadAsync` with custom metadata
- Test `GetByIdAsync` with valid ID
- Test `GetByIdAsync` with invalid ID returns null
- Test `DeleteAsync` soft delete behavior
- Test `UpdateMetadataAsync` changes title, description, altText
- Test error handling scenarios
- Verify repository methods called correctly

**Dependencies:** xUnit, Moq

**Validation:**
- All tests pass
- Edge cases covered
- Mocks verify interactions
- Code coverage > 80%

---

### Step 24: Write Unit Tests for MediaFacade

**Location:** `Viblog.Tests/Facades`

**Tasks:**
- Create `MediaFacadeTests` class
- Mock `IMediaService`, repositories
- Test `BulkUploadAsync` with multiple files
- Test `GetMediaItemsAsync` folder filtering
- Test `BulkMoveAsync` updates all items
- Test `SearchAsync` functionality
- Test `GetAllFolderPathsAsync` returns unique paths
- Test transaction rollback on error
- Test parallel processing limits

**Dependencies:** xUnit, Moq

**Validation:**
- All tests pass
- Bulk operations tested
- Error handling verified
- Code coverage > 80%

---

### Step 25: Write Unit Tests for Storage Repositories

**Location:** `Viblog.Tests/Repositories`

**Tasks:**
- Create `BlobStorageRepositoryTests` class
- Mock Azure Blob Storage client
- Test `UploadAsync` creates blob correctly
- Test `DownloadAsync` retrieves stream
- Test `DeleteAsync` removes blob
- Test public URL generation
- Test SAS token generation
- Create `FileSystemStorageRepositoryTests` class
- Test file system operations
- Test directory creation
- Mock file system operations where possible

**Dependencies:** xUnit, Moq, Azure.Storage.Blobs test helpers

**Validation:**
- All tests pass
- Both implementations tested
- File operations verified
- Error cases covered

---

### Step 26: Add Integration Tests for Media Workflows

**Location:** `Viblog.Tests/Integration`

**Tasks:**
- Create `MediaWorkflowTests` class
- Use test database (Cosmos DB Emulator) and test storage
- Test complete upload workflow (file ? storage ? metadata)
- Test folder organization and navigation
- Test file move between folders
- Test search across metadata fields
- Test soft delete and restore
- Test bulk operations end-to-end
- Clean up test data after each test
- Use realistic test files (small images, PDFs)

**Dependencies:** xUnit, test infrastructure, Cosmos DB Emulator

**Validation:**
- Integration tests pass
- Workflows work end-to-end
- Test data cleaned up
- Tests run independently

---

## Success Criteria

### Functionality
- ? Media upload works for all supported file types
- ? Folder organization using path-based system
- ? Preview panel displays PDFs, Word, Excel documents
- ? Search finds media by name, title, description
- ? Multi-select and bulk operations work efficiently
- ? Storage provider can be switched via configuration

### Performance
- ? Bulk upload handles 50+ files concurrently
- ? Media grid loads in < 2 seconds for 500 items
- ? Search results return in < 1 second
- ? Preview panel opens in < 3 seconds

### Code Quality
- ? All unit tests pass
- ? Code coverage > 80%
- ? No critical code smells
- ? Follows .NET conventions
- ? Proper error handling throughout

### User Experience
- ? Intuitive three-panel layout
- ? Responsive design works on different screen sizes
- ? Clear feedback for all operations
- ? Errors displayed with helpful messages
- ? Loading states for async operations

---

## Notes

### Key Design Decisions

1. **CSS Grid over Telerik Grid/TileLayout**
   - Simpler implementation for media display
   - Full control over styling and behavior
   - Reduced dependency on Telerik for basic layouts
   - Still using Telerik for TreeView and document viewers

2. **UI-Only Folders**
   - No placeholder files or database entries for empty folders
   - Folders exist in component state until files uploaded
   - Simplifies data model
   - Clear UX with "New" badge

3. **No Thumbnail Generation**
   - Full images rescaled via CSS
   - Icons for non-image files
   - Reduces server processing
   - Relies on browser rendering

4. **Path-Based Organization**
   - No separate folder entities
   - Folders derived from file paths
   - Simpler to maintain
   - Easy reorganization

### Future Enhancements

- Media picker component for selecting media in blog posts
- Advanced search with filters
- Batch metadata editing
- Image editing (crop, resize, filters)
- Video thumbnail generation
- CDN integration for FileSystem provider
- Permission-based access control
- Audit log for all operations

---

## References

- [Media Manager Intent Document](../intent/media-manager.md)
- [General Architecture Guide](../intent/general.md)
- [Post System Intent](../intent/post-system.md)
- [Telerik UI for Blazor Documentation](https://docs.telerik.com/blazor-ui)
- [Azure Blob Storage Documentation](https://docs.microsoft.com/en-us/azure/storage/blobs/)
