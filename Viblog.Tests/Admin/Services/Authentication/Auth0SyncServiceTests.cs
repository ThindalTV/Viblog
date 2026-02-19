using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Viblog.Admin.Configuration;
using Viblog.Admin.Services.Authentication;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;
using Xunit;

namespace Viblog.Tests.Admin.Services.Authentication;

/// <summary>
/// Unit tests for Auth0SyncService
/// Tests Auth0 integration logic and user synchronization
/// </summary>
public class Auth0SyncServiceTests
{
    private readonly Mock<IOptions<Auth0Settings>> _mockAuth0Settings;
    private readonly Mock<IUserManagementService> _mockUserManagementService;
    private readonly Mock<ILogger<Auth0SyncService>> _mockLogger;
    private readonly Auth0SyncService _sut;

    public Auth0SyncServiceTests()
    {
        _mockAuth0Settings = new Mock<IOptions<Auth0Settings>>();
        _mockUserManagementService = new Mock<IUserManagementService>();
        _mockLogger = new Mock<ILogger<Auth0SyncService>>();

        var auth0Settings = new Auth0Settings
        {
            Domain = "test.auth0.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            Audience = "https://test.auth0.com/api/v2/"
        };

        _mockAuth0Settings.Setup(x => x.Value).Returns(auth0Settings);

        _sut = new Auth0SyncService(
            _mockAuth0Settings.Object,
            _mockUserManagementService.Object,
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

        _mockUserManagementService
            .Setup(x => x.CreateOrUpdateFromExternalLoginAsync(
                externalUserId, email, name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.SyncUserAsync(externalUserId, email, name);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user-1", result.Id);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task SyncUserAsync_WhenUserDoesNotExist_CreatesNewUser()
    {
        // Arrange
        var externalUserId = "auth0|new-user";
        var email = "newuser@example.com";
        var name = "New User";

        var newUser = new AdminUser
        {
            Id = "user-new",
            Email = email,
            DisplayName = name,
            ExternalUserId = externalUserId,
            CustomClaims = []
        };

        _mockUserManagementService
            .Setup(x => x.CreateOrUpdateFromExternalLoginAsync(
                externalUserId, email, name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);

        // Act
        var result = await _sut.SyncUserAsync(externalUserId, email, name);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user-new", result.Id);
        Assert.Equal(email, result.Email);
        Assert.Empty(result.CustomClaims);
    }

    [Fact]
    public async Task SyncUserAsync_WhenSyncFails_ReturnsNull()
    {
        // Arrange
        var externalUserId = "auth0|fail";
        var email = "fail@example.com";
        var name = "Fail User";

        _mockUserManagementService
            .Setup(x => x.CreateOrUpdateFromExternalLoginAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

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
        _mockUserManagementService
            .Setup(x => x.CreateOrUpdateFromExternalLoginAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
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
