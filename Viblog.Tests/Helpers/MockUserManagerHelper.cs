using Microsoft.AspNetCore.Identity;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Tests.Helpers;

/// <summary>
/// Helper class for creating mock UserManager for testing
/// </summary>
public static class MockUserManagerHelper
{
    /// <summary>
    /// Create a mock UserManager with default setup
    /// </summary>
    public static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
        
        mockUserManager.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
        mockUserManager.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());

        return mockUserManager;
    }
}
