# Media Manager Configuration & Navigation Setup

## ✅ Completed: Option 1 - Configuration for Testing

### Files Modified

#### 1. **appsettings.Development.json**
Added MediaStorage configuration for local development:

```json
{
  "MediaStorage": {
    "Provider": "FileSystem",
    "FileSystem": {
      "BasePath": "C:\\Viblog\\Media",
      "BaseUrl": "https://localhost:5001/media"
    },
    "BlobStorage": {
      "ConnectionString": "UseDevelopmentStorage=true",
      "ContainerName": "Viblog-media-dev",
      "CdnUrl": ""
    }
  }
}
```

**Configuration Details:**
- **Provider**: Set to "FileSystem" for local development
- **FileSystem.BasePath**: Local directory where media files will be stored (`C:\Viblog\Media`)
- **FileSystem.BaseUrl**: URL path for accessing media files (`/media`)
- **BlobStorage**: Optional settings for Azure Storage Emulator if needed

#### 2. **appsettings.json**
Added MediaStorage configuration for production:

```json
{
  "MediaStorage": {
    "Provider": "FileSystem",
    "FileSystem": {
      "BasePath": "/var/Viblog/media",
      "BaseUrl": "https://yourblog.com/media"
    },
    "BlobStorage": {
      "ConnectionString": "",
      "ContainerName": "Viblog-media",
      "CdnUrl": ""
    }
  }
}
```

**Production Notes:**
- Update `Provider` to "BlobStorage" when using Azure
- Fill in `ConnectionString` and `CdnUrl` for Azure Blob Storage
- Update `BaseUrl` to match your production domain

#### 3. **Program.cs**
Added static file serving for media directory:

```csharp
// Configure static file serving for media files
var mediaBasePath = builder.Configuration["MediaStorage:FileSystem:BasePath"];
if (!string.IsNullOrEmpty(mediaBasePath) && Directory.Exists(mediaBasePath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(mediaBasePath),
        RequestPath = "/media"
    });
}
```

**What This Does:**
- Serves files from the configured media directory
- Maps them to the `/media` URL path
- Only activates if the directory exists
- Allows uploaded files to be accessed via public URLs

---

## ✅ Completed: Option 2 - Add Navigation Menu Item

### Files Modified

#### 1. **AdminLayout.razor**
Added Media Library to the admin navigation menu:

```csharp
private List<DrawerItem> _navigationItems = new()
{
    new DrawerItem { Text = "Dashboard", Icon = SvgIcon.Grid, Url = "/admin" },
    new DrawerItem { Text = "Posts", Icon = SvgIcon.FileAdd, Url = "/admin/posts" },
    new DrawerItem { Text = "Pages", Icon = SvgIcon.FileData, Url = "/admin/pages" },
    new DrawerItem { Text = "Media", Icon = SvgIcon.Image, Url = "/admin/media" },  // NEW
    new DrawerItem { Text = "Analytics", Icon = SvgIcon.ChartLineMarkers, Url = "/admin/analytics" },
    new DrawerItem { Text = "Settings", Icon = SvgIcon.Gear, Url = "/admin/settings" }
};
```

**Features:**
- **Icon**: Uses Telerik's `SvgIcon.Image` for visual consistency
- **Position**: Placed between Pages and Analytics
- **URL**: `/admin/media`

#### 2. **GetCurrentPageName() Method Enhancement**
Updated to support routes with parameters:

```csharp
private string GetCurrentPageName()
{
    var currentPath = Navigation.ToBaseRelativePath(Navigation.Uri);
    
    // Check for exact match first
    var item = _navigationItems.FirstOrDefault(i => 
        currentPath.Equals(i.Url.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
        ?? _toolsItems.FirstOrDefault(i => 
            currentPath.Equals(i.Url.TrimStart('/'), StringComparison.OrdinalIgnoreCase));
    
    // If no exact match, check if current path starts with any nav item URL
    if (item == null)
    {
        item = _navigationItems.FirstOrDefault(i => 
            currentPath.StartsWith(i.Url.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
            ?? _toolsItems.FirstOrDefault(i => 
                currentPath.StartsWith(i.Url.TrimStart('/'), StringComparison.OrdinalIgnoreCase));
    }
    
    return item?.Text ?? "Dashboard";
}
```

**Why This Matters:**
- Handles routes like `/admin/media/images/2024` correctly
- Shows "Media" in breadcrumb when navigating folder structure
- Falls back to exact match, then prefix match

---

## 🎯 Testing the Media Manager

### Prerequisites

1. **Create the media directory** (if using FileSystem provider):
   ```powershell
   mkdir C:\Viblog\Media
   ```

2. **Start the application**:
   ```powershell
   dotnet run --project Viblog
   ```

3. **Navigate to admin**:
   - Go to `https://localhost:5001/admin`
   - Login with admin credentials
   - Click "Media" in the sidebar

### What You Should See

1. **Empty State**: If no files uploaded yet, you'll see:
   - Large image icon
   - "No media files" message
   - "Upload Files" button

2. **Navigation**: 
   - "Media" link in sidebar with image icon
   - Breadcrumb showing: "Home / Media"

3. **Three-Panel Layout**:
   - **Left Panel**: Folder tree (empty initially)
   - **Center Panel**: Media grid (empty initially)
   - **Right Panel**: Preview (hidden until item selected)

### Test Uploading

1. Click "Upload" button in header
2. Select one or more files (images, PDFs, etc.)
3. Click "Upload" in dialog
4. Watch progress bars
5. See uploaded files appear in grid

### Switching Storage Providers

To use Azure Blob Storage instead:

1. **Update appsettings.Development.json**:
   ```json
   {
     "MediaStorage": {
       "Provider": "BlobStorage",
       "BlobStorage": {
         "ConnectionString": "YOUR_AZURE_CONNECTION_STRING",
         "ContainerName": "Viblog-media-dev",
         "CdnUrl": ""
       }
     }
   }
   ```

2. **Restart the application**

---

## 📝 Next Steps (Paused)

We've completed Options 1 and 2. The following remain available:

### Option 3: Implement Missing File Operations
- Context menu for media items
- Move to folder dialog
- Delete confirmation
- Download functionality
- Edit metadata dialog

### Option 4: Add Unit Tests
- MediaFacade tests
- MediaService tests
- Storage repository tests

### Option 5: Production Deployment
- Set up Azure Blob Storage
- Configure CDN
- Update production appsettings

---

## 🔍 Troubleshooting

### Media Directory Not Found
**Error**: Files upload but can't be viewed
**Solution**: 
```powershell
mkdir C:\Viblog\Media
# Then restart the application
```

### Static Files Not Serving
**Check**: 
1. Directory exists: `C:\Viblog\Media`
2. Program.cs has static file middleware
3. Configuration is correct in appsettings

### Upload Fails
**Check**:
1. Directory permissions (write access)
2. File size limits (100MB default)
3. Check logs for specific error messages

---

## ✅ Summary

**Configuration**: ✅ Complete  
**Navigation**: ✅ Complete  
**Ready to Test**: ✅ Yes  
**Build Status**: ✅ No errors  

The Media Manager is now fully configured and accessible via the admin navigation!
