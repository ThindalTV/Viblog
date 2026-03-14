using Viblog.Infrastructure.Data.Common;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Infrastructure.Data.Repositories;
using Viblog.Infrastructure.Facades;

namespace Viblog.Admin.Facades;

/// <summary>
/// Facade implementation for admin dashboard read-only queries.
/// </summary>
public class DashboardFacade : IDashboardFacade
{
    private const int TopPostsLimit = 10;
    private const int ScheduledPostsLimit = 5;
    private const int RecentActivityCount = 10;
    private const int MaxQueryPageSize = 100;

    private readonly IBlogPostRepository _blogPostRepository;
    private readonly IAuditLogFacade _auditLogFacade;

    public DashboardFacade(IBlogPostRepository blogPostRepository, IAuditLogFacade auditLogFacade)
    {
        _blogPostRepository = blogPostRepository ?? throw new ArgumentNullException(nameof(blogPostRepository));
        _auditLogFacade = auditLogFacade ?? throw new ArgumentNullException(nameof(auditLogFacade));
    }

    /// <inheritdoc/>
    public virtual async Task<PostStats> GetPostStatsAsync(CancellationToken cancellationToken = default)
    {
        // Load all posts in batches and compute all four stats in a single pass.
        var allPosts = new List<BlogPost>();
        var pageNumber = 1;
        PagedResult<BlogPost> page;

        do
        {
            page = await _blogPostRepository.GetAllAsync<DateTimeOffset>(
                new PagingParameters(pageNumber, MaxQueryPageSize),
                orderBy: null,
                ascending: false,
                includeDeleted: false,
                cancellationToken);

            allPosts.AddRange(page.Items);
            pageNumber++;
        }
        while (page.HasNextPage);

        return new PostStats(
            PublishedCount: allPosts.Count(p => p.IsPublished),
            DraftCount: allPosts.Count(p => !p.IsPublished && p.Schedule.Status == ContentStatus.Draft),
            ScheduledCount: allPosts.Count(p => p.Schedule.Status == ContentStatus.Scheduled),
            TotalViews: allPosts.Sum(p => p.ViewCount));
    }

    /// <inheritdoc/>
    public virtual async Task<IReadOnlyList<BlogPost>> GetTopPostsByViewsAsync(
        DashboardDateFilter filter,
        CancellationToken cancellationToken = default)
    {
        var cutoff = GetCutoffDate(filter);

        var result = await _blogPostRepository.FindAsync(
            p => p.IsPublished && (cutoff == null || p.PublishedAt >= cutoff),
            new PagingParameters(1, TopPostsLimit),
            p => p.ViewCount,
            ascending: false,
            includeDeleted: false,
            cancellationToken);

        return result.Items.ToList();
    }

    /// <inheritdoc/>
    public virtual async Task<IReadOnlyList<ViewsByMonthData>> GetViewsByPublishedMonthAsync(
        CancellationToken cancellationToken = default)
    {
        var allPosts = new List<BlogPost>();
        var pageNumber = 1;
        PagedResult<BlogPost> page;

        do
        {
            page = await _blogPostRepository.FindAsync(
                p => p.IsPublished && p.PublishedAt != null,
                new PagingParameters(pageNumber, MaxQueryPageSize),
                p => p.PublishedAt,
                ascending: true,
                includeDeleted: false,
                cancellationToken);

            allPosts.AddRange(page.Items);
            pageNumber++;
        }
        while (page.HasNextPage);

        return allPosts
            .Where(p => p.PublishedAt.HasValue)
            .GroupBy(p => new DateTimeOffset(
                p.PublishedAt!.Value.Year,
                p.PublishedAt.Value.Month,
                1, 0, 0, 0,
                TimeSpan.Zero))
            .OrderBy(g => g.Key)
            .Select(g => new ViewsByMonthData(g.Key.ToString("MMMM"), g.Sum(p => p.ViewCount)))
            .ToList();
    }

    /// <inheritdoc/>
    public virtual async Task<IReadOnlyList<BlogPost>> GetScheduledPostsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _blogPostRepository.FindAsync(
            p => p.Schedule.Status == ContentStatus.Scheduled,
            new PagingParameters(1, ScheduledPostsLimit),
            p => p.Schedule.ScheduledPublishDate,
            ascending: true,
            includeDeleted: false,
            cancellationToken);

        return result.Items.ToList();
    }

    /// <inheritdoc/>
    public virtual async Task<IReadOnlyList<AuditLog>> GetRecentActivityAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = await _auditLogFacade.GetRecentActivityAsync(RecentActivityCount, cancellationToken);
        return entries.ToList();
    }

    private static DateTimeOffset? GetCutoffDate(DashboardDateFilter filter) => filter switch
    {
        DashboardDateFilter.LastDay   => DateTimeOffset.UtcNow.AddDays(-1),
        DashboardDateFilter.LastWeek  => DateTimeOffset.UtcNow.AddDays(-7),
        DashboardDateFilter.LastMonth => DateTimeOffset.UtcNow.AddMonths(-1),
        DashboardDateFilter.LastYear  => DateTimeOffset.UtcNow.AddYears(-1),
        DashboardDateFilter.Ever or _ => null
    };
}
