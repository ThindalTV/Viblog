using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Admin.Models;

/// <summary>
/// View model for the simplified media library
/// </summary>
public class MediaLibraryViewModel
{
    /// <summary>
    /// Selected media type filter
    /// </summary>
    public MediaTypeCategory? SelectedMediaType { get; set; }

    /// <summary>
    /// Search term for filtering media items
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Selected date folder (yyyyMM format)
    /// </summary>
    public string? SelectedDateFolder { get; set; }

    /// <summary>
    /// List of available date folders
    /// </summary>
    public List<DateFolderInfo> DateFolders { get; set; } = [];

    /// <summary>
    /// Current page of media items
    /// </summary>
    public List<MediaItem> MediaItems { get; set; } = [];

    /// <summary>
    /// Total count of media items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Items per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Whether data is currently loading
    /// </summary>
    public bool IsLoading { get; set; }

    /// <summary>
    /// Selected media item for preview/editing
    /// </summary>
    public MediaItem? SelectedItem { get; set; }
}

/// <summary>
/// Information about a date folder
/// </summary>
public class DateFolderInfo
{
    /// <summary>
    /// Date folder in yyyyMM format
    /// </summary>
    public string DateFolder { get; set; } = string.Empty;

    /// <summary>
    /// Display name (e.g., "January 2025")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Number of items in this folder
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Parse date folder to display name
    /// </summary>
    public static string GetDisplayName(string dateFolder)
    {
        if (dateFolder.Length != 6 || !int.TryParse(dateFolder, out _))
        {
            return dateFolder;
        }

        var year = dateFolder[..4];
        var monthNum = int.Parse(dateFolder[4..]);
        var monthName = new DateTime(int.Parse(year), monthNum, 1).ToString("MMMM");

        return $"{monthName} {year}";
    }
}
