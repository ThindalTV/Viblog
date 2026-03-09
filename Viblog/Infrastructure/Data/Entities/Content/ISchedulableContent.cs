using Viblog.Infrastructure.Extensions;

namespace Viblog.Infrastructure.Data.Entities.Content;

/// <summary>
/// Marker interface for content that supports scheduling and publishing.
/// </summary>
public interface ISchedulableContent
{
    /// <summary>
    /// Publishing schedule information.
    /// </summary>
    ContentSchedule Schedule { get; set; }

    /// <summary>
    /// When the content was last updated.
    /// </summary>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Unique identifier for the content.
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Stored flag indicating Live content exists. Set by <see cref="SchedulableContentExtensions.SetLiveContent"/>.
    /// Used directly in EF Core queries — do not compute from <c>Live != null</c>.
    /// </summary>
    bool IsPublished { get; set; }
}
