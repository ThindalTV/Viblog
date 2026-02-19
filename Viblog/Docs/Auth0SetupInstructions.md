# Auth0 Tenant Setup Instructions

**Step 3 of Auth0 Migration Plan**  
**Status:** Not Started  
**Estimated Time:** 30-45 minutes  
**Type:** Non-Code (Configuration Only)

---

## ✅ **Completion Checklist**

Track your progress:

- [ ] Auth0 account accessed
- [ ] Tenant created
- [ ] Regular Web Application created
- [ ] Application URLs configured
- [ ] Credentials documented
- [ ] Database connection configured
- [ ] Management API authorized
- [ ] Auth0Configuration.md created
- [ ] Ready for Step 4

---

## 📋 **Prerequisites**

Before starting:
- [ ] Auth0 account created at https://auth0.com (you already have this)
- [ ] Access to https://manage.auth0.com
- [ ] Notepad or text editor ready for copying credentials
- [ ] 30-45 minutes of uninterrupted time

---

## 🚀 **Step-by-Step Instructions**

### **Task 1: Login to Auth0 Dashboard**

1. [ ] Navigate to: **https://manage.auth0.com**
2. [ ] Login with your Auth0 credentials
3. [ ] Verify you're on the Auth0 Dashboard homepage

---

### **Task 2: Create Development Tenant**

**What is a tenant?** A tenant is an isolated Auth0 environment. You should have separate tenants for dev/staging/production.

1. [ ] Click your **profile icon** (top right corner)
2. [ ] Select **"Create Tenant"** from the dropdown
3. [ ] Fill in tenant details:
   - **Tenant Domain:** `viblog-dev` (or your preferred name)
     - This becomes: `viblog-dev.auth0.com`
     - ⚠️ **Cannot be changed later!**
   - **Region:** Select closest to you
     - [ ] US (if in North America)
     - [ ] EU (if in Europe)
     - [ ] AU (if in Australia/Asia)
   - **Environment Tag:** Select **"Development"**
4. [ ] Click **"Create"**
5. [ ] Wait for tenant to be provisioned (15-30 seconds)

**✍️ Write down your tenant domain:**
```
Tenant Domain: _________________________.auth0.com
```

---

### **Task 3: Create Regular Web Application**

Auth0 uses "Applications" to represent your apps.

1. [ ] From the left sidebar, navigate to: **Applications → Applications**
2. [ ] Click **"Create Application"** button (top right)
3. [ ] Configure the application:
   - **Name:** `Viblog Admin`
   - **Application Type:** Select **"Regular Web Applications"**
   - **Technology:** Select **"ASP.NET Core"** (optional, just for code samples)
4. [ ] Click **"Create"**
5. [ ] You'll be redirected to the application's **Settings** tab

**✅ Application created!**

---

### **Task 4: Configure Application URLs**

**CRITICAL:** These URLs must match exactly or authentication will fail.

Still on the **Settings** tab:

1. [ ] Scroll down to **"Application URIs"** section
2. [ ] Find **"Allowed Callback URLs"** field
3. [ ] Paste the following (one per line or comma-separated):
   ```
   https://localhost:7001/admin/auth/callback,
   http://localhost:5000/admin/auth/callback
   ```
4. [ ] Find **"Allowed Logout URLs"** field
5. [ ] Paste the following:
   ```
   https://localhost:7001/admin/login,
   http://localhost:5000/admin/login
   ```
6. [ ] Find **"Allowed Web Origins"** field
7. [ ] Paste the following:
   ```
   https://localhost:7001,
   http://localhost:5000
   ```
8. [ ] Scroll to bottom and click **"Save Changes"**

**✅ URLs configured!**

---

### **Task 5: Copy Application Credentials**

**⚠️ IMPORTANT:** Keep these secret! Never commit to Git!

Still on the **Settings** tab, scroll to the **"Basic Information"** section at the top:

1. [ ] Find **"Domain"**
   - Copy the value (e.g., `viblog-dev.auth0.com`)
   
   **✍️ Write it here:**
   ```
   Domain: _________________________.auth0.com
   ```

2. [ ] Find **"Client ID"**
   - Copy the long alphanumeric string
   
   **✍️ Write it here (first 8 characters only for security):**
   ```
   Client ID: ________...
   ```

3. [ ] Find **"Client Secret"**
   - Click **"Show"** button
   - Copy the secret
   
   **✍️ Write it here (first 8 characters only for security):**
   ```
   Client Secret: ________...
   ```

**✅ Credentials documented!**

---

### **Task 6: Configure Database Connection**

Auth0 uses "Connections" for authentication methods. We'll use the default Username-Password database.

1. [ ] From left sidebar, navigate to: **Authentication → Database**
2. [ ] Find **"Username-Password-Authentication"** (default connection)
3. [ ] Click on the connection name to edit

#### **6.1: Disable Public Signups**

1. [ ] Click the **"Settings"** tab (if not already there)
2. [ ] Find **"Disable Sign Ups"** toggle
3. [ ] **Turn it ON** ✅ (toggle should be blue/green)
   - This prevents public registration
   - Users can ONLY be created via admin panel

#### **6.2: Configure Password Policy**

1. [ ] Click the **"Password Policy"** tab
2. [ ] Select password strength:
   - [ ] **"Good"** (recommended for development)
   - [ ] **"Excellent"** (recommended for production)
3. [ ] Verify **"Minimum Length"** is at least 8 characters

#### **6.3: Link to Application**

1. [ ] Click the **"Applications"** tab
2. [ ] Verify **"Viblog Admin"** is enabled (toggle is ON)
3. [ ] If not, toggle it ON

4. [ ] Scroll to bottom and click **"Save"**

**✅ Database connection configured!**

---

### **Task 7: Configure Management API Access**

The Management API allows your app to programmatically create/manage users.

1. [ ] From left sidebar, navigate to: **Applications → APIs**
2. [ ] Find **"Auth0 Management API"** (system API)
3. [ ] Click on it
4. [ ] Click the **"Machine to Machine Applications"** tab

#### **7.1: Authorize Your Application**

1. [ ] Find **"Viblog Admin"** in the list
2. [ ] **Toggle the switch to "Authorized"** (should turn blue/green)
3. [ ] Click the **dropdown arrow** next to "Viblog Admin" to expand

#### **7.2: Grant Required Scopes**

Scroll through the permissions list and **check the following boxes**:

**User Management Scopes:**
- [ ] ✅ `create:users` - Create new users
- [ ] ✅ `read:users` - Read user information
- [ ] ✅ `read:users_app_metadata` - Read user metadata
- [ ] ✅ `update:users` - Update user information
- [ ] ✅ `update:users_app_metadata` - Update user metadata
- [ ] ✅ `delete:users` - Delete users

**Password Reset Scope:**
- [ ] ✅ `create:user_tickets` - Generate password reset emails

**Total scopes:** 7 checked

4. [ ] Click **"Update"** button at the bottom
5. [ ] Verify the scopes are saved (count should show "7" next to Viblog Admin)

**✅ Management API configured!**

**📝 Note:** The Client ID and Secret for the Management API are the **same** as your application credentials (from Task 5).

---

### **Task 8: Optional - Customize Login Page**

This step is optional but makes the login experience nicer.

1. [ ] From left sidebar, navigate to: **Branding → Universal Login**
2. [ ] Verify **"New Universal Login Experience"** is enabled
3. [ ] (Optional) Click **"Customize Login Page"** to:
   - Upload your logo
   - Change colors to match your brand
   - Preview the login page
4. [ ] Click **"Save"** if you made changes

**✅ Branding configured (optional)!**

---

### **Task 9: Create Auth0Configuration.md**

Now document your specific configuration for the codebase.

1. [ ] Open the template: `Viblog/Docs/Auth0Configuration.TEMPLATE.md`
2. [ ] Save a copy as: `Viblog/Docs/Auth0Configuration.md`
3. [ ] Replace all `{placeholders}` with your actual values:

   **From Task 2 (Tenant):**
   - Replace `{your-tenant-name}` with your tenant domain prefix (e.g., `viblog-dev`)
   - Replace `{your-region}` with your region (US, EU, AU)

   **From Task 5 (Credentials):**
   - Replace `{from-step-3}` with your Client ID
   - Replace `{from-step-5}` with your Client ID again (same value)

4. [ ] **DO NOT** paste secrets in this file
   - Leave `{actual-secret-from-step-3}` as placeholder
   - Secrets go in User Secrets in Step 12

5. [ ] Save the file

**✅ Configuration documented!**

---

### **Task 10: Create Test User (Optional)**

Create a test user to verify login works later.

1. [ ] From left sidebar, navigate to: **User Management → Users**
2. [ ] Click **"Create User"** button
3. [ ] Fill in:
   - **Email:** Your email address (or `test@viblog.local`)
   - **Password:** Leave blank (will use password reset)
   - **Connection:** Select `Username-Password-Authentication`
4. [ ] Click **"Create"**
5. [ ] User will receive a verification email
6. [ ] Click the link in the email to set password

**✍️ Test user email:**
```
Test User Email: _________________________
```

**✅ Test user created!**

---

## 🎉 **Step 3 Complete!**

### **Final Verification Checklist**

Before moving to Step 4, verify:

- [ ] ✅ Auth0 tenant created and accessible
- [ ] ✅ "Viblog Admin" application exists
- [ ] ✅ Callback URLs configured (localhost:7001 and localhost:5000)
- [ ] ✅ Logout URLs configured
- [ ] ✅ Credentials documented (Domain, Client ID, Client Secret)
- [ ] ✅ Database signup disabled
- [ ] ✅ Password policy set to "Good" or "Excellent"
- [ ] ✅ Management API authorized with 7 scopes
- [ ] ✅ `Auth0Configuration.md` created with your values
- [ ] ✅ (Optional) Test user created

---

## 📊 **What You Created**

Your Auth0 tenant now has:

```
viblog-dev.auth0.com  (your tenant)
│
├── Applications
│   └── Viblog Admin (Regular Web Application)
│       ├── Client ID: {your-client-id}
│       ├── Client Secret: {your-secret}
│       ├── Callbacks: localhost:7001, localhost:5000
│       └── Management API Access (7 scopes)
│
├── Connections
│   └── Username-Password-Authentication
│       ├── Signups: Disabled ✅
│       ├── Password Policy: Good/Excellent
│       └── Linked to: Viblog Admin
│
└── Users (optional)
    └── test@viblog.local (test user)
```

---

## 🎯 **Next Steps**

**Commit your work:**

```powershell
git add Viblog/Docs/Auth0Configuration.md
git commit -m "docs: add Auth0 tenant configuration for development"
```

**Then proceed to Step 4:**
- Open the Quick Start Checklist: `Viblog/Docs/Auth0QuickStartChecklist.md`
- Mark Step 3 as complete ✅
- Begin Step 4: Simplify AdminUser Entity (code changes)

---

## 🆘 **Troubleshooting**

### **Can't find "Create Tenant" option?**
- You may already be in a tenant. Check the top-left corner for tenant name.
- Click tenant name dropdown → "Create Tenant"

### **"Disable Sign Ups" toggle is greyed out?**
- You need to be on a paid plan to disable signups on the default connection
- **Workaround:** This will be enforced by your app (users created via admin only)

### **Management API scopes not saving?**
- Make sure you clicked "Update" button
- Refresh the page and verify scopes are checked

### **Lost my Client Secret?**
- Go to Applications → Viblog Admin → Settings
- Scroll to Client Secret → Click "Show"
- Copy it again (you can view it anytime)

---

**Last Updated:** 2025-02-08  
**Version:** 1.0  
**Related Documents:**
- Auth0 Migration Plan: `Auth0MigrationPlan.md`
- Quick Start Checklist: `Auth0QuickStartChecklist.md`
- Configuration Template: `Auth0Configuration.TEMPLATE.md`
- Your Configuration: `Auth0Configuration.md` (create in Task 9)
