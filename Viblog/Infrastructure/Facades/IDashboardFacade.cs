using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Infrastructure.Facades;

/// <summary>
/// Facade interface for admin dashboard read-only queries.
/// </summary>
public interface IDashboardFacade
{
    /// <summary>
    /// Get a snapshot of post counts and total views across all posts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PostStats> GetPostStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the top 10 published posts by view count, optionally filtered by publish date.
    /// </summary>
    /// <param name="filter">Date range filter applied to PublishedAt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Up to 10 published posts ordered by ViewCount descending.</returns>
    Task<IReadOnlyList<BlogPost>> GetTopPostsByViewsAsync(
        DashboardDateFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total view counts grouped by the calendar month posts were published.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>View totals per published month, ordered chronologically.</returns>
    Task<IReadOnlyList<ViewsByMonthData>> GetViewsByPublishedMonthAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get upcoming scheduled posts ordered by their scheduled publish date ascending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Up to 5 scheduled posts.</returns>
    Task<IReadOnlyList<BlogPost>> GetScheduledPostsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the most recent audit log entries across all users and entity types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The 10 most recent audit log entries.</returns>
    Task<IReadOnlyList<AuditLog>> GetRecentActivityAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot of post counts and cumulative view total.
/// </summary>
/// <param name="PublishedCount">Number of currently published posts.</param>
/// <param name="DraftCount">Number of posts in draft (not published, not scheduled).</param>
/// <param name="ScheduledCount">Number of posts scheduled for future publishing.</param>
/// <param name="TotalViews">Sum of ViewCount across all posts.</param>
public record PostStats(int PublishedCount, int DraftCount, int ScheduledCount, int TotalViews);

/// <summary>
/// Date range filter for dashboard queries.
/// </summary>
public enum DashboardDateFilter
{
    LastDay,
    LastWeek,
    LastMonth,
    LastYear,
    Ever
}

/// <summary>
/// Total blog post views for a single calendar month.
/// </summary>
/// <param name="Month">Display name of the month (e.g. "June").</param>
/// <param name="TotalViews">Sum of ViewCount for all posts published that month.</param>
public record ViewsByMonthData(string Month, int TotalViews);
