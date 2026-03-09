using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Viblog.Admin.Facades;
using Viblog.Admin.Services.Auditing;
using Viblog.Infrastructure.Auditing;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Entities.Content;
using Viblog.Infrastructure.Data.Repositories;

namespace Viblog.Tests.Integration;

/// <summary>
/// Integration tests for blog post operations with audit logging
/// </summary>
public class BlogPostAuditIntegrationTests : IClassFixture<BlogTestFixture>
{
    private readonly BlogTestFixture _fixture;
    private readonly PostsAdminFacade _postsAdminFacade;
    private readonly IAuditLogService _auditLogService;

    public BlogPostAuditIntegrationTests(BlogTestFixture fixture)
    {
        _fixture = fixture;
        _postsAdminFacade = _fixture.PostsAdminFacade;
        _auditLogService = _fixture.AuditLogService;
    }

    [Fact]
    public async Task CreatePost_LogsAuditEntry()
    {
        // Arrange
        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "blog-posts",
            Slug = $"test-post-{Guid.NewGuid()}",
            Draft = new BlogPostContent
            {
                Title = "Test Post for Audit",
                Content = "This is a test post"
            },
            Live = null, // Not published
            IsFeatured = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _postsAdminFacade.CreatePostAsync(post);

        // Assert - Wait a moment for async logging
        await Task.Delay(100);

        // Get recent audit logs
        var auditLogs = await _auditLogService.GetRecentActivityAsync(100);
        var postCreatedLog = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.ContentCreated && 
            log.EntityId == post.Id);

        Assert.NotNull(postCreatedLog);
        Assert.Equal(AuditAction.ContentCreated, postCreatedLog.Action);
        Assert.Equal(EntityType.BlogPost, postCreatedLog.EntityType);
        Assert.Equal(post.Id, postCreatedLog.EntityId);
        Assert.Equal(post.Draft.Title, postCreatedLog.EntityName);
        Assert.Contains(post.Draft.Title, postCreatedLog.Description);
        Assert.Equal(ActionResult.Success, postCreatedLog.Result);
        Assert.Equal(_fixture.TestUserId, postCreatedLog.UserId);
    }

    [Fact]
    public async Task UpdatePost_LogsAuditEntry()
    {
        // Arrange - Create a post first
        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "blog-posts",
            Slug = $"original-{Guid.NewGuid()}",
            Draft = new BlogPostContent
            {
                Title = "Original Title",
                Content = "Original content"
            },
            Live = null, // Not published
            IsFeatured = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _postsAdminFacade.CreatePostAsync(post);
        await Task.Delay(100);

        // Act - Update the post
        post.Draft.Title = "Updated Title";
        post.Draft.Content = "Updated content";
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _postsAdminFacade.UpdatePostAsync(post);

        // Assert
        await Task.Delay(100);

        var auditLogs = await _auditLogService.GetRecentActivityAsync(100);
        var postUpdatedLog = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.ContentUpdated && 
            log.EntityId == post.Id);

        Assert.NotNull(postUpdatedLog);
        Assert.Equal(AuditAction.ContentUpdated, postUpdatedLog.Action);
        Assert.Equal(EntityType.BlogPost, postUpdatedLog.EntityType);
        Assert.Equal(post.Id, postUpdatedLog.EntityId);
        Assert.Equal("Updated Title", postUpdatedLog.EntityName);
        Assert.Contains("Updated Title", postUpdatedLog.Description);
        Assert.Equal(ActionResult.Success, postUpdatedLog.Result);
    }

    [Fact]
    public async Task DeletePost_LogsAuditEntry()
    {
        // Arrange - Create a post first
        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "blog-posts",
            Slug = $"delete-{Guid.NewGuid()}",
            Draft = new BlogPostContent
            {
                Title = "Post to Delete",
                Content = "This post will be deleted"
            },
            Live = null, // Not published
            IsFeatured = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _postsAdminFacade.CreatePostAsync(post);
        await Task.Delay(100);

        // Act - Delete the post
        await _postsAdminFacade.DeletePostAsync(post.Id, post.GroupKey);

        // Assert
        await Task.Delay(100);

        var auditLogs = await _auditLogService.GetRecentActivityAsync(100);
        var postDeletedLog = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.ContentDeleted && 
            log.EntityId == post.Id);

        Assert.NotNull(postDeletedLog);
        Assert.Equal(AuditAction.ContentDeleted, postDeletedLog.Action);
        Assert.Equal(EntityType.BlogPost, postDeletedLog.EntityType);
        Assert.Equal(post.Id, postDeletedLog.EntityId);
        Assert.Equal(post.Draft.Title, postDeletedLog.EntityName);
        Assert.Contains("Deleted blog post", postDeletedLog.Description);
        Assert.Equal(ActionResult.Success, postDeletedLog.Result);
    }

    [Fact]
    public async Task MultiplePostOperations_CreatesSeparateAuditLogs()
    {
        // Arrange
        var post1 = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "blog-posts",
            Slug = $"first-{Guid.NewGuid()}",
            Draft = new BlogPostContent
            {
                Title = "First Post",
                Content = "First content"
            },
            Live = null, // Not published
            IsFeatured = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var post2 = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "blog-posts",
            Slug = $"second-{Guid.NewGuid()}",
            Draft = new BlogPostContent
            {
                Title = "Second Post",
                Content = "Second content"
            },
            Live = null, // Not published
            IsFeatured = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _postsAdminFacade.CreatePostAsync(post1);
        await _postsAdminFacade.CreatePostAsync(post2);
        await _postsAdminFacade.UpdatePostAsync(post1);

        // Assert
        await Task.Delay(100);

        var auditLogs = await _auditLogService.GetRecentActivityAsync(100);
        
        // Should have 2 creates and 1 update
        var post1Created = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.ContentCreated && log.EntityId == post1.Id);
        var post2Created = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.ContentCreated && log.EntityId == post2.Id);
        var post1Updated = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.ContentUpdated && log.EntityId == post1.Id);

        Assert.NotNull(post1Created);
        Assert.NotNull(post2Created);
        Assert.NotNull(post1Updated);
    }
}

/// <summary>
/// Test fixture for blog post integration tests with audit logging
/// </summary>
public class BlogTestFixture : IDisposable
{
    private readonly string _testDataPath;
    private bool _disposed;

    public IBlogPostRepository BlogPostRepository { get; }
    public IAuditLogService AuditLogService { get; }
    public PostsAdminFacade PostsAdminFacade { get; }
    public string TestUserId { get; } = "test-user-123";
    public string TestUserName { get; } = "Test User";
    public string TestUserEmail { get; } = "test@example.com";

    public BlogTestFixture()
    {
        // Create unique temporary directory
        _testDataPath = Path.Combine(
            Path.GetTempPath(),
            "Viblog.Tests",
            $"BlogAudit_{Guid.NewGuid()}");

        Directory.CreateDirectory(_testDataPath);

        // Create logger factories
        var auditServiceLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var versionServiceLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var schedulingServiceLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Initialize blog post repository with in-memory store
        var blogPostStore = new Dictionary<string, BlogPost>();
        var blogPostRepositoryMock = new Mock<IBlogPostRepository>();
        blogPostRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<BlogPost>(), It.IsAny<CancellationToken>()))
            .Callback<BlogPost, CancellationToken>((post, _) => blogPostStore[post.Id] = post)
            .Returns(Task.CompletedTask);
        blogPostRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<BlogPost>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        blogPostRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        blogPostRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((id, _, __) => Task.FromResult<BlogPost?>(blogPostStore.GetValueOrDefault(id)));
        blogPostRepositoryMock
            .Setup(r => r.GetByIdWithoutPartitionKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((id, _) => Task.FromResult<BlogPost?>(blogPostStore.GetValueOrDefault(id)));
        blogPostRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        BlogPostRepository = blogPostRepositoryMock.Object;

        // Initialize audit log repository with in-memory store
        var auditLogStore = new List<AuditLog>();
        var auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        auditLogRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, _) => auditLogStore.Add(log))
            .Returns(Task.CompletedTask);
        auditLogRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        auditLogRepositoryMock
            .Setup(r => r.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, __) => Task.FromResult<IEnumerable<AuditLog>>(auditLogStore));

        // Initialize audit service
        AuditLogService = new AuditLogService(
            auditLogRepositoryMock.Object,
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
        PostsAdminFacade = new PostsAdminFacade(
            BlogPostRepository,
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
