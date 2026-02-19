# ApplicationUser Migration to Infrastructure

## Summary
Moved `ApplicationUser` from the data layer to the Infrastructure layer to follow proper architectural separation of concerns.

## Changes Made

### ✅ Created New Location
**`Viblog.Infrastructure\Shared\Data\Entities\ApplicationUser.cs`**
- Now located alongside other domain entities (`User`, `BlogPost`, `Page`, etc.)
- Extends `IdentityUser` with blog-specific properties
- Maintains proper layering: Domain entities in Infrastructure, not in data projects

### ✅ Removed Old Location
**`Viblog.Data.CosmosDb\Data\ApplicationUser.cs`** (deleted)

### ✅ Added Package Reference
**`Viblog.Infrastructure\Viblog.Infrastructure.csproj`**
- Added `Microsoft.Extensions.Identity.Stores` v10.0.3 for `IdentityUser` base class

### ✅ No Breaking Changes
- `ApplicationDbContext` already had the correct using statement
- All references continue to work via `using Viblog.Infrastructure.Shared.Data.Entities;`

## Why This Matters

### Before (❌ Incorrect)
```
Viblog.Data.CosmosDb
└── Data
    └── ApplicationUser.cs  ❌ Data layer
```
**Problem**: Services would need to reference data layer just to get the user entity type

### After (✅ Correct)
```
Viblog.Infrastructure
└── Shared
    └── Data
        └── Entities
            ├── ApplicationUser.cs  ✅ Domain layer
            ├── User.cs
            ├── BlogPost.cs
            ├── Page.cs
            └── ...
```
**Benefit**: Proper separation - domain entities in Infrastructure, implementations in Data projects

## Architectural Benefits

1. **Consistency** - All entities now in `Viblog.Infrastructure\Shared\Data\Entities`
2. **Separation of Concerns** - Domain entities separate from data access concerns
3. **Dependency Direction** - Services reference Infrastructure, not data layers
4. **Testability** - Can mock/test without data layer dependencies
5. **Flexibility** - Can swap data providers without changing domain model

## ApplicationUser Properties

The consolidated `ApplicationUser` now includes:

### From Identity Framework
- `Email` / `NormalizedEmail`
- `PasswordHash`
- `UserName`
- `EmailConfirmed`
- `PhoneNumber`
- `TwoFactorEnabled`
- `LockoutEnabled`
- etc.

### Custom Blog Properties
- `DisplayName` - For blog author attribution
- `CustomClaims` - Additional claims beyond Identity
- `IsActive` - Account status
- `LastLoginAt` - Login tracking
- `GroupKey` - CosmosDB partition key
- `CreatedAt` / `UpdatedAt` / `DeletedAt` - Audit timestamps
- `IsDeleted` - Soft delete support

## Next Steps

This sets the foundation to:
1. ✅ Remove the redundant `User` entity (completed)
2. ✅ Update `IUserManagementService` to use `ApplicationUser`
3. ✅ Consolidate all user management to Identity framework
4. ✅ Clean up duplicate user repositories

---

**Status**: ✅ Complete - Build successful  
**Impact**: Zero breaking changes - all existing code continues to work
