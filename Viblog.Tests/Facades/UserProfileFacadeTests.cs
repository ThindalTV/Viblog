using Viblog.Admin.Facades;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for UserProfileFacade
/// </summary>
public class UserProfileFacadeTests
{
    private readonly Mock<IUserManagementService> _mockUserManagementService;
    private readonly Mock<IAuthenticationProvider> _mockAuthProvider;
    private readonly UserProfileFacade _facade;

    public UserProfileFacadeTests()
    {
        _mockUserManagementService = new Mock<IUserManagementService>();
        _mockAuthProvider = new Mock<IAuthenticationProvider>();
        _facade = new UserProfileFacade(_mockUserManagementService.Object, _mockAuthProvider.Object);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsUser()
    {
        // Arrange
        var userId = "user-1";
        var expectedUser = new AdminUser { Id = userId, Email = "test@example.com" };

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
    public async Task ChangePasswordAsync_CallsAuthProvider()
    {
        // Arrange
        var userId = "user-1";
        var currentPassword = "OldPass123!";
        var newPassword = "NewPass456!";

        _mockAuthProvider
            .Setup(p => p.ChangePasswordAsync(userId, currentPassword, newPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordChangeResult.Successful());

        // Act
        var result = await _facade.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);

        _mockAuthProvider.Verify(
            p => p.ChangePasswordAsync(userId, currentPassword, newPassword, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ReturnsError()
    {
        // Arrange
        var userId = "user-1";
        var currentPassword = "WrongPass123!";
        var newPassword = "NewPass456!";

        _mockAuthProvider
            .Setup(p => p.ChangePasswordAsync(userId, currentPassword, newPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordChangeResult.Failed("Current password is incorrect."));

        // Act
        var result = await _facade.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Current password is incorrect.", result.ErrorMessage);
    }
}



