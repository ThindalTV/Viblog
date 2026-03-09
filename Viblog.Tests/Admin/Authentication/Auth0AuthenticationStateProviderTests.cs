using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Moq;
using Viblog.Admin.Authentication;
using Viblog.Infrastructure.Authentication;
using Viblog.Infrastructure.Data.Entities;
using Xunit;

namespace Viblog.Tests.Admin.Authentication;

/// <summary>
/// Unit tests for Auth0AuthenticationStateProvider
/// Tests claims transformation (ValidateAuthenticationStateAsync is protected and tested indirectly)
/// </summary>
public class Auth0AuthenticationStateProviderTests
{
    private readonly Mock<IIdentityProviderSyncService> _mockSyncService;
    private readonly Mock<IUserManagementService> _mockUserManagementService;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<Auth0AuthenticationStateProvider>> _mockLogger;
    private readonly Auth0AuthenticationStateProvider _sut;

    public Auth0AuthenticationStateProviderTests()
    {
        _mockSyncService = new Mock<IIdentityProviderSyncService>();
        _mockUserManagementService = new Mock<IUserManagementService>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<Auth0AuthenticationStateProvider>>();

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _sut = new Auth0AuthenticationStateProvider(
            _mockSyncService.Object,
            _mockUserManagementService.Object,
            _mockLoggerFactory.Object);
    }

    [Fact]
    public async Task TransformAuth0ClaimsAsync_WhenNotAuthenticated_ReturnsEmptyPrincipal()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal();

        // Act
        var result = await _sut.TransformAuth0ClaimsAsync(claimsPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Identity);
    }

    [Fact]
    public async Task TransformAuth0ClaimsAsync_WhenMissingRequiredClaims_ReturnsOriginalPrincipal()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Test") }, "Auth0");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Act
        var result = await _sut.TransformAuth0ClaimsAsync(claimsPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Identity);
        Assert.True(result.Identity.IsAuthenticated);
        Assert.Equal(claimsPrincipal, result); // Should return original principal
    }

    [Fact]
    public async Task TransformAuth0ClaimsAsync_WhenSyncFails_ReturnsOriginalPrincipal()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim(ClaimTypes.Email, "test@example.com")
        }, "Auth0");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _mockSyncService
            .Setup(x => x.SyncUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        // Act
        var result = await _sut.TransformAuth0ClaimsAsync(claimsPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Identity);
        Assert.True(result.Identity.IsAuthenticated);
        Assert.Equal(claimsPrincipal, result); // Should return original principal
    }

    [Fact]
    public async Task TransformAuth0ClaimsAsync_WhenSuccessful_ReturnsTransformedPrincipal()
    {
        // Arrange
        var externalUserId = "auth0|123";
        var email = "test@example.com";
        var name = "Test User";

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, externalUserId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name)
        }, "Auth0");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var localUser = new AdminUser
        {
            Id = "local-user-1",
            Email = email,
            DisplayName = name,
            ExternalUserId = externalUserId,
            CustomClaims = new List<string> { "posts:write", "pages:write" }
        };

        _mockSyncService
            .Setup(x => x.SyncUserAsync(externalUserId, email, name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localUser);

        // Act
        var result = await _sut.TransformAuth0ClaimsAsync(claimsPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Identity?.IsAuthenticated);
        Assert.Equal("local-user-1", result.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(email, result.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal(name, result.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("Admin", result.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Equal(externalUserId, result.FindFirst("external_user_id")?.Value);
    }

    [Fact]
    public async Task TransformAuth0ClaimsAsync_AddsCustomPermissionClaims()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", "auth0|123"),
            new Claim("email", "test@example.com"),
            new Claim("name", "Test")
        }, "Auth0");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var localUser = new AdminUser
        {
            Id = "user-1",
            Email = "test@example.com",
            DisplayName = "Test",
            CustomClaims = new List<string> { "posts:write", "users:read", "stats:read" }
        };

        _mockSyncService
            .Setup(x => x.SyncUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(localUser);

        // Act
        var result = await _sut.TransformAuth0ClaimsAsync(claimsPrincipal);

        // Assert
        var permissionClaims = result.FindAll("permission").ToList();
        Assert.Equal(3, permissionClaims.Count);
        Assert.Contains(permissionClaims, c => c.Value == "posts:write");
        Assert.Contains(permissionClaims, c => c.Value == "users:read");
        Assert.Contains(permissionClaims, c => c.Value == "stats:read");
    }

    [Fact]
    public async Task TransformAuth0ClaimsAsync_HandlesMissingNameClaim()
    {
        // Arrange
        var email = "test@example.com";
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", "auth0|123"),
            new Claim("email", email)
        }, "Auth0");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var localUser = new AdminUser
        {
            Id = "user-1",
            Email = email,
            DisplayName = email,
            CustomClaims = []
        };

        _mockSyncService
            .Setup(x => x.SyncUserAsync(It.IsAny<string>(), email, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localUser);

        // Act
        var result = await _sut.TransformAuth0ClaimsAsync(claimsPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Identity?.IsAuthenticated);
        Assert.Equal(email, result.FindFirst(ClaimTypes.Name)?.Value);
    }
}
