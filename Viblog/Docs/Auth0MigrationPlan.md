# Auth0 Integration Migration Plan

**Version:** 1.1  
**Date:** 2025-02-08  
**Status:** Planning Phase - Not Yet Executed  
**Last Updated:** Reorganized for clean teardown-then-rebuild approach

## Overview

This document outlines the complete migration plan for replacing the current ASP.NET Core Identity authentication system with Auth0. The migration will remove local password management while maintaining local user profile and permissions management through the admin interface.

**Documentation Index:**
- **Auth0-README.md** - Start here! Documentation index and quick start guide
- **Auth0QuickStartChecklist.md** - Condensed execution checklist ⭐ Use this while working
- **Auth0MigrationPlan.md** - This file (complete detailed plan)
- **Auth0Configuration.TEMPLATE.md** - Template for creating your config in Step 3
- **Auth0Configuration.md** - Will be created in Step 3 with your actual tenant settings

### Migration Strategy

- **Auth0** handles all authentication (login, logout, password management)
- **Admin interface** continues to manage user creation and permissions
- **Local database** stores user profiles and custom claims
- **Auth0** becomes the single source of truth for authentication
- **No public registration** - users created only through admin

### Two-Phase Approach

**Phase 1 (Steps 1-8): Complete Teardown**
- Remove ALL existing authentication infrastructure first
- Stub facades/services to maintain compilation
- Project compiles but authentication is non-functional
- **Clean slate before rebuilding**

**Phase 2 (Steps 9-14): Auth0 Integration**
- Build Auth0 authentication from scratch
- Integrate with existing user management
- Restore full authentication functionality

---

## PHASE 1: Complete Removal of ASP.NET Core Identity (Steps 1-8)

**Goal:** Remove all existing authentication infrastructure, stub remaining components for compilation.

---

## Migration Steps

## PHASE 1: Complete Removal of ASP.NET Core Identity (Steps 1-8)

**Goal:** Remove all existing authentication infrastructure, stub remaining components for compilation.

---

### Step 1: Document Current Identity Infrastructure Components

**Purpose:** Create a comprehensive inventory of all identity-related code before removal.

**Current Identity Infrastructure:**

#### Services & Providers
- `LocalAuthenticationProvider` - handles password verification using UserManager
- `IAuthenticationProvider` interface - authentication abstraction
- `AdminAuthenticationStateProvider` - manages authentication state and cookie sessions
- `UserManagementService` - user CRUD operations using UserManager

#### Database Entities (CosmosDB Containers)
- `Users` - AdminUser (extends IdentityUser)
- `Roles` - IdentityRole
- `UserClaims` - IdentityUserClaim<string>
- `UserRoles` - IdentityUserRole<string>
- `UserLogins` - IdentityUserLogin<string>

#### Service Registrations
1. `Program.cs` line 59:
   ```csharp
   builder.Services.AddIdentity<AdminUser, AdminUser>();
   ```

2. `Viblog.Data.CosmosDb/CosmosDbServiceExtensions.cs` lines 93-96:
   ```csharp
   // Register Identity infrastructure for AdminUser
   services.AddIdentityCore<AdminUser>()
       .AddEntityFrameworkStores<ApplicationDbContext>();
   ```

#### NuGet Packages
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` v10.0.3 (in Viblog.csproj and Viblog.Data.CosmosDb.csproj)
- `Microsoft.EntityFrameworkCore.Tools` v10.0.3 (development dependency)

#### Authentication Endpoints
- `/admin/api/login` - POST endpoint for credentials validation
- `/admin/api/logout` - POST endpoint for sign out

#### Configuration
- `AdminAuthenticationSettings.cs` - cookie scheme configuration
- Cookie authentication scheme: "AdminAuthenticationScheme"
- Cookie lifetime: 8 hours (30 days if persistent)

---

### Step 2: Remove ASP.NET Core Identity Infrastructure

**Purpose:** Remove all ASP.NET Core Identity dependencies and configuration.

#### Files to Modify

**1. `Viblog.Data/Viblog.Data.CosmosDb/Data/ApplicationDbContext.cs`**

Changes:
- Remove base class: `IdentityDbContext<AdminUser>` → `DbContext`
- Remove `base.OnModelCreating(builder);` call
- Remove `RemoveIdentityIndexes()` method (lines 34-49)
- Remove `ConfigureIdentityEntities()` method (lines 51-105)
- Remove Identity entity configurations:
  - IdentityRole
  - IdentityUserClaim<string>
  - IdentityUserRole<string>
  - IdentityUserLogin<string>
- Keep only `ConfigureBlogEntities()` method
- Keep AdminUser configuration but move to blog entities section

**2. `Viblog.Data/Viblog.Data.CosmosDb/CosmosDbServiceExtensions.cs`**

Changes:
- Remove lines 93-96:
  ```csharp
  services.AddIdentityCore<AdminUser>()
      .AddEntityFrameworkStores<ApplicationDbContext>();
  ```
- Remove associated TODO comment (lines 91-92)

**3. `Viblog/Program.cs`**

Changes:
- Remove line 59:
  ```csharp
  builder.Services.AddIdentity<AdminUser, AdminUser>();
  ```

**4. `Viblog/Viblog.csproj`**

Changes:
- Remove package reference:
  ```xml
  <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.3" />
  ```

**5. `Viblog.Data/Viblog.Data.CosmosDb/Viblog.Data.CosmosDb.csproj`**

Changes:
- Remove package reference:
  ```xml
  <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.3" />
  ```

---

### Step 3: Create Auth0 Configuration Documentation

**Purpose:** Document Auth0 tenant setup and integration requirements.

**Action:** Create `Viblog/Docs/Auth0Configuration.md` with complete setup guide (see section below).

---

### Step 4: Simplify ApplicationUser Entity

**Purpose:** Remove IdentityUser inheritance and Identity-specific properties.

#### File to Modify

**`Viblog.Infrastructure/Shared/Data/Entities/ApplicationUser.cs`**

Changes:
- Remove base class: `IdentityUser` → none
- Remove inherited properties (these come from IdentityUser):
  - PasswordHash
  - SecurityStamp
  - ConcurrencyStamp
  - PhoneNumber
  - PhoneNumberConfirmed
  - TwoFactorEnabled
  - LockoutEnd
  - LockoutEnabled
  - AccessFailedCount
  - UserName (will keep our own)
  - NormalizedUserName
  - NormalizedEmail
  - EmailConfirmed (will keep our own)

- Keep existing properties:
  - Id (string)
  - Email (string)
  - DisplayName (string)
  - CustomClaims (List<string>)
  - IsActive (bool)
  - LastLoginAt (DateTimeOffset?)
  - GroupKey (string)
  - CreatedAt (DateTimeOffset)
  - UpdatedAt (DateTimeOffset)
  - IsDeleted (bool)
  - DeletedAt (DateTimeOffset?)

- Add new Auth0-specific properties:
  - `Auth0UserId` (string?) - e.g., "auth0|507f1f77bcf86cd799439011"
  - `Auth0LastSync` (DateTimeOffset?) - last sync timestamp with Auth0

**Example new structure:**
```csharp
public class AdminUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> CustomClaims { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }
    public string GroupKey { get; set; } = "users";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Auth0 integration (to be used after Auth0 setup)
    public string? Auth0UserId { get; set; }
    public DateTimeOffset? Auth0LastSync { get; set; }
}
```

---

### Step 5: Remove Local Authentication Services

**Purpose:** Remove password validation and local authentication logic completely.

#### Files to DELETE

1. `Viblog/Admin/Services/Authentication/LocalAuthenticationProvider.cs`
2. `Viblog.Infrastructure/Shared/Authentication/IAuthenticationProvider.cs`
3. `Viblog.Infrastructure/Shared/Authentication/PasswordChangeResult.cs`
4. `Viblog/Admin/Configuration/AdminAuthenticationSettings.cs`
5. `Viblog/Docs/IdentityCosmosDbConfiguration.md` (outdated)

#### Files to MODIFY

**`Viblog/Admin/Services/Authentication/UserManagementService.cs`**

Changes:
- Remove `UserManager<ApplicationUser>` dependency
- Remove `IAuthenticationProvider` dependency
- Remove password-related methods completely:
  - Password validation in `CreateUserAsync()`
  - Password change logic
  - `ChangePasswordAsync()` method
- Simplify to direct database CRUD operations (for now, will add Auth0 sync later)
- Change constructor to not require UserManager or IAuthenticationProvider

**`Viblog.Infrastructure/Shared/Authentication/IUserManagementService.cs`**

Changes:
- Remove `password` parameter from `CreateUserAsync()` method signature
- Remove password validation-related return values

**`Viblog/Admin/RegisterAdminExtensions.cs`**

Changes:
- Remove `IAuthenticationProvider` service registration (line ~48)
- Comment out or temporarily remove authentication middleware configuration (lines 62-72)
- Keep authorization policies (they will still work with Auth0)

---

### Step 6: Remove Authentication Tests and Endpoints

**Purpose:** Remove all obsolete authentication test files and custom login/logout endpoints.

#### Files to DELETE

1. `Viblog.Tests/Authentication/LocalAuthenticationProviderTests.cs`
2. `Viblog.Tests/Authentication/FileSystemAuthenticationProviderTests.cs`
3. `Viblog.Tests/Integration/AuthenticationIntegrationTests.cs`

#### Files to MODIFY

**`Viblog/Admin/RegisterAdminExtensions.cs` → `MapViblogAdminEndpoints()`**

Changes:
- Remove `/admin/api/login` POST endpoint (lines 171-196)
- Remove `/admin/api/logout` POST endpoint (lines 198-204)
- Keep the method shell for now (will add Auth0 endpoints later)

**`Viblog.Tests/Facades/UserManagementFacadeTests.cs`**

Changes:
- Comment out or remove tests that depend on `IAuthenticationProvider`
- Keep user CRUD tests
- Mark file for future Auth0 test updates

**`Viblog.Tests/Authentication/UserManagementServiceTests.cs`**

Changes:
- Remove password validation tests
- Keep basic CRUD tests
- Mark file for future Auth0 test updates

---

### Step 7: Stub Facades and Services for Compilation

**Purpose:** Create minimal stubs to allow project compilation while authentication is removed.

#### Files to MODIFY

**`Viblog/Admin/Services/AdminAuthenticationStateProvider.cs`**

Changes:
- Keep class shell but comment out or remove method implementations
- Add TODO comments for Auth0 implementation
- Make it return empty/unauthenticated states for now:

```csharp
public class AdminAuthenticationStateProvider : AuthenticationStateProvider
{
    // TODO: Implement Auth0 integration in Step 8

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(anonymous));
    }
}
```

**`Viblog/Admin/Facades/UserManagementFacade.cs`**

Changes:
- Remove authentication-related method calls
- Stub out password-related operations
- Keep basic CRUD operations functional
- Add TODO comments for Auth0 integration in Step 13

**`Viblog/Admin/Pages/Login.razor`**

Changes (temporary):
- Keep page structure
- Add message: "Authentication system under maintenance - Auth0 integration in progress"
- Remove form functionality (will be replaced in Step 12)

**At this point, project should compile without errors but authentication will not work.**

---

### Step 8: Create Auth0 Configuration Documentation

**Purpose:** Document Auth0 tenant setup and integration requirements before starting Auth0 integration.

**Action:** Create `Viblog/Docs/Auth0Configuration.md` with complete setup guide (see Auth0 Configuration Guide section below).

**At this point, Phase 1 (Teardown) is complete. Project compiles but authentication is non-functional.**

---

## PHASE 2: Auth0 Integration (Steps 9-14)

---

### Step 9: Update AdminAuthenticationStateProvider for Auth0

**Purpose:** Replace stubbed authentication with Auth0 JWT validation.

**Completion Criteria:**
- [ ] AdminAuthenticationStateProvider reads from HttpContext
- [ ] Validates Auth0 claims
- [ ] Maps to local user via email lookup
- [ ] Returns anonymous if user not found/inactive
- [ ] Project compiles
- [ ] Ready to commit: "feat: implement Auth0 authentication state provider"

**Unit Tests Required:**
- Test GetAuthenticationStateAsync with mock HttpContext
- Test claim mapping from Auth0 to local user
- Test user lookup by email
- Test inactive user returns anonymous
- Test deleted user returns anonymous
- Mock IUserManagementService and IHttpContextAccessor

#### File to Modify

**`Viblog/Admin/Services/AdminAuthenticationStateProvider.cs`**

Changes:
- Replace stub implementation with Auth0 integration
- Inherit from `AuthenticationStateProvider` (already done in Step 7)
- Implement `GetAuthenticationStateAsync()`:
  - Read authentication state from HTTP context
  - Validate Auth0 JWT token
  - Map Auth0 claims to local user claims
  - Verify user still exists and is active in local DB
- Add Auth0 token refresh logic

**New dependencies:**
- Keep `IUserManagementService` for user lookup
- Keep `IHttpContextAccessor` for reading authentication context

---

### Step 10: Add Auth0 Login/Logout/Callback Endpoints

**Purpose:** Add Auth0 redirect handlers to enable authentication.

**Completion Criteria:**
- [ ] /admin/auth/login endpoint added (redirects to Auth0)
- [ ] /admin/auth/callback endpoint added (handles Auth0 response)
- [ ] /admin/auth/logout endpoint added (signs out from Auth0)
- [ ] Project compiles
- [ ] Ready to commit: "feat: add Auth0 authentication endpoints"

**Unit Tests Required:**
- Integration tests for endpoint routing (mock Auth0 responses)
- Test callback success scenario
- Test callback failure scenario
- Test logout clears session

#### File to Modify

**`Viblog/Admin/RegisterAdminExtensions.cs` → `MapViblogAdminEndpoints()`**

Add (replacing the empty shell from Step 6):
- `/admin/auth/login` GET endpoint - redirects to Auth0 Universal Login
- `/admin/auth/logout` GET endpoint - signs out and redirects to Auth0 logout
- `/admin/auth/callback` GET endpoint - Auth0 callback handler

**Example new endpoints:**
```csharp
// Redirect to Auth0 login
endpoints.MapGet("/admin/auth/login", async (HttpContext context) =>
{
    await context.ChallengeAsync("Auth0", new AuthenticationProperties
    {
        RedirectUri = "/admin"
    });
}).AllowAnonymous();

// Auth0 callback handler
endpoints.MapGet("/admin/auth/callback", async (HttpContext context) =>
{
    var result = await context.AuthenticateAsync("Auth0");
    if (result.Succeeded)
    {
        // Sign in user and create local session
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal);
        context.Response.Redirect("/admin");
    }
    else
    {
        context.Response.Redirect("/admin/login?error=auth0");
    }
}).AllowAnonymous();

// Sign out from Auth0 and local session
endpoints.MapGet("/admin/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync("Auth0", new AuthenticationProperties
    {
        RedirectUri = "/admin/login"
    });
}).RequireAuthorization();
```

---

### Step 11: Update User Management for Auth0 Integration

**Purpose:** Synchronize local user management with Auth0 Management API.

#### Files to CREATE

**1. `Viblog.Infrastructure/Shared/Authentication/IAuth0UserSyncService.cs`**

Interface for Auth0 synchronization:
```csharp
public interface IAuth0UserSyncService
{
    Task<string> CreateAuth0UserAsync(string email, string displayName, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string auth0UserId, CancellationToken cancellationToken = default);
    Task UpdateAuth0UserAsync(string auth0UserId, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteAuth0UserAsync(string auth0UserId, CancellationToken cancellationToken = default);
}
```

**2. `Viblog/Admin/Services/Authentication/Auth0UserSyncService.cs`**

Implementation using Auth0 Management API:
- Uses `ManagementApiClient` from Auth0.ManagementApi package
- Creates users with email connection
- Sends password reset emails (user sets own password)
- Blocks/unblocks users based on IsActive status
- Deletes users when soft-deleted locally

**3. `Viblog/Admin/Configuration/Auth0Settings.cs`**

Configuration class:
```csharp
public class Auth0Settings
{
    public string Domain { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ManagementApiClientId { get; set; } = string.Empty;
    public string ManagementApiClientSecret { get; set; } = string.Empty;
}
```

#### Files to MODIFY

**`Viblog/Admin/Services/Authentication/UserManagementService.cs`**

Update `CreateUserAsync()`:
1. Validate user data (no password validation)
2. Create user in local database
3. Call `IAuth0UserSyncService.CreateAuth0UserAsync()`
4. Store returned `Auth0UserId` in local user record
5. Auth0 automatically sends password reset email

Update `UpdateUserAsync()`:
1. Update local user data
2. If `IsActive` changed, call `IAuth0UserSyncService.UpdateAuth0UserAsync()`

Update `DeleteUserAsync()`:
1. Soft delete local user
2. Call `IAuth0UserSyncService.DeleteAuth0UserAsync()`

---

### Step 12: Update Authentication Middleware Configuration

**Purpose:** Replace stubbed/commented authentication with Auth0 OpenID Connect.

#### File to Modify

**`Viblog/Admin/RegisterAdminExtensions.cs` → `AddViblogAdmin()`**

Replace the stubbed/commented authentication configuration (from Step 5) with Auth0 OpenID Connect:
```csharp
// Load Auth0 settings
var auth0Settings = new Auth0Settings();
collection.AddSingleton(auth0Settings);

// Configure authentication
collection.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Auth0";
})
.AddCookie(options =>
{
    options.LoginPath = "/admin/auth/login";
    options.AccessDeniedPath = "/admin/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.Name = "Viblog.Admin.Auth";
})
.AddOpenIdConnect("Auth0", options =>
{
    options.Authority = $"https://{auth0Settings.Domain}";
    options.ClientId = auth0Settings.ClientId;
    options.ClientSecret = auth0Settings.ClientSecret;
    options.ResponseType = OpenIdConnectResponseType.Code;
    
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    
    options.CallbackPath = new PathString("/admin/auth/callback");
    options.ClaimsIssuer = "Auth0";
    options.SaveTokens = true;
    
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            context.ProtocolMessage.SetParameter("audience", auth0Settings.Audience);
            return Task.CompletedTask;
        }
    };
});
```

Add service registration:
```csharp
collection.AddScoped<IAuth0UserSyncService, Auth0UserSyncService>();
```

#### NuGet Packages to ADD

**`Viblog/Viblog.csproj`:**
```xml
<PackageReference Include="Auth0.ManagementApi" Version="7.26.2" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="10.0.0" />
```

---

### Step 13: Update Login.razor Page

**Purpose:** Replace temporary stub message with Auth0 redirect.

#### File to Modify

**`Viblog/Admin/Pages/Login.razor`**

Replace entire contents with simplified Auth0 redirect:

```razor
@page "/admin/login"
@using Microsoft.AspNetCore.Authorization
@attribute [AllowAnonymous]

<div class="admin-login-container">
    <div class="login-card">
        <div class="login-header">
            <div class="login-icon">
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 2L2 7L12 12L22 7L12 2Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    <path d="M2 17L12 22L22 17" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    <path d="M2 12L12 17L22 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
            </div>
            <h2>Admin Login</h2>
            <p class="login-subtitle">Sign in to access your dashboard</p>
        </div>
        <div class="login-body">
            @if (_showError)
            {
                <div class="error-message">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                        <line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                        <circle cx="12" cy="16" r="1" fill="currentColor"/>
                    </svg>
                    <span>Authentication failed. Please try again.</span>
                </div>
            }

            <div class="form-actions">
                <a href="/admin/auth/login" class="btn-login">
                    <span>Sign In with Auth0</span>
                </a>
            </div>
        </div>
    </div>
</div>

@code {
    [SupplyParameterFromQuery(Name = "error")]
    private string? ErrorParam { get; set; }
    
    private bool _showError => ErrorParam == "auth0";
}
```

**Note:** Keep existing CSS file `Login.razor.css` (may need minor button style adjustments)

---

### Step 14: Update UserManagementFacade and Related Components

**Purpose:** Remove stubs and add full Auth0 integration to user management UI operations.

#### File to Modify

**`Viblog/Admin/Facades/UserManagementFacade.cs`**

Changes:
- Add `IAuth0UserSyncService` dependency
- Update `CreateUserAsync()`:
  - Remove password parameter from UI
  - Local user creation now triggers Auth0 user creation
  - Show success message: "User created. Password reset email sent to {email}"
- Add `SendPasswordResetAsync(string userId)` method:
  - Looks up user's Auth0UserId
  - Calls `IAuth0UserSyncService.SendPasswordResetEmailAsync()`
- Handle Auth0 API errors gracefully:
  - Catch Auth0 exceptions
  - Display user-friendly error messages
  - Log detailed errors for debugging

**`Viblog/Admin/Pages/Users.razor`**

Changes:
- Remove password input field from user creation form
- Add "Send Password Reset" button to user grid actions
- Update success message after creation

**`Viblog/Admin/Pages/UserEdit.razor`**

Changes:
- Remove password change section
- Add "Send Password Reset Email" button
- Remove email editing (email is immutable, linked to Auth0)

---

### Step 12: Remove Authentication-Related Tests

**Purpose:** Clean up obsolete test files and update remaining tests.

#### Files to DELETE

1. `Viblog.Tests/Authentication/LocalAuthenticationProviderTests.cs`
2. `Viblog.Tests/Authentication/FileSystemAuthenticationProviderTests.cs`
3. `Viblog.Tests/Integration/AuthenticationIntegrationTests.cs`

#### Files to MODIFY

**`Viblog.Tests/Facades/UserManagementFacadeTests.cs`**

Changes:
- Mock `IAuth0UserSyncService` instead of `IAuthenticationProvider`
- Update `CreateUserAsync` tests to not include password
- Add tests for Auth0 sync error handling
- Verify Auth0UserId is stored after creation

**`Viblog.Tests/Authentication/UserManagementServiceTests.cs`**

Changes:
- Remove password validation tests
- Mock `IAuth0UserSyncService`
- Test Auth0 sync integration in create/update/delete scenarios

---

## Auth0 Configuration Guide

### Required Auth0 tenant setup before migration:

#### 1. Create Auth0 Tenant
1. Sign up at [auth0.com](https://auth0.com)
2. Create a new tenant
3. Choose region closest to your users (e.g., US, EU, AU)

#### 2. Create Regular Web Application
1. Navigate to Applications → Create Application
2. **Name:** Viblog Admin
3. **Type:** Regular Web Application
4. **Technology:** ASP.NET Core

#### 3. Configure Application Settings

**In Application Settings:**

- **Allowed Callback URLs:**
  ```
  https://yourdomain.com/admin/auth/callback
  https://localhost:5001/admin/auth/callback
  http://localhost:5000/admin/auth/callback
  ```

- **Allowed Logout URLs:**
  ```
  https://yourdomain.com/admin/login
  https://localhost:5001/admin/login
  http://localhost:5000/admin/login
  ```

- **Allowed Web Origins:**
  ```
  https://yourdomain.com
  https://localhost:5001
  http://localhost:5000
  ```

**Save the following from the Settings tab:**
- Domain (e.g., `your-tenant.auth0.com`)
- Client ID
- Client Secret

#### 4. Configure Database Connection

1. Navigate to Authentication → Database
2. Use the default `Username-Password-Authentication` connection
3. Configure settings:
   - ✅ **Disable Sign Ups** (users created via admin only)
   - **Password Policy:** Select "Good" or "Excellent"
   - **Minimum Password Length:** 8-12 characters recommended
4. Customize Password Reset Email template (optional)

#### 5. Create Management API Application

For programmatic user creation:

1. Navigate to Applications → APIs → Auth0 Management API
2. Click "Machine to Machine Applications" tab
3. Authorize your "Viblog Admin" application OR create a new M2M application
4. Grant the following scopes:
   - `create:users`
   - `read:users`
   - `update:users`
   - `delete:users`
   - `update:users_app_metadata`
   - `create:user_tickets` (for password reset emails)

**Save:**
- Management API Client ID
- Management API Client Secret

#### 6. Environment Configuration

Add to `appsettings.json` (Development) and Azure Configuration (Production):

```json
{
  "Auth0": {
    "Domain": "your-tenant.auth0.com",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Audience": "https://your-tenant.auth0.com/api/v2/",
    "ManagementApi": {
      "ClientId": "management-api-client-id",
      "ClientSecret": "management-api-client-secret"
    }
  }
}
```

**For production:** Use Azure Key Vault or User Secrets for sensitive values.

#### 7. Customize Universal Login (Optional)

1. Navigate to Branding → Universal Login
2. Customize the login page appearance
3. Upload your logo
4. Adjust colors to match Viblog branding

---

## User Migration Strategy

### For Existing Users (Post-Migration)

After migration, existing users in your database will need to be synchronized with Auth0:

**Option 1: Bulk Import Script (Recommended)**
1. Create migration script that:
   - Reads all active users from CosmosDB
   - Creates corresponding users in Auth0 via Management API
   - Stores Auth0UserId in local user records
   - Triggers password reset email for each user

**Option 2: Lazy Migration**
1. Users remain in local DB
2. On first login attempt, check if Auth0UserId exists
3. If not, create Auth0 user and send password reset email
4. User must reset password to gain access

**Recommended:** Option 1 with advance communication to users about password reset.

---

## Authentication Flow After Migration

### User Creation Flow:
1. Admin creates user via Admin UI (email + display name + claims)
2. User saved to local CosmosDB with profile and claims
3. User created in Auth0 via Management API
4. Auth0 sends password reset email automatically
5. User clicks link, sets password, and can log in
6. `Auth0UserId` stored in local user record for linking

### User Login Flow:
1. User navigates to `/admin/login`
2. Clicks "Sign In with Auth0" button
3. Redirects to Auth0 Universal Login (`/admin/auth/login`)
4. User enters credentials on Auth0 page
5. Auth0 validates and redirects to `/admin/auth/callback`
6. App validates Auth0 token
7. App looks up user in local DB by email
8. Creates local session with claims from both Auth0 + local DB
9. User accesses admin with full permissions

### Password Reset Flow:
1. Admin clicks "Send Password Reset" in user management
2. App calls Auth0 Management API
3. Auth0 sends password reset email to user
4. User completes reset on Auth0's secure page
5. User can log in with new password

---

## Testing Strategy

### Unit Tests
- Mock `IAuth0UserSyncService` in all user management tests
- Test error handling for Auth0 API failures
- Verify Auth0UserId storage and retrieval

### Integration Tests
- Use Auth0 **test tenant** (separate from production)
- Test complete user creation → Auth0 sync → login flow
- Test password reset email delivery
- Test user deactivation blocks Auth0 access

### Manual Testing Checklist
- [ ] Create user in admin → verify Auth0 user created
- [ ] User receives password reset email
- [ ] User can set password and log in
- [ ] User session includes custom claims from local DB
- [ ] Deactivate user → verify Auth0 login blocked
- [ ] Delete user → verify Auth0 user removed
- [ ] Test logout flow
- [ ] Test session timeout/revalidation

---

## Rollback Plan

If migration encounters issues:

1. **Revert code changes** via Git:
   ```bash
   git checkout <commit-before-migration>
   ```

2. **Restore NuGet packages:**
   ```bash
   dotnet restore
   ```

3. **Database:** No schema changes required (ApplicationUser in CosmosDB is unaffected)

4. **Users:** Auth0 users can remain (no harm) or be bulk-deleted via Management API

---

## Cost Considerations

### Auth0 Pricing (as of 2025)

**Free Tier:**
- 7,500 Monthly Active Users (MAU)
- Unlimited logins
- Social connections
- Email/password database
- Management API access

**Paid Tiers (if exceeding free tier):**
- Essential: $35/month (500 MAU) + $0.07 per additional MAU
- Professional: $240/month (1,000 MAU) + custom pricing

**Recommendation:** Start with free tier and monitor MAU in Auth0 dashboard.

---

## Security Considerations

1. **Secrets Management:**
   - Store Auth0 credentials in Azure Key Vault (production)
   - Use User Secrets for local development
   - Never commit secrets to Git

2. **Token Validation:**
   - Validate JWT signature using Auth0 public keys
   - Verify token issuer matches your tenant
   - Check token expiration

3. **User Permissions:**
   - Auth0 handles authentication (who you are)
   - Local database handles authorization (what you can do)
   - Custom claims stored locally, not in Auth0

4. **Password Policy:**
   - Enforce strong password policy in Auth0
   - Consider enabling MFA (multi-factor authentication)

---

## Important Notes & Decisions

### Email as Immutable Identifier
- Email cannot be changed after creation
- Email links Auth0 user to local user record
- To change email, must create new user account

### No Direct Password Management
- Admins cannot set user passwords directly
- All passwords managed through Auth0 password reset flow
- Users always set their own passwords

### Permissions Management
- Local database remains source of truth for `CustomClaims`
- Auth0 user metadata NOT used for permissions
- Claims injected into session after Auth0 authentication

### User Deactivation
- Setting `IsActive = false` in local DB also blocks Auth0 user
- Reactivating user re-enables Auth0 access
- Soft delete removes Auth0 user entirely

---

## Timeline & Phases

**Recommended Execution Plan:**

**Phase 1: Complete Removal of Authentication System (Steps 1-8)**
- ~6-10 hours
- Document existing infrastructure
- Remove ASP.NET Core Identity completely
- Remove all authentication services, providers, and tests
- Stub facades and services for compilation
- Create Auth0 documentation
- **Milestone:** Project compiles but authentication is non-functional

**Phase 2: Auth0 Integration (Steps 9-14)**
- ~10-16 hours
- Update authentication state provider for Auth0
- Add Auth0 endpoints (login/logout/callback)
- Create Auth0 user sync service
- Configure OpenID Connect middleware
- Update login page
- Integrate Auth0 into user management facade
- **Milestone:** Full Auth0 authentication working

**Phase 3: Testing & Validation**
- ~4-8 hours
- Update unit tests
- Write new Auth0 integration tests
- End-to-end testing
- User migration script
- **Milestone:** Production ready

**Total Estimated Time:** 20-34 hours

---

## Status: Ready for Execution ✅

**Quick Start:** See `Auth0QuickStartChecklist.md` for condensed execution guide

**Execution Instructions:**
1. Follow steps in order (1 through 15)
2. Complete all tasks in each step before moving to next
3. **Commit after each step** using suggested commit messages
4. Test compilation/functionality after each step
5. Pause between phases if needed

**Current Step:** 1 - Document Current Identity Infrastructure  
**Next Action:** Review this plan, then commit  
**Next Commit Message:** `docs: add Auth0 migration documentation and plan`

**After this commit:** Begin Step 1 execution (already complete - this IS the documentation)

**Phase 1 Goal:** Remove all existing auth (Steps 1-8)  
**Phase 2 Goal:** Implement Auth0 (Steps 9-15)

**Document Version:** 1.2  
**Last Updated:** 2025-02-08 (Updated for immediate execution with detailed Auth0 setup)  
**Execution Status:** Ready - Step 1  
**Reviewed By:** N/A (Solo developer)  
**Approved By:** N/A (Solo developer)

---

## Additional Findings

### Aspire Projects
- ✅ Aspire AppHost - No authentication code (only infrastructure orchestration)
- ✅ Aspire ServiceDefaults - No authentication code (only observability)
- No changes required to Aspire projects during migration

### CosmosDB Service Extensions
- ✅ Found `AddIdentityCore<ApplicationUser>()` registration in `Viblog.Data.CosmosDb/CosmosDbServiceExtensions.cs`
- This registration provides `UserManager<ApplicationUser>` used by authentication services
- Must be removed in Step 2 (marked with TODO comment in current code)

### Storage Providers
- ✅ Viblog.Data.Filesystem - No authentication code (clean)
- ✅ Viblog.Data.AzureStorage - No authentication code (clean)
- All authentication logic isolated to CosmosDB provider and Admin services

---

**END OF MIGRATION PLAN**
