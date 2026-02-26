using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Viblog.Admin.Facades;
using Viblog.Admin.Services.Auditing;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Data.Entities.Content;

namespace Viblog.Tests.Integration;

/// <summary>
/// Integration tests for page operations with audit logging
/// </summary>
public class PageAuditIntegrationTests : IClassFixture<PageTestFixture>
{
    private readonly PageTestFixture _fixture;
    private readonly PagesAdminFacade _pagesAdminFacade;
    private readonly IAuditLogService _auditLogService;

    public PageAuditIntegrationTests(PageTestFixture fixture)
    {
        _fixture = fixture;
        _pagesAdminFacade = _fixture.PagesAdminFacade;
        _auditLogService = _fixture.AuditLogService;
    }

    [Fact]
    public async Task CreatePage_LogsAuditEntry()
    {
        // Arrange
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "pages",
            Slug = $"test-page-{Guid.NewGuid()}",
            Draft = new PageContent { Title = "Test Page", Markdown = "This is a test page" },
            Live = null, // Not published yet
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _pagesAdminFacade.CreatePageAsync(page);

        // Assert - Wait a moment for async logging
        await Task.Delay(100);

        // Get recent audit logs
        var auditLogs = await _auditLogService.GetRecentActivityAsync(100);
        var pageCreatedLog = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.ContentCreated && 
            log.EntityId == page.Id);

        Assert.NotNull(pageCreatedLog);
        Assert.Equal(AuditAction.ContentCreated, pageCreatedLog.Action);
        Assert.Equal(EntityType.Page, pageCreatedLog.EntityType);
        Assert.Equal(page.Id, pageCreatedLog.EntityId);
        Assert.Equal(page.Slug, pageCreatedLog.EntityName);
        Assert.Contains(page.Slug, pageCreatedLog.Description);
        Assert.Equal(ActionResult.Success, pageCreatedLog.Result);
        Assert.Equal(_fixture.TestUserId, pageCreatedLog.UserId);
    }

    [Fact]
    public async Task UpdatePage_LogsAuditEntry()
    {
        // Arrange - Create a page first
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "pages",
            Slug = $"original-page-{Guid.NewGuid()}",
            Draft = new PageContent { Title = "Original", Markdown = "Original content" },
            Live = null, // Not published
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _pagesAdminFacade.CreatePageAsync(page);
        await Task.Delay(100);

        // Act - Update the page
        page.Draft.Markdown = "Updated content";
        page.UpdatedAt = DateTimeOffset.UtcNow;
        await _pagesAdminFacade.UpdatePageAsync(page);

        // Assert
        await Task.Delay(100);

        var auditLogs = await _auditLogService.GetRecentActivityAsync(100);
        var pageUpdatedLog = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.ContentUpdated && 
            log.EntityId == page.Id);

        Assert.NotNull(pageUpdatedLog);
        Assert.Equal(AuditAction.ContentUpdated, pageUpdatedLog.Action);
        Assert.Equal(EntityType.Page, pageUpdatedLog.EntityType);
        Assert.Equal(page.Id, pageUpdatedLog.EntityId);
        Assert.Equal(page.Slug, pageUpdatedLog.EntityName);
        Assert.Contains(page.Slug, pageUpdatedLog.Description);
        Assert.Equal(ActionResult.Success, pageUpdatedLog.Result);
    }

    [Fact]
    public async Task DeletePage_LogsAuditEntry()
    {
        // Arrange - Create a page first
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "pages",
            Slug = $"delete-page-{Guid.NewGuid()}",
            Draft = new PageContent { Title = "To Delete", Markdown = "This page will be deleted" },
            Live = null, // Not published
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _pagesAdminFacade.CreatePageAsync(page);
        await Task.Delay(100);

        // Act - Delete the page
        await _pagesAdminFacade.DeletePageAsync(page.Id, page.GroupKey);

        // Assert
        await Task.Delay(100);

        var auditLogs = await _auditLogService.GetRecentActivityAsync(100);
        var pageDeletedLog = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.ContentDeleted && 
            log.EntityId == page.Id);

        Assert.NotNull(pageDeletedLog);
        Assert.Equal(AuditAction.ContentDeleted, pageDeletedLog.Action);
        Assert.Equal(EntityType.Page, pageDeletedLog.EntityType);
        Assert.Equal(page.Id, pageDeletedLog.EntityId);
        Assert.Equal(page.Slug, pageDeletedLog.EntityName);
        Assert.Contains("Deleted page", pageDeletedLog.Description);
        Assert.Equal(ActionResult.Success, pageDeletedLog.Result);
    }

    [Fact]
    public async Task PublishPage_LogsAuditEntry()
    {
        // Arrange - Create an unpublished page
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "pages",
            Slug = $"publish-page-{Guid.NewGuid()}",
            Draft = new PageContent { Title = "Publish Test", Markdown = "This page will be published" },
            Live = null, // Not published
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _pagesAdminFacade.CreatePageAsync(page);
        await Task.Delay(100);

        // Act - Publish the page
        await _pagesAdminFacade.PublishPageNowAsync(page.Id);

        // Assert
        await Task.Delay(100);

        var auditLogs = await _auditLogService.GetRecentActivityAsync(100);
        var pagePublishedLog = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.ContentPublished && 
            log.EntityId == page.Id);

        Assert.NotNull(pagePublishedLog);
        Assert.Equal(AuditAction.ContentPublished, pagePublishedLog.Action);
        Assert.Equal(EntityType.Page, pagePublishedLog.EntityType);
        Assert.Equal(page.Id, pagePublishedLog.EntityId);
        Assert.Contains("Published", pagePublishedLog.Description);
        Assert.Equal(ActionResult.Success, pagePublishedLog.Result);
    }

    [Fact]
    public async Task UnpublishPage_LogsAuditEntry()
    {
        // Arrange - Create and publish a page
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "pages",
            Slug = $"unpublish-page-{Guid.NewGuid()}",
            Draft = new PageContent { Title = "Unpublish Test", Markdown = "This page will be unpublished" },
            Live = null, // Not published
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _pagesAdminFacade.CreatePageAsync(page);
        await _pagesAdminFacade.PublishPageNowAsync(page.Id);
        await Task.Delay(100);

        // Act - Unpublish the page
        await _pagesAdminFacade.UnpublishPageAsync(page.Id);

        // Assert
        await Task.Delay(100);

        var auditLogs = await _auditLogService.GetRecentActivityAsync(100);
        var pageUnpublishedLog = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.ContentUnpublished && 
            log.EntityId == page.Id);

        Assert.NotNull(pageUnpublishedLog);
        Assert.Equal(AuditAction.ContentUnpublished, pageUnpublishedLog.Action);
        Assert.Equal(EntityType.Page, pageUnpublishedLog.EntityType);
        Assert.Equal(page.Id, pageUnpublishedLog.EntityId);
        Assert.Contains("Unpublished", pageUnpublishedLog.Description);
        Assert.Equal(ActionResult.Success, pageUnpublishedLog.Result);
    }

    [Fact]
    public async Task PageLifecycle_CreatesCompleteAuditTrail()
    {
        // Arrange
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "pages",
            Slug = $"lifecycle-page-{Guid.NewGuid()}",
            Draft = new PageContent { Title = "Lifecycle", Markdown = "Original content" },
            Live = null, // Not published
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act - Full lifecycle
        await _pagesAdminFacade.CreatePageAsync(page);          // Create
        await Task.Delay(50);
        
        page.Draft.Markdown = "Updated content";
        await _pagesAdminFacade.UpdatePageAsync(page);          // Update
        await Task.Delay(50);
        
        await _pagesAdminFacade.PublishPageNowAsync(page.Id);   // Publish
        await Task.Delay(50);
        
        await _pagesAdminFacade.UnpublishPageAsync(page.Id);    // Unpublish
        await Task.Delay(50);
        
        await _pagesAdminFacade.DeletePageAsync(page.Id, page.GroupKey); // Delete

        // Assert
        await Task.Delay(100);

        var auditLogs = await _auditLogService.GetRecentActivityAsync(100);
        var pageLogs = auditLogs.Where(log => log.EntityId == page.Id).ToList();

        // Should have 5 audit entries for this page
        Assert.True(pageLogs.Count >= 5, $"Expected at least 5 audit logs, found {pageLogs.Count}");
        
        Assert.Contains(pageLogs, log => log.Action == AuditAction.ContentCreated);
        Assert.Contains(pageLogs, log => log.Action == AuditAction.ContentUpdated);
        Assert.Contains(pageLogs, log => log.Action == AuditAction.ContentPublished);
        Assert.Contains(pageLogs, log => log.Action == AuditAction.ContentUnpublished);
        Assert.Contains(pageLogs, log => log.Action == AuditAction.ContentDeleted);
    }
}

/// <summary>
/// Test fixture for page integration tests with audit logging
/// </summary>
public class PageTestFixture : IDisposable
{
    private readonly string _testDataPath;
    private bool _disposed;

    public IPageRepository PageRepository { get; }
    public IAuditLogService AuditLogService { get; }
    public PagesAdminFacade PagesAdminFacade { get; }
    public string TestUserId { get; } = "test-user-456";
    public string TestUserName { get; } = "Page Test User";
    public string TestUserEmail { get; } = "pagetest@example.com";

    public PageTestFixture()
    {
        // Create unique temporary directory
        _testDataPath = Path.Combine(
            Path.GetTempPath(),
            "Viblog.Tests",
            $"PageAudit_{Guid.NewGuid()}");

        Directory.CreateDirectory(_testDataPath);

        // Create logger factories
        var pageRepoLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var auditRepoLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var auditServiceLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var versionServiceLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var schedulingServiceLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        var auditLogRepository = Mock.Of<IAuditLogRepository>();

        // Initialize audit service
        AuditLogService = new AuditLogService(
            auditLogRepository,
            auditServiceLoggerFactory.CreateLogger<AuditLogService>());

        // Create mock HttpContextAccessor with test user
        var httpContextAccessor = CreateMockHttpContextAccessor();

        // Initialize version and scheduling services (version repos mocked — not under test here)
        var versionService = new Viblog.Shared.Services.Content.ContentVersionService(
            Mock.Of<IBlogPostVersionRepository>(),
            Mock.Of<IPageVersionRepository>(),
            versionServiceLoggerFactory.CreateLogger<Viblog.Shared.Services.Content.ContentVersionService>());

        var schedulingService = new Viblog.Shared.Services.Content.ContentSchedulingService(
            versionService,
            schedulingServiceLoggerFactory.CreateLogger<Viblog.Shared.Services.Content.ContentSchedulingService>());

        // Initialize facade
        PagesAdminFacade = new PagesAdminFacade(
            PageRepository,
            schedulingService,
            AuditLogService,
            httpContextAccessor);
    }

    private IHttpContextAccessor CreateMockHttpContextAccessor()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Name, TestUserName),
            new Claim(ClaimTypes.Email, TestUserEmail)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        var mock = new Mock<IHttpContextAccessor>();
        mock.Setup(x => x.HttpContext).Returns(httpContext);

        return mock.Object;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            try
            {
                if (Directory.Exists(_testDataPath))
                {
                    Directory.Delete(_testDataPath, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _disposed = true;
    }
}
