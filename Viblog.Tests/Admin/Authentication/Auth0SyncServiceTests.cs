using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Viblog.Admin.Authentication;
using Viblog.Admin.Configuration;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Infrastructure.Data.Repositories;
using Xunit;

namespace Viblog.Tests.Admin.Authentication;

/// <summary>
/// Unit tests for Auth0SyncService
/// Tests Auth0 integration logic and user synchronization
/// </summary>
public class Auth0SyncServiceTests
{
    private readonly Mock<IOptions<Auth0Settings>> _mockAuth0Settings;
    private readonly Mock<IAdminUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<Auth0SyncService>> _mockLogger;
    private readonly Auth0SyncService _sut;

    public Auth0SyncServiceTests()
    {
        _mockAuth0Settings = new Mock<IOptions<Auth0Settings>>();
        _mockUserRepository = new Mock<IAdminUserRepository>();
        _mockLogger = new Mock<ILogger<Auth0SyncService>>();

        var auth0Settings = new Auth0Settings
        {
            Domain = "test.auth0.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            Audience = "https://test.auth0.com/api/v2/"
        };

        _mockAuth0Settings.Setup(x => x.Value).Returns(auth0Settings);

        // Setup SaveChangesAsync on the base IRepository interface
        _mockUserRepository
            .As<IRepository<AdminUser>>()
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new Auth0SyncService(
            _mockAuth0Settings.Object,
            _mockUserRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SyncUserAsync_WhenUserExists_UpdatesExistingUser()
    {
        // Arrange
        var externalUserId = "auth0|123456";
        var email = "test@example.com";
        var name = "Test User";

        var existingUser = new AdminUser
        {
            Id = "user-1",
            Email = email,
            DisplayName = "Old Name",
            ExternalUserId = externalUserId
        };

        _mockUserRepository
            .Setup(x => x.GetByExternalIdAsync(externalUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.SyncUserAsync(externalUserId, email, name);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user-1", result.Id);
        Assert.Equal(email, result.Email);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<AdminUser>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.As<IRepository<AdminUser>>().Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncUserAsync_WhenUserDoesNotExist_CreatesNewUser()
    {
        // Arrange
        var externalUserId = "auth0|new-user";
        var email = "newuser@example.com";
        var name = "New User";

        _mockUserRepository
            .Setup(x => x.GetByExternalIdAsync(externalUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        _mockUserRepository
            .Setup(x => x.AnyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Other users exist

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<AdminUser>(), It.IsAny<CancellationToken>()))
            .Returns((AdminUser user, CancellationToken ct) => Task.FromResult(user));

        // Act
        var result = await _sut.SyncUserAsync(externalUserId, email, name);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Empty(result.CustomClaims); // Should have no permissions when not first user
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<AdminUser>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.As<IRepository<AdminUser>>().Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncUserAsync_WhenFirstUser_CreatesUserWithFullPermissions()
    {
        // Arrange
        var externalUserId = "auth0|first-user";
        var email = "firstuser@example.com";
        var name = "First User";

        _mockUserRepository
            .Setup(x => x.GetByExternalIdAsync(externalUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        _mockUserRepository
            .Setup(x => x.AnyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // No users exist - this is the first!

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<AdminUser>(), It.IsAny<CancellationToken>()))
            .Returns((AdminUser user, CancellationToken ct) => Task.FromResult(user));

        // Act
        var result = await _sut.SyncUserAsync(externalUserId, email, name);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.NotEmpty(result.CustomClaims); // Should have permissions as first user
        Assert.Contains(UserClaims.PostWrite, result.CustomClaims);
        Assert.Contains(UserClaims.PageWrite, result.CustomClaims);
        Assert.Contains(UserClaims.UserRead, result.CustomClaims);
        Assert.Contains(UserClaims.UserWrite, result.CustomClaims);
        Assert.Contains(UserClaims.StatisticsRead, result.CustomClaims);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<AdminUser>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.As<IRepository<AdminUser>>().Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncUserAsync_WhenSyncFails_ReturnsNull()
    {
        // Arrange
        var externalUserId = "auth0|fail";
        var email = "fail@example.com";
        var name = "Fail User";

        _mockUserRepository
            .Setup(x => x.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.SyncUserAsync(externalUserId, email, name);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SyncUserAsync_LogsInformation_WhenUserSyncedSuccessfully()
    {
        // Arrange
        var externalUserId = "auth0|123";
        var email = "test@example.com";
        var name = "Test";

        var user = new AdminUser { Id = "user-1", Email = email };

        _mockUserRepository
            .Setup(x => x.GetByExternalIdAsync(externalUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _sut.SyncUserAsync(externalUserId, email, name);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Syncing user from Auth0")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
