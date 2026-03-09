using Viblog.Admin.Facades;
using Viblog.Infrastructure.Authentication;
using Viblog.Infrastructure.Data.Entities;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for UserProfileFacade
/// </summary>
public class UserProfileFacadeTests
{
    private readonly Mock<IUserManagementService> _mockUserManagementService;
    private readonly UserProfileFacade _facade;

    public UserProfileFacadeTests()
    {
        _mockUserManagementService = new Mock<IUserManagementService>();
        _facade = new UserProfileFacade(_mockUserManagementService.Object);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsUser()
    {
        // Arrange
        var userId = "user-1";
        var expectedUser = new AdminUser { Id = userId, Email = "test@example.com", DisplayName = "Test" };

        _mockUserManagementService
            .Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _facade.GetCurrentUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task UpdateProfileAsync_PreservesClaimsAndActiveStatus()
    {
        // Arrange
        var userId = "user-1";
        var newName = "Updated Name";
        var newEmail = "updated@example.com";
        var existingUser = new AdminUser
        {
            Id = userId,
            Email = "old@example.com",
            DisplayName = "Old Name",
            CustomClaims = [UserClaims.PostWrite, UserClaims.PageWrite],
            IsActive = true
        };
        var updatedUser = new AdminUser
        {
            Id = userId,
            Email = newEmail,
            DisplayName = newName,
            CustomClaims = existingUser.CustomClaims,
            IsActive = existingUser.IsActive
        };

        _mockUserManagementService
            .Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserManagementService
            .Setup(s => s.UpdateUserAsync(
                userId,
                newName,
                newEmail,
                It.Is<IEnumerable<string>>(c => c.SequenceEqual(existingUser.CustomClaims)),
                existingUser.IsActive,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((updatedUser, UserValidationResult.Valid()));

        // Act
        var (user, validationResult) = await _facade.UpdateProfileAsync(userId, newName, newEmail);

        // Assert
        Assert.NotNull(user);
        Assert.True(validationResult.IsValid);
        Assert.Equal(newName, user.DisplayName);
        Assert.Equal(newEmail, user.Email);
        Assert.Equal(2, user.CustomClaims.Count);

        _mockUserManagementService.Verify(
            s => s.UpdateUserAsync(
                userId,
                newName,
                newEmail,
                It.Is<IEnumerable<string>>(c => c.SequenceEqual(existingUser.CustomClaims)),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNonexistentUser_ReturnsError()
    {
        // Arrange
        var userId = "nonexistent";
        var name = "Test";
        var email = "test@example.com";

        _mockUserManagementService
            .Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        // Act
        var (user, validationResult) = await _facade.UpdateProfileAsync(userId, name, email);

        // Assert
        Assert.Null(user);
        Assert.False(validationResult.IsValid);
        Assert.Contains(validationResult.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task ChangePasswordAsync_DelegatesToResetPasswordAsync()
    {
        // Arrange
        var userId = "user-1";
        var expectedResult = UserValidationResult.Valid();

        _mockUserManagementService
            .Setup(s => s.ResetPasswordAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.ChangePasswordAsync(userId, "current-password", "new-password");

        // Assert
        Assert.True(result.IsValid);
        _mockUserManagementService.Verify(
            s => s.ResetPasswordAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenResetFails_ReturnsInvalidResult()
    {
        // Arrange
        var userId = "user-1";
        var expectedResult = UserValidationResult.Invalid("Auth0 error");

        _mockUserManagementService
            .Setup(s => s.ResetPasswordAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.ChangePasswordAsync(userId, "current-password", "new-password");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Auth0 error"));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNullUserId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _facade.ChangePasswordAsync(null!, "current", "new"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ChangePasswordAsync_WithInvalidUserId_ThrowsArgumentException(string invalidId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _facade.ChangePasswordAsync(invalidId, "current", "new"));
    }
}



