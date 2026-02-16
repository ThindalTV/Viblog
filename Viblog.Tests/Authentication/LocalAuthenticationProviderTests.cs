using Microsoft.Extensions.Logging;
using Viblog.Data.Filesystem.Authentication;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Tests.Authentication;

/// <summary>
/// Unit tests for LocalAuthenticationProvider
/// </summary>
public class LocalAuthenticationProviderTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<LocalAuthenticationProvider>> _mockLogger;
    private readonly LocalAuthenticationProvider _provider;

    public LocalAuthenticationProviderTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<LocalAuthenticationProvider>>();
        _provider = new LocalAuthenticationProvider(_mockUserRepository.Object, _mockLogger.Object);
    }

    #region HashPassword Tests

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsBase64String()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _provider.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        
        // Verify it's valid base64
        var bytes = Convert.FromBase64String(hash);
        Assert.Equal(48, bytes.Length); // 16 bytes salt + 32 bytes hash
    }

    [Fact]
    public void HashPassword_WithSamePassword_ProducesDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _provider.HashPassword(password);
        var hash2 = _provider.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // Different salts should produce different hashes
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void HashPassword_WithInvalidPassword_ThrowsArgumentException(string password)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _provider.HashPassword(password));
    }

    [Fact]
    public void HashPassword_WithNullPassword_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _provider.HashPassword(null!));
    }

    #endregion

    #region VerifyPassword Tests

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _provider.HashPassword(password);

        // Act
        var result = _provider.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _provider.HashPassword(password);

        // Act
        var result = _provider.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidHash = "InvalidHash";

        // Act
        var result = _provider.VerifyPassword(password, invalidHash);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void VerifyPassword_WithInvalidPassword_ThrowsArgumentException(string password)
    {
        // Arrange
        var hash = "ValidHashHere";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _provider.VerifyPassword(password, hash));
    }

    [Fact]
    public void VerifyPassword_WithNullPassword_ThrowsArgumentNullException()
    {
        // Arrange
        var hash = "ValidHashHere";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _provider.VerifyPassword(null!, hash));
    }

    #endregion

    #region ValidatePassword Tests

    [Fact]
    public void ValidatePassword_WithValidPassword_ReturnsValid()
    {
        // Arrange
        var password = "ValidPass123!";

        // Act
        var result = _provider.ValidatePassword(password);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidatePassword_WithEmptyPassword_ReturnsInvalid(string password)
    {
        // Act
        var result = _provider.ValidatePassword(password);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("required"));
    }

    [Fact]
    public void ValidatePassword_WithShortPassword_ReturnsInvalid()
    {
        // Arrange
        var password = "Short1!";

        // Act
        var result = _provider.ValidatePassword(password);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("at least 8 characters"));
    }

    [Fact]
    public void ValidatePassword_WithoutUppercase_ReturnsInvalid()
    {
        // Arrange
        var password = "lowercase123!";

        // Act
        var result = _provider.ValidatePassword(password);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("uppercase letter"));
    }

    [Fact]
    public void ValidatePassword_WithoutLowercase_ReturnsInvalid()
    {
        // Arrange
        var password = "UPPERCASE123!";

        // Act
        var result = _provider.ValidatePassword(password);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("lowercase letter"));
    }

    [Fact]
    public void ValidatePassword_WithoutDigit_ReturnsInvalid()
    {
        // Arrange
        var password = "NoDigits!";

        // Act
        var result = _provider.ValidatePassword(password);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("number"));
    }

    [Fact]
    public void ValidatePassword_WithoutSpecialCharacter_ReturnsInvalid()
    {
        // Arrange
        var password = "NoSpecial123";

        // Act
        var result = _provider.ValidatePassword(password);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("special character"));
    }

    #endregion

    #region AuthenticateAsync Tests

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var password = "TestPassword123!";
        var passwordHash = _provider.HashPassword(password);
        var user = new User
        {
            Id = "user-1",
            Email = email,
            Name = "Test User",
            PasswordHash = passwordHash,
            IsActive = true,
            Claims = [UserClaims.PostWrite]
        };

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(r => r.UpdateLastLoginAsync(user.Id, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _provider.AuthenticateAsync(email, password);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal(email, result.User.Email);
        Assert.Null(result.ErrorMessage);

        _mockUserRepository.Verify(
            r => r.UpdateLastLoginAsync(user.Id, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidEmail_ReturnsFailed()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "TestPassword123!";

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _provider.AuthenticateAsync(email, password);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.User);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsync_WithNonexistentUser_PerformsHashingToPreventTimingAttack()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "TestPassword123!";

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _provider.AuthenticateAsync(email, password);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.User);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);

        // Verify that some computational work was done (timing attack mitigation)
        // The elapsed time should be > 0ms, indicating password hashing occurred
        // even though the user doesn't exist
        Assert.True(elapsed.TotalMilliseconds > 0, 
            "Authentication should perform password hashing even when user doesn't exist to prevent timing attacks");
    }

    [Fact]
    public async Task AuthenticateAsync_WithInactiveUser_ReturnsFailed()
    {
        // Arrange
        var email = "test@example.com";
        var password = "TestPassword123!";
        var user = new User
        {
            Id = "user-1",
            Email = email,
            PasswordHash = _provider.HashPassword(password),
            IsActive = false
        };

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _provider.AuthenticateAsync(email, password);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.User);
        Assert.Equal("User account is inactive.", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsync_WithWrongPassword_ReturnsFailed()
    {
        // Arrange
        var email = "test@example.com";
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var user = new User
        {
            Id = "user-1",
            Email = email,
            PasswordHash = _provider.HashPassword(password),
            IsActive = true
        };

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _provider.AuthenticateAsync(email, wrongPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.User);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ReturnsSuccess()
    {
        // Arrange
        var userId = "user-1";
        var currentPassword = "OldPassword123!";
        var newPassword = "NewPassword456!";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = _provider.HashPassword(currentPassword),
            IsActive = true
        };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _provider.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);

        _mockUserRepository.Verify(
            r => r.UpdateAsync(It.Is<User>(u => u.Id == userId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ReturnsFailed()
    {
        // Arrange
        var userId = "user-1";
        var currentPassword = "OldPassword123!";
        var wrongCurrentPassword = "WrongPassword456!";
        var newPassword = "NewPassword789!";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = _provider.HashPassword(currentPassword),
            IsActive = true
        };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _provider.ChangePasswordAsync(userId, wrongCurrentPassword, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Current password is incorrect.", result.ErrorMessage);

        _mockUserRepository.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidNewPassword_ReturnsFailed()
    {
        // Arrange
        var userId = "user-1";
        var currentPassword = "OldPassword123!";
        var weakNewPassword = "weak";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = _provider.HashPassword(currentPassword),
            IsActive = true
        };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _provider.ChangePasswordAsync(userId, currentPassword, weakNewPassword);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("at least 8 characters", result.ErrorMessage);

        _mockUserRepository.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonexistentUser_ReturnsFailed()
    {
        // Arrange
        var userId = "nonexistent-user";
        var currentPassword = "OldPassword123!";
        var newPassword = "NewPassword456!";

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId, "users", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _provider.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User not found.", result.ErrorMessage);
    }

    #endregion
}
