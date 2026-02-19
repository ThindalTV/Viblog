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
    public static Mock<UserManager<AdminUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<AdminUser>>();
        var mockUserManager = new Mock<UserManager<AdminUser>>(
            store.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
        
        mockUserManager.Object.UserValidators.Add(new UserValidator<AdminUser>());
        mockUserManager.Object.PasswordValidators.Add(new PasswordValidator<AdminUser>());

        return mockUserManager;
    }
}
