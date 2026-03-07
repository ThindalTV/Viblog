# Plan: Rename `/admin` URL Prefix to `/viblog`

**Goal:** Change all admin-section URLs from `/admin/...` to `/viblog/...` without moving any files on disk.  
**Scope:** Blazor `@page` directives, in-code navigation, backend auth endpoints, Auth0 configuration.

---

## Pre-flight Checklist

- [ ] Confirm Auth0 dashboard credentials are accessible (needed for Phase 4)
- [ ] Note any deployed environment callback URLs that must be updated in Auth0

---

## Phase 1 — `@page` Directives

All files are under `Viblog/Admin/Pages/`. Change every `@page "/admin` to `@page "/viblog`.

| File | Routes to change |
|---|---|
| `Index.razor` | `/admin` → `/viblog` |
| `Login.razor` | `/admin/login` → `/viblog/login` |
| `Posts.razor` | `/admin/posts` → `/viblog/posts` |
| `PostEdit.razor` | `/admin/posts/new` → `/viblog/posts/new` · `/admin/posts/edit/{id}` → `/viblog/posts/edit/{id}` |
| `Pages.razor` | `/admin/pages` → `/viblog/pages` |
| `PageEdit.razor` | `/admin/pages/new` → `/viblog/pages/new` · `/admin/pages/edit/{id}` → `/viblog/pages/edit/{id}` |
| `Users.razor` | `/admin/users` → `/viblog/users` |
| `UserEdit.razor` | `/admin/users/new` → `/viblog/users/new` · `/admin/users/edit/{userId}` → `/viblog/users/edit/{userId}` |
| `MediaLibrary.razor` | `/admin/media` → `/viblog/media` |
| `MediaPickerExample.razor` | `/admin/media-picker-example` → `/viblog/media-picker-example` |
| `Analytics.razor` | `/admin/analytics` → `/viblog/analytics` |
| `AuditLogs.razor` | `/admin/audit-logs` → `/viblog/audit-logs` |
| `Settings.razor` | `/admin/settings` → `/viblog/settings` |
| `Profile.razor` | `/admin/profile` → `/viblog/profile` |

---

## Phase 2 — In-Code Navigation & HTML Links

### `Viblog/Admin/AdminRoutes.razor`

| Location | Old value | New value |
|---|---|---|
| `NotAuthorized` redirect | `"/admin/login?ReturnUrl={returnUrl}"` | `"/viblog/login?ReturnUrl={returnUrl}"` |
| `NotFound` anchor | `href="/admin"` | `href="/viblog"` |

---

### `Viblog/Admin/Layout/AdminLayout.razor`

| Location | Old value | New value |
|---|---|---|
| `_navigationItems` — Dashboard | `Url = "/admin"` | `Url = "/viblog"` |
| `_navigationItems` — Posts | `Url = "/admin/posts"` | `Url = "/viblog/posts"` |
| `_navigationItems` — Pages | `Url = "/admin/pages"` | `Url = "/viblog/pages"` |
| `_navigationItems` — Media | `Url = "/admin/media"` | `Url = "/viblog/media"` |
| `_navigationItems` — Users | `Url = "/admin/users"` | `Url = "/viblog/users"` |
| `_navigationItems` — Profile | `Url = "/admin/profile"` | `Url = "/viblog/profile"` |
| `_navigationItems` — Audit Logs | `Url = "/admin/audit-logs"` | `Url = "/viblog/audit-logs"` |
| `_navigationItems` — Analytics | `Url = "/admin/analytics"` | `Url = "/viblog/analytics"` |
| `_navigationItems` — Settings | `Url = "/admin/settings"` | `Url = "/viblog/settings"` |
| Logout `MenuItem` | `Url = "/admin/logout"` | `Url = "/viblog/logout"` |
| Breadcrumb anchor | `href="/admin"` | `href="/viblog"` |
| `IsSelected()` exact check | `item.Url == "/admin/media"` | `item.Url == "/viblog/media"` |
| `IsSelected()` prefix check | `currentPath.StartsWith("admin/media"...)` | `currentPath.StartsWith("viblog/media"...)` |
| `HandleLogout()` | `NavigateTo("/admin/logout", forceLoad: true)` | `NavigateTo("/viblog/logout", forceLoad: true)` |

---

### `Viblog/Admin/Pages/Login.razor`

| Location | Old value | New value |
|---|---|---|
| Already-authenticated redirect | `NavigateTo("/admin", true)` | `NavigateTo("/viblog", true)` |
| "Try Again" anchor | `href="/admin/auth/challenge"` | `href="/viblog/auth/challenge"` |
| `OnAfterRenderAsync` — no return-URL | `"/admin/auth/challenge"` | `"/viblog/auth/challenge"` |
| `OnAfterRenderAsync` — with return-URL | `$"/admin/auth/challenge?returnUrl=..."` | `$"/viblog/auth/challenge?returnUrl=..."` |

---

### `Viblog/Admin/Pages/Posts.razor`

| Location | Old value | New value |
|---|---|---|
| "New Post" button click | `NavigateTo("/admin/posts/new")` | `NavigateTo("/viblog/posts/new")` |
| `OnRowClick` | `NavigateTo($"/admin/posts/edit/{post.Id}")` | `NavigateTo($"/viblog/posts/edit/{post.Id}")` |

---

### `Viblog/Admin/Pages/PostEdit.razor`

| Location | Old value | New value |
|---|---|---|
| After create success | `Navigation.NavigateTo("/admin/posts")` | `Navigation.NavigateTo("/viblog/posts")` |
| `Cancel()` | `Navigation.NavigateTo("/admin/posts")` | `Navigation.NavigateTo("/viblog/posts")` |

---

### `Viblog/Admin/Pages/Pages.razor`

| Location | Old value | New value |
|---|---|---|
| "New Page" button click | `NavigateTo("/admin/pages/new")` | `NavigateTo("/viblog/pages/new")` |
| `OnRowClick` | `NavigateTo($"/admin/pages/edit/{item.Id}")` | `NavigateTo($"/viblog/pages/edit/{item.Id}")` |

---

### `Viblog/Admin/Pages/PageEdit.razor`

| Location | Old value | New value |
|---|---|---|
| After create success | `Navigation.NavigateTo($"/admin/pages/edit/{page.Id}")` | `Navigation.NavigateTo($"/viblog/pages/edit/{page.Id}")` |
| `Cancel()` | `Navigation.NavigateTo("/admin/pages")` | `Navigation.NavigateTo("/viblog/pages")` |

---

### `Viblog/Admin/Pages/Users.razor`

| Location | Old value | New value |
|---|---|---|
| "New User" button click | `NavigateTo("/admin/users/new")` | `NavigateTo("/viblog/users/new")` |
| Edit button click | `NavigateTo($"/admin/users/edit/{user!.Id}")` | `NavigateTo($"/viblog/users/edit/{user!.Id}")` |

---

### `Viblog/Admin/Pages/UserEdit.razor`

| Location | Old value | New value |
|---|---|---|
| After save success | `NavigateTo("/admin/users")` | `NavigateTo("/viblog/users")` |
| `OnCancel()` | `NavigateTo("/admin/users")` | `NavigateTo("/viblog/users")` |

---

## Phase 3 — Backend Auth Endpoints & Cookie Options

### `Viblog/Admin/RegisterAdminExtensions.cs`

| Location | Old value | New value |
|---|---|---|
| Cookie `LoginPath` | `"/admin/login"` | `"/viblog/login"` |
| Cookie `AccessDeniedPath` | `"/admin/access-denied"` | `"/viblog/access-denied"` |
| OIDC `SignedOutCallbackPath` | `"/admin/signout-callback"` | `"/viblog/signout-callback"` |
| Dev-mode cookie `LoginPath` | `"/admin/login"` | `"/viblog/login"` |
| Dev-mode cookie `AccessDeniedPath` | `"/admin/access-denied"` | `"/viblog/access-denied"` |
| Challenge endpoint route | `MapGet("/admin/auth/challenge", ...)` | `MapGet("/viblog/auth/challenge", ...)` |
| Challenge `RedirectUri` fallback | `returnUrl ?? "/admin"` | `returnUrl ?? "/viblog"` |
| Logout endpoint route | `MapGet("/admin/logout", ...)` | `MapGet("/viblog/logout", ...)` |
| Access-denied endpoint route | `MapGet("/admin/access-denied", ...)` | `MapGet("/viblog/access-denied", ...)` |
| Access-denied redirect | `Redirect("/admin/login?error=access_denied")` | `Redirect("/viblog/login?error=access_denied")` |

### `Viblog/Admin/Configuration/Auth0Settings.cs`

| Property | Old default | New default |
|---|---|---|
| `CallbackPath` | `"/admin/auth/callback"` | `"/viblog/auth/callback"` |
| `LogoutRedirectUri` | `"/admin/login"` | `"/viblog/login"` |

---

## Phase 4 — Configuration & External Services

> ⚠️ **Phase 4 must be deployed at the same time as Phase 3.** Deploying Phase 3 first will break the Auth0 callback and lock all users out.

### `appsettings.json` / `appsettings.Development.json` / User Secrets

Check for explicit overrides of these keys and update if present:

```
Auth0:CallbackPath          /admin/auth/callback  →  /viblog/auth/callback
Auth0:LogoutRedirectUri     /admin/login          →  /viblog/login
```

### Auth0 Dashboard (manual step)

Navigate to: **Applications → Viblog Admin → Settings**

| Field | Remove | Add |
|---|---|---|
| Allowed Callback URLs | `{host}/admin/auth/callback` | `{host}/viblog/auth/callback` |
| Allowed Logout URLs | `{host}/admin/login` | `{host}/viblog/login` |

> Update for every environment (localhost, staging, production).  
> Auth0 will reject any callback URL that is not explicitly listed.

### `Viblog/Docs/Auth0Configuration.TEMPLATE.md`

Update all example URLs:

| Old | New |
|---|---|
| `/admin/auth/callback` | `/viblog/auth/callback` |
| `/admin/login` | `/viblog/login` |

---

## Phase 5 — Verification

- [ ] Build the solution — no compilation errors
- [ ] Browse to `/viblog` — redirects to `/viblog/login` when unauthenticated
- [ ] Complete Auth0 login — lands on `/viblog` dashboard
- [ ] Verify all sidebar nav links route correctly
- [ ] Verify breadcrumb "Home" links to `/viblog`
- [ ] Test logout — redirects to `/` (public home)
- [ ] Confirm `/admin` (old URL) returns a 404
- [ ] Verify OIDC callback URL is accepted by Auth0 (no "Callback URL mismatch" error)

---

## Execution Order

```
Phase 1  →  Phase 2  →  Phase 3 + Phase 4 (simultaneous deploy)  →  Phase 5
```

Phases 1 and 2 only affect client-side routing and are safe to commit independently.  
Phase 3 must ship **together with** the Auth0 dashboard changes in Phase 4.

---

## Files Touched Summary

| File | Phase |
|---|---|
| `Admin/Pages/Index.razor` | 1 |
| `Admin/Pages/Login.razor` | 1, 2 |
| `Admin/Pages/Posts.razor` | 1, 2 |
| `Admin/Pages/PostEdit.razor` | 1, 2 |
| `Admin/Pages/Pages.razor` | 1, 2 |
| `Admin/Pages/PageEdit.razor` | 1, 2 |
| `Admin/Pages/Users.razor` | 1, 2 |
| `Admin/Pages/UserEdit.razor` | 1, 2 |
| `Admin/Pages/MediaLibrary.razor` | 1 |
| `Admin/Pages/MediaPickerExample.razor` | 1 |
| `Admin/Pages/Analytics.razor` | 1 |
| `Admin/Pages/AuditLogs.razor` | 1 |
| `Admin/Pages/Settings.razor` | 1 |
| `Admin/Pages/Profile.razor` | 1 |
| `Admin/AdminRoutes.razor` | 2 |
| `Admin/Layout/AdminLayout.razor` | 2 |
| `Admin/RegisterAdminExtensions.cs` | 3 |
| `Admin/Configuration/Auth0Settings.cs` | 3 |
| `appsettings.json` / `appsettings.Development.json` | 4 |
| Auth0 Dashboard (external) | 4 |
| `Docs/Auth0Configuration.TEMPLATE.md` | 4 |
