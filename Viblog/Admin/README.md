# Viblog Authentication Architecture

## Overview

Viblog uses a dual-architecture approach:
- **Public Frontend**: Statically rendered, no authentication required
- **Admin Area**: Server-interactive with authentication required

## Authentication Setup

### Admin Authentication
The admin area uses a custom authentication system located in:
- `Admin/Services/AdminAuthenticationStateProvider.cs` - Custom auth provider
- `Admin/Configuration/AdminAuthenticationSettings.cs` - Hardcoded credentials (temporary)
- `Admin/Pages/Login.razor` - Admin login page

**Current Credentials** (will be replaced with external service):
- Email: `eric@ericjohansson.se`
- Password: `admin123!`

### Identity Components
The `Components/Account` folder contains ASP.NET Core Identity scaffolding:
- These components are **registered globally** but **only used in admin**
- They are kept for potential future use with external authentication
- The public frontend never accesses these components

## Admin-Only Resources

### Components
- `Admin/Layout/AdminLayout.razor` - Admin layout with drawer navigation
- `Admin/Pages/Login.razor` - Login page
- `Admin/Pages/Index.razor` - Dashboard
- `Components/Layout/ReconnectModal.razor` - Server reconnection modal (admin-only)

### Styles
- `wwwroot/admin.scss` - All admin-specific styles
- `Admin/**/*.razor.scss` - Component-scoped admin styles
- Telerik UI CSS - Loaded only in admin layout

### Scripts
- Telerik Blazor JS - Loaded automatically by TelerikRootComponent
- PasskeySubmit.razor.js - Not loaded (kept for future use)

## Public Frontend Resources

### Styles
- `wwwroot/blog.scss` - Public blog styles
- `wwwroot/app.scss` - Global application styles
- `Frontend/**/*.razor.scss` - Component-scoped frontend styles

### Features
- Statically rendered pages (no server interaction)
- No authentication required
- Optimized for performance and SEO

## Future Enhancements

1. **External Authentication Service**: Replace hardcoded credentials with Azure AD, Auth0, or similar
2. **Role-Based Access**: Implement granular permissions for different admin users
3. **Audit Logging**: Track admin actions for security and compliance
4. **Two-Factor Authentication**: Add MFA for enhanced security

## Development Notes

- Admin routes are prefixed with `/admin`
- Public routes have no prefix
- Authentication state is only checked in admin routes
- The `AuthorizeView` component is only used in `AdminLayout.razor`
