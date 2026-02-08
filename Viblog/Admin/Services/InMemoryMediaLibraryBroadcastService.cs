namespace Viblog.Admin.Services;

/// <summary>
/// In-memory implementation of media library broadcast service for single-server scenarios
/// </summary>
public class InMemoryMediaLibraryBroadcastService : IMediaLibraryBroadcastService
{
    /// <summary>
    /// Event raised when folder contents change
    /// </summary>
    public event Func<FolderContentsChangedEventArgs, Task>? OnFolderContentsChanged;

    /// <summary>
    /// Broadcast folder contents change to all subscribers
    /// </summary>
    public async Task BroadcastFolderContentsChangedAsync(FolderContentsChangedEventArgs args)
    {
        if (OnFolderContentsChanged != null)
        {
            // Invoke all handlers, catching exceptions to prevent one failure from breaking others
            foreach (var handler in OnFolderContentsChanged.GetInvocationList())
            {
                try
                {
                    await ((Func<FolderContentsChangedEventArgs, Task>)handler).Invoke(args);
                }
                catch (Exception ex)
                {
                    // Log but don't break other subscribers
                    Console.WriteLine($"Error in folder contents changed handler: {ex}");
                }
            }
        }
    }
}
