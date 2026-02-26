using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly IPageRepository _pageRepository;
    private readonly ContentSchedulingService _schedulingService;
    private readonly ILogger<ContentPublishingBackgroundService> _logger;
    private readonly ContentPublishingOptions _options;

    public ContentPublishingBackgroundService(
        IBlogPostRepository blogPostRepository,
        IPageRepository pageRepository,
        ContentSchedulingService schedulingService,
        ILogger<ContentPublishingBackgroundService> logger,
        IOptions<ContentPublishingOptions> options)
    {
        _blogPostRepository = blogPostRepository;
        _pageRepository = pageRepository;
        _schedulingService = schedulingService;
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
        var now = DateTimeOffset.UtcNow;
        var publishedCount = 0;

        // Process scheduled BlogPosts
        publishedCount += await ProcessScheduledBlogPostsAsync(now, cancellationToken);

        // Process scheduled Pages
        publishedCount += await ProcessScheduledPagesAsync(now, cancellationToken);

        if (publishedCount > 0)
        {
            _logger.LogInformation("Published {Count} scheduled items", publishedCount);
        }
    }

    private async Task<int> ProcessScheduledBlogPostsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        // TODO: rewire to IBlogPostRepository.GetScheduledPostsReadyToPublishAsync in Phase 3
        await Task.CompletedTask;
        return 0;
    }

    private async Task<int> ProcessScheduledPagesAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        // TODO: rewire to IPageRepository.GetScheduledPagesReadyToPublishAsync in Phase 3
        await Task.CompletedTask;
        return 0;
    }
}

/// <summary>
/// Configuration options for ContentPublishingBackgroundService.
/// </summary>
public class ContentPublishingOptions
{
    /// <summary>
    /// How often to check for scheduled content (in minutes).
    /// Default: 1 minute.
    /// </summary>
    public int CheckIntervalMinutes { get; set; } = 1;
}
