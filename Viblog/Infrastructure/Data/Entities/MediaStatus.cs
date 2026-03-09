namespace Viblog.Infrastructure.Data.Entities;

/// <summary>
/// Represents the status of a media item in the system
/// </summary>
public enum MediaStatus
{
    /// <summary>
    /// Media is currently being uploaded
    /// </summary>
    Uploading,

    /// <summary>
    /// Media is available for use
    /// </summary>
    Available,

    /// <summary>
    /// Media is currently in use (referenced by blog posts or other content)
    /// </summary>
    InUse,

    /// <summary>
    /// Media has been marked as deleted
    /// </summary>
    Deleted
}
