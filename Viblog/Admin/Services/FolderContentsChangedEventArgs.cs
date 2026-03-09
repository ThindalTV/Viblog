using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Admin.Services;

/// <summary>
/// Event arguments for folder content changes
/// </summary>
public class FolderContentsChangedEventArgs
{
    /// <summary>
    /// The date folder that was changed (yyyyMM format), or null if all folders affected
    /// </summary>
    public string? DateFolder { get; init; }

    /// <summary>
    /// The media type category affected, or null if all types affected
    /// </summary>
    public MediaTypeCategory? MediaType { get; init; }

    /// <summary>
    /// Type of change that occurred
    /// </summary>
    public FolderChangeType ChangeType { get; init; }

    /// <summary>
    /// Creates event args for a specific folder change
    /// </summary>
    public static FolderContentsChangedEventArgs ForFolder(string? dateFolder, MediaTypeCategory? mediaType, FolderChangeType changeType)
    {
        return new FolderContentsChangedEventArgs
        {
            DateFolder = dateFolder,
            MediaType = mediaType,
            ChangeType = changeType
        };
    }

    /// <summary>
    /// Creates event args for a change affecting all folders
    /// </summary>
    public static FolderContentsChangedEventArgs ForAllFolders(FolderChangeType changeType)
    {
        return new FolderContentsChangedEventArgs
        {
            DateFolder = null,
            MediaType = null,
            ChangeType = changeType
        };
    }
}

/// <summary>
/// Type of change that occurred to folder contents
/// </summary>
public enum FolderChangeType
{
    /// <summary>
    /// Files were uploaded
    /// </summary>
    Upload,

    /// <summary>
    /// A file was deleted
    /// </summary>
    Delete,

    /// <summary>
    /// General refresh requested
    /// </summary>
    Refresh
}
