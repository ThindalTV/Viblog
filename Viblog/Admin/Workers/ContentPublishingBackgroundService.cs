using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Shared.Services.Content;

namespace Viblog.Admin.Workers;

/// <summary>
/// Background service that automatically publishes scheduled content.
/// Handles both BlogPost and Page entities.
/// Runs on a configurable interval (default: 1 minute).
/// </summary>
public class ContentPublishingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContentPublishingBackgroundService> _logger;
    private readonly ContentPublishingOptions _options;

    public ContentPublishingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ContentPublishingBackgroundService> logger,
        IOptions<ContentPublishingOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ContentPublishingBackgroundService starting (interval: {Interval} minutes)", 
            _options.CheckIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledContentAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled content");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.CheckIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("ContentPublishingBackgroundService stopping");
    }

    private async Task ProcessScheduledContentAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var blogPostRepository = scope.ServiceProvider.GetRequiredService<IBlogPostRepository>();
        var pageRepository = scope.ServiceProvider.GetRequiredService<IPageRepository>();
        var schedulingService = scope.ServiceProvider.GetRequiredService<ContentSchedulingService>();
        var auditLogService = scope.ServiceProvider.GetService<IAuditLogService>();

        var now = DateTimeOffset.UtcNow;
        var publishedCount = 0;

        // Process scheduled BlogPosts
        publishedCount += await ProcessScheduledBlogPostsAsync(blogPostRepository, schedulingService, auditLogService, now, cancellationToken);

        // Process scheduled Pages
        publishedCount += await ProcessScheduledPagesAsync(pageRepository, schedulingService, auditLogService, now, cancellationToken);

        if (publishedCount > 0)
        {
            _logger.LogInformation("Published {Count} scheduled items", publishedCount);
        }
    }

    private async Task<int> ProcessScheduledBlogPostsAsync(
        IBlogPostRepository blogPostRepository,
        ContentSchedulingService schedulingService,
        IAuditLogService? auditLogService,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var readyToPublish = await blogPostRepository.GetScheduledPostsReadyToPublishAsync(cancellationToken);
        var publishedCount = 0;

        foreach (var post in readyToPublish)
        {
            try
            {
                var published = await schedulingService.PromoteIfReadyAsync(post, "system", "System", cancellationToken);
                if (!published)
                {
                    continue;
                }

                await blogPostRepository.UpdateAsync(post, cancellationToken);
                await blogPostRepository.SaveChangesAsync(cancellationToken);

                if (auditLogService != null)
                {
                    await auditLogService.LogActionAsync(
                        userId: "system",
                        userName: "System",
                        userEmail: "system@viblog.internal",
                        action: AuditAction.ContentPublished,
                        entityType: EntityType.BlogPost,
                        entityId: post.Id,
                        entityName: post.Draft.Title,
                        description: $"Scheduled publish: BlogPost '{post.Draft.Title}'",
                        result: ActionResult.Success,
                        cancellationToken: cancellationToken);
                }

                publishedCount++;
                _logger.LogInformation("Auto-published BlogPost {PostId} ({Title})", post.Id, post.Draft.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-publish BlogPost {PostId}", post.Id);
            }
        }

        return publishedCount;
    }

    private async Task<int> ProcessScheduledPagesAsync(IPageRepository pageRepository, ContentSchedulingService schedulingService, IAuditLogService? auditLogService, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var readyToPublish = await pageRepository.GetScheduledPagesReadyToPublishAsync(cancellationToken);
        var publishedCount = 0;

        foreach (var page in readyToPublish)
        {
            try
            {
                var published = await schedulingService.PromoteIfReadyAsync(page, "system", "System", cancellationToken);
                if (!published)
                {
                    continue;
                }

                await pageRepository.UpdateAsync(page, cancellationToken);
                await pageRepository.SaveChangesAsync(cancellationToken);

                if (auditLogService != null)
                {
                    await auditLogService.LogActionAsync(
                        userId: "system",
                        userName: "System",
                        userEmail: "system@viblog.internal",
                        action: AuditAction.ContentPublished,
                        entityType: EntityType.Page,
                        entityId: page.Id,
                        entityName: page.Slug,
                        description: $"Scheduled publish: Page '{page.Slug}'",
                        result: ActionResult.Success,
                        cancellationToken: cancellationToken);
                }

                publishedCount++;
                _logger.LogInformation("Auto-published Page {PageId} ({Slug})", page.Id, page.Slug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-publish Page {PageId}", page.Id);
            }
        }

        return publishedCount;
    }
}

/// <summary>
/// Configuration options for ContentPublishingBackgroundService.
/// </summary>
public class ContentPublishingOptions
{
    /// <summary>
    /// How often to check for scheduled content (in minutes).
    /// Default: 5 minutes.
    /// </summary>
    public int CheckIntervalMinutes { get; set; } = 5;
}
