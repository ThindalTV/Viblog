# Auth0 Migration - Quick Start Checklist

**Version:** 1.0  
**Purpose:** Condensed execution checklist for Auth0 migration  
**Full Details:** See `Auth0MigrationPlan.md`

---

## PHASE 1: Teardown (Steps 1-8)

### ☐ Step 1: Document Current Infrastructure
- Review documented infrastructure in plan
- **Commit:** `docs: document current identity infrastructure before Auth0 migration`

### ☐ Step 2: Remove ASP.NET Core Identity
**Files to modify:** 5 files
- [ ] Remove `IdentityDbContext` from ApplicationDbContext
- [ ] Remove Identity config from CosmosDbServiceExtensions
- [ ] Remove `AddIdentity` from Program.cs
- [ ] Remove Identity package from Viblog.csproj
- [ ] Remove Identity package from Viblog.Data.CosmosDb.csproj
- **Verify:** Project compiles (warnings OK)
- **Commit:** `refactor: remove ASP.NET Core Identity infrastructure`

### ☐ Step 3: Setup Auth0 Tenant (Non-Code)
**Time:** 30-45 minutes

**In Auth0 Dashboard:**
1. [ ] Create tenant (dev environment)
2. [ ] Create "Viblog Admin" Regular Web Application
3. [ ] Configure callback URLs:
   - `https://localhost:7001/admin/auth/callback`
   - `http://localhost:5000/admin/auth/callback`
4. [ ] Configure logout URLs:
   - `https://localhost:7001/admin/login`
   - `http://localhost:5000/admin/login`
5. [ ] Copy credentials (Domain, Client ID, Client Secret)
6. [ ] Disable signups in Username-Password-Authentication
7. [ ] Set password policy to "Excellent"
8. [ ] Authorize Viblog Admin app for Management API
9. [ ] Grant scopes: create:users, read:users, update:users, delete:users, create:user_tickets
10. [ ] Create `Viblog/Docs/Auth0Configuration.md` with YOUR settings
11. [ ] Save secrets to TEMP notepad (will add to User Secrets in Step 12)

- **Commit:** `docs: add Auth0 configuration guide with tenant details`

### ☐ Step 4: Simplify ApplicationUser
**Files to modify:** 1 file
- [ ] Remove `: IdentityUser` base class
- [ ] Add `Auth0UserId` and `Auth0LastSync` properties
- **Verify:** Project compiles
- **Commit:** `refactor: simplify ApplicationUser entity for Auth0`

### ☐ Step 5: Remove Local Auth Services
**Files to DELETE:** 5 files
- [ ] LocalAuthenticationProvider.cs
- [ ] IAuthenticationProvider.cs
- [ ] PasswordChangeResult.cs
- [ ] AdminAuthenticationSettings.cs
- [ ] IdentityCosmosDbConfiguration.md

**Files to modify:** 3 files
- [ ] UserManagementService (remove UserManager/IAuthenticationProvider)
- [ ] IUserManagementService (remove password param)
- [ ] RegisterAdminExtensions (remove auth middleware, keep policies)

- **Verify:** Compiles with errors (expected)
- **Update Tests:** Remove password tests, mock DB access
- **Commit:** `refactor: remove local authentication services`

### ☐ Step 6: Remove Auth Tests & Endpoints
**Files to DELETE:** 3 test files
- [ ] LocalAuthenticationProviderTests.cs
- [ ] FileSystemAuthenticationProviderTests.cs
- [ ] AuthenticationIntegrationTests.cs

**Files to modify:** 3 files
- [ ] Remove /admin/api/login and /logout endpoints
- [ ] Update UserManagementFacadeTests (mark Auth0 tests as Skip)
- [ ] Update UserManagementServiceTests (keep CRUD tests)

- **Verify:** Tests compile
- **Commit:** `test: remove obsolete authentication tests and endpoints`

### ☐ Step 7: Stub for Compilation
**Files to modify:** 3 files
- [ ] AdminAuthenticationStateProvider → return anonymous
- [ ] UserManagementFacade → stub password ops
- [ ] Login.razor → show "maintenance" message

- **Verify:** Project compiles and runs (auth non-functional)
- **Test:** Stub tests pass
- **Commit:** `refactor: stub authentication services for compilation`

**✅ PHASE 1 COMPLETE - Compiles, auth disabled**

### ☐ Step 8: Generic Auth Abstraction (Optional)
**If implementing pluggable auth:**
- [ ] Create IExternalAuthProvider interface
- [ ] Create Auth0Provider implementation
- [ ] Configure DI to use interface
- **Verify:** Compiles
- **Test:** Mock IExternalAuthProvider
- **Commit:** `feat: add pluggable auth provider abstraction`

---

## PHASE 2: Auth0 Integration (Steps 9-15)

### ☐ Step 9: Auth0 AuthenticationStateProvider
**Files to modify:** 1 file
- [ ] AdminAuthenticationStateProvider
  - Read from HttpContext
  - Validate Auth0 claims
  - Map to local user (email lookup)
  - Return anonymous if user not found/inactive

- **Test:** Mock HttpContext, test claim mapping, user lookup
- **Commit:** `feat: implement Auth0 authentication state provider`

### ☐ Step 10: Auth0 Endpoints
**Files to modify:** 1 file
- [ ] Add /admin/auth/login (redirect to Auth0)
- [ ] Add /admin/auth/callback (handle Auth0 response)
- [ ] Add /admin/auth/logout (sign out)

- **Test:** Mock Auth0 responses, test routing
- **Commit:** `feat: add Auth0 authentication endpoints`

### ☐ Step 11: Auth0 User Sync Service
**Files to CREATE:** 2-3 files
- [ ] IAuth0UserSyncService (or use IExternalAuthProvider)
- [ ] Auth0UserSyncService implementation
- [ ] Auth0Settings configuration class

**Files to modify:** 2 files
- [ ] UserManagementService (add sync calls)
- [ ] InitializeViblogAdminAsync (create admin in Auth0)

- **Test:** Mock sync service, test create/update/delete, test admin creation
- **Commit:** `feat: implement Auth0 user synchronization service`

### ☐ Step 12: Configure OpenID Connect
**Files to modify:** 2 files + config files
- [ ] RegisterAdminExtensions (add Auth0 middleware)
- [ ] Add NuGet packages:
  - Auth0.ManagementApi (7.26.2)
  - Microsoft.AspNetCore.Authentication.OpenIdConnect (10.0.0)

**Add to appsettings.Development.json:**
```json
{
  "Auth0": {
    "Domain": "{from-step-3}",
    "ClientId": "{from-step-3}",
    "Audience": "https://{tenant}.auth0.com/api/v2/",
    "ManagementApi": {
      "ClientId": "{from-step-3}"
    }
  }
}
```

**Add to User Secrets (right-click Viblog → Manage User Secrets):**
```json
{
  "Auth0:ClientSecret": "{from-step-3}",
  "Auth0:ManagementApi:ClientSecret": "{from-step-3}"
}
```

- **Verify:** Compiles and runs, redirects to Auth0
- **Test:** Mock auth pipeline
- **Commit:** `feat: configure Auth0 OpenID Connect middleware`

### ☐ Step 13: Update Login Page
**Files to modify:** 1 file
- [ ] Login.razor → Auth0 redirect button

- **Manual Test:** 
  - Navigate to /admin/login
  - Click button → Auth0 Universal Login
  - Login → redirected to /admin
  
- **Test:** Component test for error display
- **Commit:** `feat: update login page for Auth0`

### ☐ Step 14: Update User Management UI
**Files to modify:** 3 files
- [ ] UserManagementFacade (remove password, add sync)
- [ ] Users.razor (remove password field, add reset button)
- [ ] UserEdit.razor (remove password section)

- **Manual Test:**
  - Create user → verify in Auth0
  - Send password reset
  - Deactivate user → verify blocked
  - Delete user → verify removed
  
- **Test:** Mock sync service, test facade methods
- **Commit:** `feat: integrate Auth0 into user management UI`

**✅ PHASE 2 COMPLETE - Auth0 working**

### ☐ Step 15: Final Tests & Documentation
- [ ] Write all Auth0 tests
- [ ] Remove all [Skip] attributes
- [ ] Generate code coverage report
- [ ] Complete Auth0Configuration.md
- [ ] Update README.md with Auth0 setup
- [ ] Complete manual testing checklist

- **Commit:** `test: finalize Auth0 integration tests and documentation`

**✅ MIGRATION COMPLETE**

---

## Quick Reference

**Auth0 Dashboard:** https://manage.auth0.com  
**Full Plan:** `Viblog/Docs/Auth0MigrationPlan.md`  
**Your Config:** `Viblog/Docs/Auth0Configuration.md`

**Commit Pattern:**
- Step 1: `docs:`
- Steps 2, 4-7: `refactor:`
- Step 3: `docs:`
- Step 6: `test:`
- Steps 8-14: `feat:`
- Step 15: `test:`

**Total Steps:** 15 (8 in Phase 1, 7 in Phase 2)  
**Estimated Time:** 20-34 hours

---

**TIPS:**
- ✅ Commit after EVERY step
- ✅ Test compilation after each step
- ✅ Don't skip Step 3 (Auth0 setup)
- ✅ Save secrets to User Secrets (never commit)
- ✅ Write tests as you go (Step 5+)
- ✅ Manual test after Step 13
