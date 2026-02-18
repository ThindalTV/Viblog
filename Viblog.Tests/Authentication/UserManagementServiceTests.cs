using Microsoft.Extensions.Logging;
using Viblog.Admin.Services.Authentication;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Tests.Authentication;

/// <summary>
/// Unit tests for UserManagementService
/// </summary>
public class UserManagementServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IAuthenticationProvider> _mockAuthProvider;
    private readonly Mock<ILogger<UserManagementService>> _mockLogger;
    private readonly UserManagementService _service;

    public UserManagementServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockAuthProvider = new Mock<IAuthenticationProvider>();
        _mockLogger = new Mock<ILogger<UserManagementService>>();
        _service = new UserManagementService(
            _mockUserRepository.Object,
            _mockAuthProvider.Object,
            _mockLogger.Object);
    }

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_WithActiveUsersOnly_ReturnsOnlyActiveUsers()
    {
        // Arrange
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<User>
        {
            Items = [new User { Email = "user1@test.com", IsActive = true }],
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockUserRepository
            .Setup(r => r.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                pagingParams,
                It.IsAny<System.Linq.Expressions.Expression<Func<User, string>>>(),
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetUsersAsync(pagingParams, includeInactive: false);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.True(result.Items.First().IsActive);
    }

    [Fact]
    public async Task GetUsersAsync_WithIncludeInactive_ReturnsAllUsers()
    {
        // Arrange
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<User>
        {
            Items =
            [
                new User { Email = "active@test.com", IsActive = true },
                new User { Email = "inactive@test.com", IsActive = false }
            ],
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mockUserRepository
            .Setup(r => r.GetAllAsync(
                pagingParams,
                It.IsAny<System.Linq.Expressions.Expression<Func<User, string>>>(),
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetUsersAsync(pagingParams, includeInactive: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var userId = "user-1";
        var expectedUser = new User { Id = userId, Email = "test@example.com" };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var userId = "nonexistent";

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetUserByEmailAsync Tests

    [Fact]
    public async Task GetUserByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange
        var email = "test@example.com";
        var expectedUser = new User { Email = email };

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _service.GetUserByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WithValidData_CreatesUser()
    {
        // Arrange
        var name = "Test User";
        var email = "test@example.com";
        var password = "ValidPass123!";
        var claims = new List<string> { UserClaims.PostWrite };

        _mockUserRepository
            .Setup(r => r.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockAuthProvider
            .Setup(p => p.ValidatePassword(password))
            .Returns(UserValidationResult.Valid());

        _mockAuthProvider
            .Setup(p => p.HashPassword(password))
            .Returns("hashed-password");

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var (user, validationResult) = await _service.CreateUserAsync(name, email, password, claims);

        // Assert
        Assert.NotNull(user);
        Assert.True(validationResult.IsValid);
        Assert.Equal(name, user.Name);
        Assert.Equal(email.ToLowerInvariant(), user.Email);
        Assert.Contains(UserClaims.PostWrite, user.Claims);

        _mockUserRepository.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WithExistingEmail_ReturnsValidationError()
    {
        // Arrange
        var name = "Test User";
        var email = "existing@example.com";
        var password = "ValidPass123!";
        var claims = new List<string> { UserClaims.PostWrite };

        _mockUserRepository
            .Setup(r => r.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var (user, validationResult) = await _service.CreateUserAsync(name, email, password, claims);

        // Assert
        Assert.Null(user);
        Assert.False(validationResult.IsValid);
        Assert.Contains(validationResult.Errors, e => e.Contains("already in use"));

        _mockUserRepository.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidPassword_ReturnsValidationError()
    {
        // Arrange
        var name = "Test User";
        var email = "test@example.com";
        var password = "weak";
        var claims = new List<string> { UserClaims.PostWrite };

        _mockUserRepository
            .Setup(r => r.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockAuthProvider
            .Setup(p => p.ValidatePassword(password))
            .Returns(UserValidationResult.Invalid("Password must be at least 8 characters long."));

        // Act
        var (user, validationResult) = await _service.CreateUserAsync(name, email, password, claims);

        // Assert
        Assert.Null(user);
        Assert.False(validationResult.IsValid);
        Assert.Contains(validationResult.Errors, e => e.Contains("at least 8 characters"));
    }

    [Theory]
    [InlineData("", "test@example.com")]
    [InlineData("Test User", "")]
    [InlineData(" ", "test@example.com")]
    [InlineData("Test User", " ")]
    public async Task CreateUserAsync_WithEmptyNameOrEmail_ReturnsValidationError(string name, string email)
    {
        // Arrange
        var password = "ValidPass123!";
        var claims = new List<string> { UserClaims.PostWrite };

        // Act
        var (user, validationResult) = await _service.CreateUserAsync(name, email, password, claims);

        // Assert
        Assert.Null(user);
        Assert.False(validationResult.IsValid);
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_WithValidData_UpdatesUser()
    {
        // Arrange
        var userId = "user-1";
        var name = "Updated Name";
        var email = "existing@example.com"; // Use same email - email cannot be changed
        var claims = new List<string> { UserClaims.PostWrite, UserClaims.PageWrite };
        var isActive = true;

        var existingUser = new User
        {
            Id = userId,
            GroupKey = "users",
            Email = "existing@example.com", // Same as update email
            Name = "Old Name",
            PasswordHash = "hashed",
            Claims = [UserClaims.PostWrite],
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserRepository
            .Setup(r => r.EmailExistsAsync(email, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var (user, validationResult) = await _service.UpdateUserAsync(userId, name, email, claims, isActive);

        // Assert
        Assert.NotNull(user);
        Assert.True(validationResult.IsValid);
        Assert.Equal(name, user.Name);
        Assert.Equal(email.ToLowerInvariant(), user.Email);
        Assert.Equal(2, user.Claims.Count);

        _mockUserRepository.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WithNonexistentUser_ReturnsValidationError()
    {
        // Arrange
        var userId = "nonexistent";
        var name = "Test";
        var email = "test@example.com";
        var claims = new List<string>();

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var (user, validationResult) = await _service.UpdateUserAsync(userId, name, email, claims, true);

        // Assert
        Assert.Null(user);
        Assert.False(validationResult.IsValid);
        Assert.Contains(validationResult.Errors, e => e.Contains("not found"));
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WithValidId_DeletesUser()
    {
        // Arrange
        var userId = "user-1";

        _mockUserRepository
            .Setup(r => r.DeleteAsync(userId, "users", true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteUserAsync(userId);

        // Assert
        Assert.True(result);

        _mockUserRepository.Verify(
            r => r.DeleteAsync(userId, "users", true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ValidateUserDataAsync Tests

    [Fact]
    public async Task ValidateUserDataAsync_WithValidData_ReturnsValid()
    {
        // Arrange
        var name = "Test User";
        var email = "test@example.com";

        _mockUserRepository
            .Setup(r => r.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ValidateUserDataAsync(name, email);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateUserDataAsync_WithShortName_ReturnsInvalid()
    {
        // Arrange
        var name = "A";
        var email = "test@example.com";

        // Act
        var result = await _service.ValidateUserDataAsync(name, email);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("at least 2 characters"));
    }

    [Fact]
    public async Task ValidateUserDataAsync_WithInvalidEmail_ReturnsInvalid()
    {
        // Arrange
        var name = "Test User";
        var email = "not-an-email";

        // Act
        var result = await _service.ValidateUserDataAsync(name, email);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not valid"));
    }

    #endregion

    #region AnyUsersExistAsync Tests

    [Fact]
    public async Task AnyUsersExistAsync_WhenUsersExist_ReturnsTrue()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AnyUsersExistAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AnyUsersExistAsync_WhenNoUsersExist_ReturnsFalse()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.AnyUsersExistAsync();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CreateDefaultAdminUserAsync Tests

    [Fact]
    public async Task CreateDefaultAdminUserAsync_CreatesAdminWithAllClaims()
    {
        // Arrange
        _mockAuthProvider
            .Setup(p => p.HashPassword("admin123!"))
            .Returns("hashed-admin-password");

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateDefaultAdminUserAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Administrator", result.Name);
        Assert.Equal("admin@viblog.local", result.Email);
        Assert.Equal(UserClaims.AllClaims.Count, result.Claims.Count);
        Assert.True(result.IsActive);

        _mockUserRepository.Verify(
            r => r.AddAsync(It.Is<User>(u => 
                u.Email == "admin@viblog.local" && 
                u.Claims.Count == UserClaims.AllClaims.Count), 
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidPassword_UpdatesPasswordHash()
    {
        // Arrange
        var userId = "user-1";
        var newPassword = "NewSecure123!";
        var newPasswordHash = "new-hashed-password";

        var existingUser = new User
        {
            Id = userId,
            GroupKey = "users",
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "old-hashed-password",
            Claims = [],
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockAuthProvider
            .Setup(p => p.ValidatePassword(newPassword))
            .Returns(UserValidationResult.Valid());

        _mockAuthProvider
            .Setup(p => p.HashPassword(newPassword))
            .Returns(newPasswordHash);

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ResetPasswordAsync(userId, newPassword);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(newPasswordHash, existingUser.PasswordHash);

        _mockUserRepository.Verify(
            r => r.UpdateAsync(It.Is<User>(u => u.PasswordHash == newPasswordHash), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidPassword_ReturnsValidationError()
    {
        // Arrange
        var userId = "user-1";
        var weakPassword = "weak";

        _mockAuthProvider
            .Setup(p => p.ValidatePassword(weakPassword))
            .Returns(UserValidationResult.Invalid("Password must be at least 8 characters"));

        // Act
        var result = await _service.ResetPasswordAsync(userId, weakPassword);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("at least 8 characters"));

        _mockUserRepository.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithNonexistentUser_ReturnsValidationError()
    {
        // Arrange
        var userId = "nonexistent";
        var newPassword = "NewSecure123!";

        _mockAuthProvider
            .Setup(p => p.ValidatePassword(newPassword))
            .Returns(UserValidationResult.Valid());

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.ResetPasswordAsync(userId, newPassword);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));

        _mockUserRepository.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Arrange
        var newPassword = "NewSecure123!";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.ResetPasswordAsync(null!, newPassword));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithEmptyPassword_ReturnsValidationError()
    {
        // Arrange
        var userId = "user-1";
        var emptyPassword = string.Empty;

        _mockAuthProvider
            .Setup(p => p.ValidatePassword(emptyPassword))
            .Returns(UserValidationResult.Invalid("Password is required"));

        // Act
        var result = await _service.ResetPasswordAsync(userId, emptyPassword);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("required"));
    }

    #endregion

    #region Email Immutability Tests

    [Fact]
    public async Task UpdateUserAsync_AttemptToChangeEmail_ReturnsValidationError()
    {
        // Arrange
        var userId = "user-1";
        var originalEmail = "original@example.com";
        var attemptedNewEmail = "changed@example.com";

        var existingUser = new User
        {
            Id = userId,
            GroupKey = "users",
            Name = "Test User",
            Email = originalEmail,
            PasswordHash = "hashed-password",
            Claims = [UserClaims.PostWrite],
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _service.UpdateUserAsync(
            userId,
            "Test User",
            attemptedNewEmail, // Attempting to change email
            [UserClaims.PostWrite],
            isActive: true);

        // Assert
        Assert.False(result.ValidationResult.IsValid);
        Assert.Contains(result.ValidationResult.Errors, e => e.Contains("Email cannot be changed"));

        // Verify that UpdateAsync was NOT called (no changes should be saved)
        _mockUserRepository.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_WithSameEmail_SucceedsAndUpdatesOtherFields()
    {
        // Arrange
        var userId = "user-1";
        var email = "user@example.com";
        var updatedName = "Updated Name";

        var existingUser = new User
        {
            Id = userId,
            GroupKey = "users",
            Name = "Original Name",
            Email = email,
            PasswordHash = "hashed-password",
            Claims = [UserClaims.PostWrite],
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UpdateUserAsync(
            userId,
            updatedName,
            email, // Same email - should be allowed
            [UserClaims.PostWrite, UserClaims.PageWrite],
            isActive: true);

        // Assert
        Assert.True(result.ValidationResult.IsValid);
        Assert.NotNull(result.User);
        Assert.Equal(updatedName, result.User.Name);
        Assert.Equal(email, result.User.Email); // Email unchanged
        Assert.Equal(2, result.User.Claims.Count);

        // Verify that UpdateAsync WAS called (changes were saved)
        _mockUserRepository.Verify(
            r => r.UpdateAsync(It.Is<User>(u => 
                u.Name == updatedName && 
                u.Email == email &&
                u.Claims.Count == 2), 
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WithEmailCaseChange_SucceedsAsSameEmail()
    {
        // Arrange
        var userId = "user-1";
        var originalEmail = "user@example.com"; // Stored as lowercase
        var caseChangedEmail = "User@Example.COM"; // Different case, same email
        var updatedName = "Updated Name";

        var existingUser = new User
        {
            Id = userId,
            GroupKey = "users",
            Name = "Original Name",
            Email = originalEmail, // Already normalized to lowercase
            PasswordHash = "hashed-password",
            Claims = [UserClaims.PostWrite],
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UpdateUserAsync(
            userId,
            updatedName,
            caseChangedEmail, // Different case but same normalized email
            [UserClaims.PostWrite],
            isActive: true);

        // Assert
        Assert.True(result.ValidationResult.IsValid); // Should succeed!
        Assert.NotNull(result.User);
        Assert.Equal(updatedName, result.User.Name);
        Assert.Equal(originalEmail, result.User.Email); // Email stays lowercase normalized

        // Verify that UpdateAsync WAS called (update succeeded)
        _mockUserRepository.Verify(
            r => r.UpdateAsync(It.Is<User>(u => 
                u.Name == updatedName && 
                u.Email == originalEmail), // Normalized to lowercase
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
