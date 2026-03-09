
namespace Viblog.Admin.Services;

/// <summary>
/// Service for broadcasting media library folder content changes across all users/circuits
/// </summary>
public interface IMediaLibraryBroadcastService
{
    /// <summary>
    /// Event raised when folder contents change (upload, delete, etc.)
    /// </summary>
    event Func<FolderContentsChangedEventArgs, Task>? OnFolderContentsChanged;

    /// <summary>
    /// Broadcast that folder contents have changed
    /// </summary>
    /// <param name="args">Details about what changed</param>
    Task BroadcastFolderContentsChangedAsync(FolderContentsChangedEventArgs args);
}
