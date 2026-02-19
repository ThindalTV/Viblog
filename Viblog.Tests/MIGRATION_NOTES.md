# Migration to ApplicationUser - Test Files Need Update

## Status: Tests Need Refactoring

The following test files still reference the old `User` entity and `IUserRepository` and need to be updated to work with `ApplicationUser` and `UserManager<ApplicationUser>`:

### Test Files to Update:

1. **Viblog.Tests\Authentication\UserManagementServiceTests.cs**
   - Replace `Mock<IUserRepository>` with `Mock<UserManager<ApplicationUser>>`
   - Note: Mocking UserManager is complex - consider using InMemory Identity or integration tests instead
   - Change all `User` references to `ApplicationUser`
   - Change `User.Name` to `ApplicationUser.DisplayName`
   - Change `User.Claims` to `ApplicationUser.CustomClaims`

2. **Viblog.Tests\Authentication\LocalAuthenticationProviderTests.cs**
   - Replace `Mock<IUserRepository>` with `Mock<UserManager<ApplicationUser>>`
   - Update all `User` references to `ApplicationUser`

3. **Viblog.Tests\Integration\AuthenticationIntegrationTests.cs**
   - Replace `IUserRepository` with `UserManager<ApplicationUser>`
   - Update all `User` references to `ApplicationUser`

4. **Viblog.Tests\Integration\FileSystemTestFixture.cs**
   - Remove `IUserRepository UserRepository` property
   - Add `UserManager<ApplicationUser>` setup if needed for integration tests

### Recommendation: Use Integration Tests for Identity

Mocking `UserManager<ApplicationUser>` is notoriously difficult because:
- It has 10+ constructor dependencies
- Many methods are not virtual
- Complex internal state management

**Better approach:**
```csharp
// Use InMemory database for Identity tests
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));
    
services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
```

Then test against the real `UserManager<ApplicationUser>` instance.

### Quick Fix for Now

Temporarily mark these test classes as `[Trait("Category", "Pending")]` or skip them until they can be properly refactored:

```csharp
[Trait("Category", "PendingMigration")]
public class UserManagementServiceTests
{
    // Tests temporarily disabled during ApplicationUser migration
}
```

## Files Already Updated ✅

- ✅ IUserManagementService interface
- ✅ UserManagementService implementation
- ✅ LocalAuthenticationProvider implementation  
- ✅ IUserManagementFacade interface
- ✅ UserManagementFacade implementation
- ✅ IUserProfileFacade interface
- ✅ UserProfileFacade implementation
- ✅ AdminAuthenticationStateProvider
- ✅ Users.razor page
- ✅ Profile.razor page
- ✅ AuthenticationResult class
- ✅ ApplicationUser moved to Infrastructure
- ✅ Removed User entity
- ✅ Removed IUserRepository and implementations
- ✅ Updated service registrations
