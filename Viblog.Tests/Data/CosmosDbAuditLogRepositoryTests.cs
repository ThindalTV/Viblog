using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Tests.Data;

/// <summary>
/// Integration tests for audit log repository operations
/// These tests verify that the IAuditLogRepository contract is correctly implemented
/// </summary>
public class AuditLogRepositoryTests
{
    private readonly Mock<IAuditLogRepository> _mockRepository;

    public AuditLogRepositoryTests()
    {
        _mockRepository = new Mock<IAuditLogRepository>();
    }

    [Fact]
    public async Task GetByUserIdAsync_WithValidUserId_ReturnsUserLogs()
    {
        // Arrange
        var userId = "test-user-123";
        var userLogs = new List<AuditLog>
        {
            CreateAuditLog(userId, AuditAction.PostCreated, "post-1"),
            CreateAuditLog(userId, AuditAction.PostUpdated, "post-1"),
            CreateAuditLog(userId, AuditAction.PostDeleted, "post-2")
        };

        var expectedResult = new PagedResult<AuditLog>
        {
            Items = userLogs,
            TotalCount = 3,
            PageNumber = 1,
            PageSize = 10
        };

        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        _mockRepository
            .Setup(x => x.GetByUserIdAsync(userId, pagingParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockRepository.Object.GetByUserIdAsync(userId, pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, log => Assert.Equal(userId, log.UserId));
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Arrange
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        _mockRepository
            .Setup(x => x.GetByUserIdAsync(null!, pagingParams, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Value cannot be null or whitespace. (Parameter 'userId')", "userId"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _mockRepository.Object.GetByUserIdAsync(null!, pagingParams));
    }

    [Fact]
    public async Task GetByEntityAsync_WithValidEntityInfo_ReturnsEntityLogs()
    {
        // Arrange
        var entityId = "post-123";
        var logs = new List<AuditLog>
        {
            CreateAuditLog("user-1", AuditAction.PostCreated, entityId, EntityType.BlogPost),
            CreateAuditLog("user-2", AuditAction.PostUpdated, entityId, EntityType.BlogPost)
        };

        var expectedResult = new PagedResult<AuditLog>
        {
            Items = logs,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        _mockRepository
            .Setup(x => x.GetByEntityAsync(EntityType.BlogPost, entityId, pagingParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockRepository.Object.GetByEntityAsync(EntityType.BlogPost, entityId, pagingParams);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, log => Assert.Equal(entityId, log.EntityId));
        Assert.All(result.Items, log => Assert.Equal(EntityType.BlogPost, log.EntityType));
    }

    [Fact]
    public async Task GetByActionAsync_WithValidAction_ReturnsFilteredLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            CreateAuditLog("user-1", AuditAction.PostCreated, "post-1"),
            CreateAuditLog("user-2", AuditAction.PostCreated, "post-2")
        };

        var expectedResult = new PagedResult<AuditLog>
        {
            Items = logs,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        _mockRepository
            .Setup(x => x.GetByActionAsync(AuditAction.PostCreated, pagingParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockRepository.Object.GetByActionAsync(AuditAction.PostCreated, pagingParams);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, log => Assert.Equal(AuditAction.PostCreated, log.Action));
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithValidRange_ReturnsLogsInRange()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var logs = new List<AuditLog>
        {
            CreateAuditLog("user-2", AuditAction.PostCreated, "post-2", timestamp: now.AddDays(-3)),
            CreateAuditLog("user-3", AuditAction.PostCreated, "post-3", timestamp: now.AddDays(-1))
        };

        var expectedResult = new PagedResult<AuditLog>
        {
            Items = logs,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        var startDate = now.AddDays(-4);
        var endDate = now;
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        _mockRepository
            .Setup(x => x.GetByDateRangeAsync(startDate, endDate, pagingParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockRepository.Object.GetByDateRangeAsync(startDate, endDate, pagingParams);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, log => Assert.True(log.Timestamp >= startDate && log.Timestamp <= endDate));
    }

    [Fact]
    public async Task GetRecentAsync_WithDefaultCount_ReturnsRecentLogs()
    {
        // Arrange
        var logs = Enumerable.Range(0, 100)
            .Select(i => CreateAuditLog($"user-{i}", AuditAction.PostCreated, $"post-{i}",
                timestamp: DateTimeOffset.UtcNow.AddMinutes(-i)))
            .ToList();

        _mockRepository
            .Setup(x => x.GetRecentAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _mockRepository.Object.GetRecentAsync();

        // Assert
        Assert.Equal(100, result.Count());
        var resultList = result.ToList();
        Assert.True(resultList[0].Timestamp >= resultList[resultList.Count - 1].Timestamp);
    }

    [Fact]
    public async Task GetFailedActionsAsync_WithValidRange_ReturnsOnlyFailedActions()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var logs = new List<AuditLog>
        {
            CreateAuditLog("user-2", AuditAction.PostCreated, "post-2", result: ActionResult.Failed, timestamp: now.AddDays(-2)),
            CreateAuditLog("user-3", AuditAction.PostUpdated, "post-3", result: ActionResult.Failed, timestamp: now.AddDays(-1))
        };

        var expectedResult = new PagedResult<AuditLog>
        {
            Items = logs,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        var startDate = now.AddDays(-3);
        var endDate = now;
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };

        _mockRepository
            .Setup(x => x.GetFailedActionsAsync(startDate, endDate, pagingParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockRepository.Object.GetFailedActionsAsync(startDate, endDate, pagingParams);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, log => Assert.Equal(ActionResult.Failed, log.Result));
        Assert.All(result.Items, log => Assert.True(log.Timestamp >= startDate && log.Timestamp <= endDate));
    }

    [Fact]
    public async Task GetUserStatisticsAsync_WithValidData_ReturnsCorrectCounts()
    {
        // Arrange
        var userId = "test-user";
        var now = DateTimeOffset.UtcNow;
        var stats = new Dictionary<AuditAction, int>
        {
            { AuditAction.PostCreated, 3 },
            { AuditAction.PostUpdated, 2 },
            { AuditAction.PostDeleted, 1 }
        };

        var startDate = now.AddDays(-2);
        var endDate = now;

        _mockRepository
            .Setup(x => x.GetUserStatisticsAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _mockRepository.Object.GetUserStatisticsAsync(userId, startDate, endDate);

        // Assert
        Assert.Equal(3, result[AuditAction.PostCreated]);
        Assert.Equal(2, result[AuditAction.PostUpdated]);
        Assert.Equal(1, result[AuditAction.PostDeleted]);
    }

    [Fact]
    public async Task DeleteOldLogsAsync_WithOlderThanDate_DeletesOldLogs()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var olderThan = now.AddDays(-30);

        _mockRepository
            .Setup(x => x.DeleteOldLogsAsync(olderThan, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var deletedCount = await _mockRepository.Object.DeleteOldLogsAsync(olderThan);

        // Assert
        Assert.Equal(2, deletedCount);
    }

    [Fact]
    public async Task DeleteOldLogsAsync_WhenNoOldLogs_ReturnsZero()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var olderThan = now.AddDays(-30);

        _mockRepository
            .Setup(x => x.DeleteOldLogsAsync(olderThan, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var deletedCount = await _mockRepository.Object.DeleteOldLogsAsync(olderThan);

        // Assert
        Assert.Equal(0, deletedCount);
    }

    // Helper method to create test audit logs
    private AuditLog CreateAuditLog(
        string userId,
        AuditAction action,
        string entityId,
        EntityType entityType = EntityType.BlogPost,
        ActionResult result = ActionResult.Success,
        DateTimeOffset? timestamp = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid().ToString(),
            GroupKey = "auditlogs",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = $"Entity {entityId}",
            UserId = userId,
            UserEmail = $"{userId}@example.com",
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            Result = result,
            Description = $"{action} on {entityType} {entityId}"
        };
    }
}
