using Microsoft.Extensions.Logging;
using Viblog.Admin.Services.Authentication;
using Viblog.Data.Filesystem.Data.Repositories;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Tests.Integration;

/// <summary>
/// Integration tests for authentication system using filesystem provider
/// </summary>
public class AuthenticationIntegrationTests : IClassFixture<FileSystemTestFixture>
{
    private readonly FileSystemTestFixture _fixture;
    private readonly IUserRepository _userRepository;
    private readonly IAuthenticationProvider _authProvider;
    private readonly IUserManagementService _userManagementService;

    public AuthenticationIntegrationTests(FileSystemTestFixture fixture)
    {
        _fixture = fixture;
        _userRepository = _fixture.UserRepository;
        _authProvider = _fixture.AuthenticationProvider;
        _userManagementService = _fixture.UserManagementService;
    }

    #region User Creation and Authentication Tests

    [Fact]
    public async Task CreateUser_ThenAuthenticate_Success()
    {
        // Arrange
        var name = "Test User";
        var email = $"test-{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";
        var claims = new List<string> { UserClaims.PostWrite, UserClaims.PageWrite };

        // Act - Create user
        var (user, validationResult) = await _userManagementService.CreateUserAsync(
            name, email, password, claims);

        // Assert - User created
        Assert.NotNull(user);
        Assert.True(validationResult.IsValid);
        Assert.Equal(name, user.Name);
        Assert.Equal(email.ToLowerInvariant(), user.Email);
        Assert.True(user.IsActive);
        Assert.Equal(2, user.Claims.Count);

        // Act - Authenticate
        var authResult = await _authProvider.AuthenticateAsync(email, password);

        // Assert - Authentication successful
        Assert.True(authResult.Success);
        Assert.NotNull(authResult.User);
        Assert.Equal(email.ToLowerInvariant(), authResult.User.Email);
        Assert.Null(authResult.ErrorMessage);
    }

    [Fact]
    public async Task Authenticate_WithInvalidPassword_Fails()
    {
        // Arrange
        var name = "Test User";
        var email = $"test-{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";
        var wrongPassword = "WrongPassword456!";

        await _userManagementService.CreateUserAsync(name, email, password, new List<string>());

        // Act
        var authResult = await _authProvider.AuthenticateAsync(email, wrongPassword);

        // Assert
        Assert.False(authResult.Success);
        Assert.Null(authResult.User);
        Assert.Equal("Invalid email or password.", authResult.ErrorMessage);
    }

    [Fact]
    public async Task Authenticate_WithNonexistentUser_Fails()
    {
        // Arrange
        var email = $"nonexistent-{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        // Act
        var authResult = await _authProvider.AuthenticateAsync(email, password);

        // Assert
        Assert.False(authResult.Success);
        Assert.Null(authResult.User);
        Assert.Equal("Invalid email or password.", authResult.ErrorMessage);
    }

    [Fact]
    public async Task Authenticate_WithInactiveUser_Fails()
    {
        // Arrange
        var name = "Inactive User";
        var email = $"inactive-{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        var (user, _) = await _userManagementService.CreateUserAsync(
            name, email, password, new List<string>());

        // Deactivate user
        await _userManagementService.UpdateUserAsync(
            user!.Id, name, email, user.Claims, isActive: false);

        // Act
        var authResult = await _authProvider.AuthenticateAsync(email, password);

        // Assert
        Assert.False(authResult.Success);
        Assert.Null(authResult.User);
        Assert.Equal("User account is inactive.", authResult.ErrorMessage);
    }

    #endregion

    #region Password Management Tests

    [Fact]
    public async Task AdminResetPassword_ThenAuthenticateWithNewPassword_Success()
    {
        // Arrange
        var name = "Test User";
        var email = $"test-{Guid.NewGuid()}@example.com";
        var originalPassword = "OriginalPass123!";
        var newPassword = "NewPassword456!";

        var (user, _) = await _userManagementService.CreateUserAsync(
            name, email, originalPassword, new List<string>());

        // Act - Admin resets password
        var resetResult = await _userManagementService.ResetPasswordAsync(user!.Id, newPassword);

        // Assert - Reset successful
        Assert.True(resetResult.IsValid);

        // Act - Try old password
        var oldPasswordAuth = await _authProvider.AuthenticateAsync(email, originalPassword);

        // Assert - Old password fails
        Assert.False(oldPasswordAuth.Success);

        // Act - Try new password
        var newPasswordAuth = await _authProvider.AuthenticateAsync(email, newPassword);

        // Assert - New password works
        Assert.True(newPasswordAuth.Success);
        Assert.NotNull(newPasswordAuth.User);
    }

    [Fact]
    public async Task UserChangePassword_WithCorrectCurrentPassword_Success()
    {
        // Arrange
        var name = "Test User";
        var email = $"test-{Guid.NewGuid()}@example.com";
        var currentPassword = "CurrentPass123!";
        var newPassword = "NewPassword456!";

        var (user, _) = await _userManagementService.CreateUserAsync(
            name, email, currentPassword, new List<string>());

        // Act - User changes password
        var changeResult = await _authProvider.ChangePasswordAsync(
            user!.Id, currentPassword, newPassword);

        // Assert - Change successful
        Assert.True(changeResult.Success);

        // Act - Authenticate with new password
        var authResult = await _authProvider.AuthenticateAsync(email, newPassword);

        // Assert - New password works
        Assert.True(authResult.Success);
        Assert.NotNull(authResult.User);
    }

    [Fact]
    public async Task UserChangePassword_WithIncorrectCurrentPassword_Fails()
    {
        // Arrange
        var name = "Test User";
        var email = $"test-{Guid.NewGuid()}@example.com";
        var currentPassword = "CurrentPass123!";
        var wrongCurrentPassword = "WrongPassword123!";
        var newPassword = "NewPassword456!";

        var (user, _) = await _userManagementService.CreateUserAsync(
            name, email, currentPassword, new List<string>());

        // Act - Try to change password with wrong current password
        var changeResult = await _authProvider.ChangePasswordAsync(
            user!.Id, wrongCurrentPassword, newPassword);

        // Assert - Change fails
        Assert.False(changeResult.Success);
        Assert.Equal("Current password is incorrect.", changeResult.ErrorMessage);

        // Verify original password still works
        var authResult = await _authProvider.AuthenticateAsync(email, currentPassword);
        Assert.True(authResult.Success);
    }

    [Fact]
    public async Task ChangePassword_WithWeakPassword_Fails()
    {
        // Arrange
        var name = "Test User";
        var email = $"test-{Guid.NewGuid()}@example.com";
        var currentPassword = "CurrentPass123!";
        var weakPassword = "weak";

        var (user, _) = await _userManagementService.CreateUserAsync(
            name, email, currentPassword, new List<string>());

        // Act
        var changeResult = await _authProvider.ChangePasswordAsync(
            user!.Id, currentPassword, weakPassword);

        // Assert
        Assert.False(changeResult.Success);
        Assert.Contains("at least 8 characters", changeResult.ErrorMessage);
    }

    #endregion

    #region Profile Management Tests

    [Fact]
    public async Task UpdateUserProfile_NameAndEmail_Success()
    {
        // Arrange
        var originalName = "Original Name";
        var originalEmail = $"original-{Guid.NewGuid()}@example.com";
        var newName = "Updated Name";
        var newEmail = $"updated-{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";
        var claims = new List<string> { UserClaims.PostWrite };

        var (user, _) = await _userManagementService.CreateUserAsync(
            originalName, originalEmail, password, claims);

        // Act - Update profile
        var (updatedUser, validationResult) = await _userManagementService.UpdateUserAsync(
            user!.Id, newName, newEmail, claims, isActive: true);

        // Assert - Update successful
        Assert.True(validationResult.IsValid);
        Assert.NotNull(updatedUser);
        Assert.Equal(newName, updatedUser.Name);
        Assert.Equal(newEmail.ToLowerInvariant(), updatedUser.Email);
        Assert.Single(updatedUser.Claims);

        // Verify old email no longer works
        var oldEmailAuth = await _authProvider.AuthenticateAsync(originalEmail, password);
        Assert.False(oldEmailAuth.Success);

        // Verify new email works
        var newEmailAuth = await _authProvider.AuthenticateAsync(newEmail, password);
        Assert.True(newEmailAuth.Success);
    }

    [Fact]
    public async Task UpdateUser_WithDuplicateEmail_Fails()
    {
        // Arrange
        var user1Email = $"user1-{Guid.NewGuid()}@example.com";
        var user2Email = $"user2-{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        var (user1, _) = await _userManagementService.CreateUserAsync(
            "User 1", user1Email, password, new List<string>());

        var (user2, _) = await _userManagementService.CreateUserAsync(
            "User 2", user2Email, password, new List<string>());

        // Act - Try to update user2 with user1's email
        var (updatedUser, validationResult) = await _userManagementService.UpdateUserAsync(
            user2!.Id, "User 2", user1Email, user2.Claims, isActive: true);

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Null(updatedUser);
        Assert.Contains(validationResult.Errors, e => e.Contains("Email address is already in use"));
    }

    #endregion

    #region User Management Tests

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_Fails()
    {
        // Arrange
        var email = $"duplicate-{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        await _userManagementService.CreateUserAsync(
            "User 1", email, password, new List<string>());

        // Act - Try to create another user with same email
        var (user, validationResult) = await _userManagementService.CreateUserAsync(
            "User 2", email, password, new List<string>());

        // Assert
        Assert.Null(user);
        Assert.False(validationResult.IsValid);
        Assert.Contains(validationResult.Errors, e => e.Contains("Email address is already in use"));
    }

    [Fact]
    public async Task DeleteUser_ThenAuthenticationFails()
    {
        // Arrange
        var name = "Test User";
        var email = $"test-{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        var (user, _) = await _userManagementService.CreateUserAsync(
            name, email, password, new List<string>());

        // Verify user can authenticate
        var authBefore = await _authProvider.AuthenticateAsync(email, password);
        Assert.True(authBefore.Success);

        // Act - Delete user
        var deleteResult = await _userManagementService.DeleteUserAsync(user!.Id);

        // Assert - Delete successful
        Assert.True(deleteResult);

        // Verify user can no longer authenticate
        var authAfter = await _authProvider.AuthenticateAsync(email, password);
        Assert.False(authAfter.Success);
    }

    [Fact]
    public async Task GetUsers_WithPaging_ReturnsCorrectPage()
    {
        // Arrange - Create multiple users
        var userCount = 5;
        for (int i = 0; i < userCount; i++)
        {
            await _userManagementService.CreateUserAsync(
                $"User {i}",
                $"user{i}-{Guid.NewGuid()}@example.com",
                "SecurePass123!",
                new List<string>());
        }

        // Act - Get first page
        var page1 = await _userManagementService.GetUsersAsync(
            new PagingParameters { PageNumber = 1, PageSize = 3 },
            includeInactive: false);

        // Assert
        Assert.Equal(3, page1.Items.Count());
        Assert.True(page1.TotalCount >= userCount);
        Assert.Equal(1, page1.PageNumber);
    }

    #endregion

    #region Default Admin Tests

    [Fact]
    public async Task CreateDefaultAdmin_ThenAuthenticate_Success()
    {
        // Act - Create default admin
        var admin = await _userManagementService.CreateDefaultAdminUserAsync();

        // Assert - Admin created with correct properties
        Assert.NotNull(admin);
        Assert.Equal("Administrator", admin.Name);
        Assert.Equal("admin@viblog.local", admin.Email);
        Assert.True(admin.IsActive);
        Assert.Equal(UserClaims.AllClaims.Count, admin.Claims.Count);

        // Verify all claims are present
        foreach (var claim in UserClaims.AllClaims)
        {
            Assert.Contains(claim, admin.Claims);
        }

        // Act - Authenticate as admin
        var authResult = await _authProvider.AuthenticateAsync(
            "admin@viblog.local", "admin123!");

        // Assert - Authentication successful
        Assert.True(authResult.Success);
        Assert.NotNull(authResult.User);
        Assert.Equal("Administrator", authResult.User.Name);
    }

    #endregion

    #region Multi-User Scenarios

    [Fact]
    public async Task MultipleUsers_WithDifferentClaims_AuthenticateIndependently()
    {
        // Arrange - Create users with different permissions
        var postEditor = await _userManagementService.CreateUserAsync(
            "Post Editor",
            $"editor-{Guid.NewGuid()}@example.com",
            "EditorPass123!",
            new List<string> { UserClaims.PostWrite });

        var pageEditor = await _userManagementService.CreateUserAsync(
            "Page Editor",
            $"page-{Guid.NewGuid()}@example.com",
            "PagePass123!",
            new List<string> { UserClaims.PageWrite });

        var viewer = await _userManagementService.CreateUserAsync(
            "Viewer",
            $"viewer-{Guid.NewGuid()}@example.com",
            "ViewerPass123!",
            new List<string> { UserClaims.StatisticsRead });

        // Assert - All users created successfully
        Assert.True(postEditor.ValidationResult.IsValid);
        Assert.True(pageEditor.ValidationResult.IsValid);
        Assert.True(viewer.ValidationResult.IsValid);

        // Verify each user has correct claims
        Assert.Single(postEditor.User!.Claims);
        Assert.Contains(UserClaims.PostWrite, postEditor.User.Claims);

        Assert.Single(pageEditor.User!.Claims);
        Assert.Contains(UserClaims.PageWrite, pageEditor.User.Claims);

        Assert.Single(viewer.User!.Claims);
        Assert.Contains(UserClaims.StatisticsRead, viewer.User.Claims);

        // Verify all can authenticate independently
        var auth1 = await _authProvider.AuthenticateAsync(postEditor.User.Email, "EditorPass123!");
        var auth2 = await _authProvider.AuthenticateAsync(pageEditor.User.Email, "PagePass123!");
        var auth3 = await _authProvider.AuthenticateAsync(viewer.User.Email, "ViewerPass123!");

        Assert.True(auth1.Success);
        Assert.True(auth2.Success);
        Assert.True(auth3.Success);
    }

    #endregion

    #region Last Login Tracking

    [Fact]
    public async Task Authenticate_UpdatesLastLoginTimestamp()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        var (user, _) = await _userManagementService.CreateUserAsync(
            "Test User", email, password, new List<string>());

        var originalLastLogin = user!.LastLoginAt;

        // Wait a bit to ensure timestamp difference
        await Task.Delay(100);

        // Act - Authenticate
        var authResult = await _authProvider.AuthenticateAsync(email, password);

        // Assert
        Assert.True(authResult.Success);

        // Get updated user
        var updatedUser = await _userManagementService.GetUserByEmailAsync(email);

        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.LastLoginAt);
        
        if (originalLastLogin.HasValue)
        {
            Assert.True(updatedUser.LastLoginAt > originalLastLogin);
        }
    }

    #endregion
}
