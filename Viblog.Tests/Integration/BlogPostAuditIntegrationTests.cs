using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Viblog.Admin.Facades;
using Viblog.Admin.Services.Auditing;
using Viblog.Data.Filesystem.Data.Repositories;
using Viblog.Infrastructure.Shared.Auditing;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Infrastructure.Shared.Data.Repositories;

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
            log.Action == AuditAction.PostCreated && 
            log.EntityId == post.Id);

        Assert.NotNull(postCreatedLog);
        Assert.Equal(AuditAction.PostCreated, postCreatedLog.Action);
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
            log.Action == AuditAction.PostUpdated && 
            log.EntityId == post.Id);

        Assert.NotNull(postUpdatedLog);
        Assert.Equal(AuditAction.PostUpdated, postUpdatedLog.Action);
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
            log.Action == AuditAction.PostDeleted && 
            log.EntityId == post.Id);

        Assert.NotNull(postDeletedLog);
        Assert.Equal(AuditAction.PostDeleted, postDeletedLog.Action);
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
            log.Action == AuditAction.PostCreated && log.EntityId == post1.Id);
        var post2Created = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.PostCreated && log.EntityId == post2.Id);
        var post1Updated = auditLogs.FirstOrDefault(log => 
            log.Action == AuditAction.PostUpdated && log.EntityId == post1.Id);

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
        var blogRepoLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var auditRepoLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var auditServiceLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Create options
        var storageOptions = Microsoft.Extensions.Options.Options.Create(
            new Viblog.Data.Filesystem.Configuration.FilesystemStorageOptions
            {
                RootPath = _testDataPath
            });

        // Initialize repositories
        BlogPostRepository = new FileSystemBlogPostRepository(
            storageOptions,
            blogRepoLoggerFactory.CreateLogger<Viblog.Data.Filesystem.Data.Repositories.FilesystemRepository<BlogPost>>());

        var auditLogRepository = new FileSystemAuditLogRepository(
            storageOptions,
            auditRepoLoggerFactory.CreateLogger<Viblog.Data.Filesystem.Data.Repositories.FilesystemRepository<AuditLog>>());

        // Initialize audit service
        AuditLogService = new AuditLogService(
            auditLogRepository,
            auditServiceLoggerFactory.CreateLogger<AuditLogService>());

        // Create mock HttpContextAccessor with test user
        var httpContextAccessor = CreateMockHttpContextAccessor();

        // Initialize facade
        PostsAdminFacade = new PostsAdminFacade(
            BlogPostRepository,
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
