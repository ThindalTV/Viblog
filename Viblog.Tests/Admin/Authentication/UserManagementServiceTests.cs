using Microsoft.Extensions.Logging;
using Moq;
using Viblog.Admin.Authentication;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Xunit;

namespace Viblog.Tests.Admin.Authentication;

/// <summary>
/// Unit tests for UserManagementService
/// Tests business logic for user management using repository pattern
/// </summary>
public class UserManagementServiceTests
{
    private readonly Mock<IAdminUserRepository> _mockRepository;
    private readonly Mock<IIdentityProviderSyncService> _mockIdentityProviderSyncService;
    private readonly Mock<ILogger<UserManagementService>> _mockLogger;
    private readonly UserManagementService _sut;

    public UserManagementServiceTests()
    {
        _mockRepository = new Mock<IAdminUserRepository>();
        _mockIdentityProviderSyncService = new Mock<IIdentityProviderSyncService>();
        _mockLogger = new Mock<ILogger<UserManagementService>>();

        _sut = new UserManagementService(
            _mockRepository.Object,
            _mockIdentityProviderSyncService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var userId = "user-1";
        var expectedUser = new AdminUser
        {
            Id = userId,
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        _mockRepository
            .Setup(x => x.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var userId = "nonexistent";

        _mockRepository
            .Setup(x => x.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByEmailAsync_CallsRepositoryWithEmail()
    {
        // Arrange
        var email = "test@example.com";
        var user = new AdminUser { Email = email };
        
        _mockRepository
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetUserByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(
            x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateFromExternalLoginAsync_WhenUserExistsByExternalId_UpdatesUser()
    {
        // Arrange
        var externalUserId = "auth0|123";
        var email = "updated@example.com";
        var displayName = "Updated Name";

        var existingUser = new AdminUser
        {
            Id = "user-1",
            Email = "old@example.com",
            DisplayName = "Old Name",
            ExternalUserId = externalUserId
        };

        _mockRepository
            .Setup(x => x.GetByExternalIdAsync(externalUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        /*_mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<AdminUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);*/

        // Act
        var result = await _sut.CreateOrUpdateFromExternalLoginAsync(
            externalUserId, email, displayName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email.ToLowerInvariant(), result.Email);
        Assert.Equal(displayName, result.DisplayName);
        Assert.NotNull(result.ExternalUserLastSync);
        Assert.NotNull(result.LastLoginAt);
    }

    [Fact]
    public async Task CreateOrUpdateFromExternalLoginAsync_WhenUserExistsByEmail_LinksToExternalProvider()
    {
        // Arrange
        var externalUserId = "auth0|new";
        var email = "existing@example.com";
        var displayName = "Existing User";

        var existingUser = new AdminUser
        {
            Id = "user-1",
            Email = email.ToLowerInvariant(),
            DisplayName = displayName,
            ExternalUserId = null
        };

        _mockRepository
            .Setup(x => x.GetByExternalIdAsync(externalUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        _mockRepository
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockRepository
            .Setup(x => x.GetByIdAsync("user-1", "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        /*_mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<AdminUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);*/

        // Act
        var result = await _sut.CreateOrUpdateFromExternalLoginAsync(
            externalUserId, email, displayName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(externalUserId, result.ExternalUserId);
        Assert.NotNull(result.ExternalUserLastSync);
    }

    [Fact]
    public async Task CreateOrUpdateFromExternalLoginAsync_WhenNewUser_CreatesUserWithNoClaims()
    {
        // Arrange
        var externalUserId = "auth0|newuser";
        var email = "newuser@example.com";
        var displayName = "New User";

        _mockRepository
            .Setup(x => x.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        _mockRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<AdminUser>(), It.IsAny<CancellationToken>()))
            .Returns((AdminUser user, CancellationToken ct) => Task.FromResult(user));

        // Act
        var result = await _sut.CreateOrUpdateFromExternalLoginAsync(
            externalUserId, email, displayName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email.ToLowerInvariant(), result.Email);
        Assert.Equal(displayName, result.DisplayName);
        Assert.Equal(externalUserId, result.ExternalUserId);
        Assert.Empty(result.CustomClaims);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task ValidateUserDataAsync_WhenNameIsEmpty_ReturnsInvalid()
    {
        // Arrange
        var name = "";
        var email = "test@example.com";

        // Act
        var result = await _sut.ValidateUserDataAsync(name, email);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Name is required", result.Errors);
    }

    [Fact]
    public async Task ValidateUserDataAsync_WhenNameIsTooShort_ReturnsInvalid()
    {
        // Arrange
        var name = "A";
        var email = "test@example.com";

        // Act
        var result = await _sut.ValidateUserDataAsync(name, email);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Name must be at least 2 characters long", result.Errors);
    }

    [Fact]
    public async Task ValidateUserDataAsync_WhenEmailIsInvalid_ReturnsInvalid()
    {
        // Arrange
        var name = "Test User";
        var email = "invalid-email";

        // Act
        var result = await _sut.ValidateUserDataAsync(name, email);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Email address is not valid", result.Errors);
    }

    [Fact]
    public async Task ValidateUserDataAsync_WhenEmailAlreadyExists_ReturnsInvalid()
    {
        // Arrange
        var name = "Test User";
        var email = "existing@example.com";

        var existingUser = new AdminUser
        {
            Id = "other-user",
            Email = email.ToLowerInvariant()
        };

        _mockRepository
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.ValidateUserDataAsync(name, email);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Email address is already in use", result.Errors);
    }

    [Fact]
    public async Task ValidateUserDataAsync_WhenUpdatingSameUserEmail_ReturnsValid()
    {
        // Arrange
        var name = "Test User";
        var email = "test@example.com";
        var userId = "user-1";

        var existingUser = new AdminUser
        {
            Id = userId,
            Email = email.ToLowerInvariant()
        };

        _mockRepository
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.ValidateUserDataAsync(name, email, userId);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateUserDataAsync_WhenAllDataValid_ReturnsValid()
    {
        // Arrange
        var name = "Test User";
        var email = "test@example.com";

        _mockRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        // Act
        var result = await _sut.ValidateUserDataAsync(name, email);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task AnyUsersExistAsync_WhenUsersExist_ReturnsTrue()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.AnyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.AnyUsersExistAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AnyUsersExistAsync_WhenNoUsersExist_ReturnsFalse()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.AnyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.AnyUsersExistAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenEmailChangedAttempted_ReturnsInvalid()
    {
        // Arrange
        var userId = "user-1";
        var oldEmail = "old@example.com";
        var newEmail = "new@example.com";

        var existingUser = new AdminUser
        {
            Id = userId,
            Email = oldEmail,
            DisplayName = "Test"
        };

        _mockRepository
            .Setup(x => x.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.UpdateUserAsync(
            userId, "Test", newEmail, new List<string>(), true);

        // Assert
        Assert.False(result.ValidationResult.IsValid);
        Assert.Contains("Email cannot be changed", result.ValidationResult.Errors);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsPagedResults()
    {
        // Arrange
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var users = new List<AdminUser>
        {
            new() { Id = "1", Email = "user1@example.com" },
            new() { Id = "2", Email = "user2@example.com" }
        };

        var pagedResult = new PagedResult<AdminUser>(users, 2, 1, 10);

        _mockRepository
            .Setup(x => x.GetAllAsync(pagingParams, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetUsersAsync(pagingParams);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.ToList().Count);
    }
}
