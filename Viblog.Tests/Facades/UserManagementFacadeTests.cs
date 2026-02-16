using Viblog.Admin.Facades;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Tests.Facades;

/// <summary>
/// Unit tests for UserManagementFacade
/// </summary>
public class UserManagementFacadeTests
{
    private readonly Mock<IUserManagementService> _mockUserManagementService;
    private readonly UserManagementFacade _facade;

    public UserManagementFacadeTests()
    {
        _mockUserManagementService = new Mock<IUserManagementService>();
        _facade = new UserManagementFacade(_mockUserManagementService.Object);
    }

    [Fact]
    public async Task GetUsersAsync_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagingParams = new PagingParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<User>
        {
            Items = [new User { Email = "test@example.com" }],
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockUserManagementService
            .Setup(s => s.GetUsersAsync(pagingParams, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _facade.GetUsersAsync(pagingParams, includeInactive: false);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        _mockUserManagementService.Verify(
            s => s.GetUsersAsync(pagingParams, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_CallsServiceWithCorrectId()
    {
        // Arrange
        var userId = "user-1";
        var expectedUser = new User { Id = userId, Email = "test@example.com" };

        _mockUserManagementService
            .Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _facade.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task CreateUserAsync_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var name = "Test User";
        var email = "test@example.com";
        var password = "ValidPass123!";
        var claims = new List<string> { UserClaims.PostWrite };
        var expectedUser = new User { Name = name, Email = email };

        _mockUserManagementService
            .Setup(s => s.CreateUserAsync(name, email, password, claims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedUser, UserValidationResult.Valid()));

        // Act
        var (user, validationResult) = await _facade.CreateUserAsync(name, email, password, claims);

        // Assert
        Assert.NotNull(user);
        Assert.True(validationResult.IsValid);
        Assert.Equal(name, user.Name);
    }

    [Fact]
    public async Task UpdateUserAsync_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var userId = "user-1";
        var name = "Updated Name";
        var email = "updated@example.com";
        var claims = new List<string> { UserClaims.PostWrite };
        var isActive = true;
        var expectedUser = new User { Id = userId, Name = name, Email = email };

        _mockUserManagementService
            .Setup(s => s.UpdateUserAsync(userId, name, email, claims, isActive, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedUser, UserValidationResult.Valid()));

        // Act
        var (user, validationResult) = await _facade.UpdateUserAsync(userId, name, email, claims, isActive);

        // Assert
        Assert.NotNull(user);
        Assert.True(validationResult.IsValid);
        Assert.Equal(name, user.Name);
    }

    [Fact]
    public async Task DeleteUserAsync_CallsServiceWithCorrectId()
    {
        // Arrange
        var userId = "user-1";

        _mockUserManagementService
            .Setup(s => s.DeleteUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _facade.DeleteUserAsync(userId);

        // Assert
        Assert.True(result);
        _mockUserManagementService.Verify(
            s => s.DeleteUserAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void GetAvailableClaims_ReturnsAllClaims()
    {
        // Act
        var result = _facade.GetAvailableClaims();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(UserClaims.AllClaims.Count, result.Count);
        Assert.Contains(UserClaims.PostWrite, result);
        Assert.Contains(UserClaims.PageWrite, result);
        Assert.Contains(UserClaims.StatisticsRead, result);
        Assert.Contains(UserClaims.UserRead, result);
        Assert.Contains(UserClaims.UserWrite, result);
    }

    [Fact]
    public async Task AnyUsersExistAsync_CallsService()
    {
        // Arrange
        _mockUserManagementService
            .Setup(s => s.AnyUsersExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _facade.AnyUsersExistAsync();

        // Assert
        Assert.True(result);
    }
}
