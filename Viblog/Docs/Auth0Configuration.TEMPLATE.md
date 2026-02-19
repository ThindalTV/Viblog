# Auth0 Configuration - TEMPLATE

**Instructions:** Complete this file during Step 3 of the Auth0 migration. Replace all `{placeholders}` with your actual values.

**⚠️ IMPORTANT:** DO NOT commit secrets (Client Secrets) to Git. Store secrets in User Secrets only.

---

## Tenant Information

- **Domain:** `{your-tenant-name}.auth0.com`
- **Region:** `{e.g., US, EU, AU}`
- **Environment:** Development
- **Created:** {date}

---

## Application: Viblog Admin

### Basic Information
- **Name:** Viblog Admin
- **Type:** Regular Web Application
- **Client ID:** `{copy-from-auth0-dashboard}`
- **Client Secret:** *(stored in User Secrets - DO NOT WRITE HERE)*

### Allowed URLs
**Callback URLs:**
```
https://localhost:7001/admin/auth/callback
http://localhost:5000/admin/auth/callback
```

**Logout URLs:**
```
https://localhost:7001/admin/login
http://localhost:5000/admin/login
```

**Web Origins:**
```
https://localhost:7001
http://localhost:5000
```

### Production URLs (Add Later)
```
https://yourdomain.com/admin/auth/callback
https://yourdomain.com/admin/login
https://yourdomain.com
```

---

## Management API Authorization

### Application Used
- **Application:** Viblog Admin (same app)
- **API:** Auth0 Management API
- **Client ID:** `{same-as-above}` (if using Option A)
- **Client Secret:** *(same as above, in User Secrets)*

### Granted Scopes
- ✅ `create:users`
- ✅ `read:users`
- ✅ `read:users_app_metadata`
- ✅ `update:users`
- ✅ `update:users_app_metadata`
- ✅ `delete:users`
- ✅ `create:user_tickets`

---

## Database Connection

- **Connection Name:** Username-Password-Authentication
- **Type:** Database (Auth0-hosted)
- **Signups:** Disabled ✅
- **Password Policy:** Excellent
- **Minimum Password Length:** 8 characters
- **Associated Apps:** Viblog Admin

---

## Configuration Files

### appsettings.Development.json
```json
{
  "Auth0": {
    "Domain": "{your-tenant-name}.auth0.com",
    "ClientId": "{client-id-from-dashboard}",
    "Audience": "https://{your-tenant-name}.auth0.com/api/v2/",
    "ManagementApi": {
      "ClientId": "{same-as-above-or-separate-if-option-B}"
    }
  }
}
```

### User Secrets (Manage User Secrets in Visual Studio)
```json
{
  "Auth0:ClientSecret": "{actual-client-secret-from-dashboard}",
  "Auth0:ManagementApi:ClientSecret": "{same-as-above-or-separate-if-option-B}"
}
```

**To set User Secrets:**
1. Right-click `Viblog` project in Solution Explorer
2. Select "Manage User Secrets"
3. Paste the JSON above with actual secrets
4. Save and close

---

## Testing Configuration

### Test User (Create in Auth0 Dashboard)
- **Email:** test@example.com (or your email)
- **Password:** {set-via-password-reset-email}
- **Created:** {date}
- **Purpose:** Manual testing of login flow

**To create test user:**
1. Navigate to User Management → Users in Auth0 Dashboard
2. Click "Create User"
3. Enter email
4. Connection: Username-Password-Authentication
5. ✅ Send verification email
6. User will receive password reset email to set password

---

## Branding (Optional)

- **Logo:** {uploaded | not-yet}
- **Primary Color:** {hex-code}
- **Background:** {default | custom}
- **Customized:** {yes | no}

---

## Troubleshooting

### Common Issues

**"Callback URL mismatch" error:**
- Verify callback URL in Auth0 matches exactly
- Check for trailing slashes
- Ensure both http and https variants are listed

**"Access denied" when creating users:**
- Verify Management API scopes are granted
- Check that Viblog Admin app is authorized for Management API

**"Invalid audience" error:**
- Ensure Audience in config matches: `https://{tenant}.auth0.com/api/v2/`

**Users can't login:**
- Verify user exists in Auth0 Users section
- Check that user is associated with Username-Password-Authentication
- Verify password was set (via password reset email)

---

## Links

- **Auth0 Dashboard:** https://manage.auth0.com
- **Your Tenant:** https://{your-tenant-name}.auth0.com
- **Documentation:** https://auth0.com/docs

---

**Completed:** {date}  
**Completed By:** {your-name}  
**Migration Step:** Step 3 of Auth0 Migration Plan
