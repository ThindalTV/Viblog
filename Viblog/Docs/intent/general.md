# Vilog: Blogging Platform Architecture

## Overview

**Vilog** is a modern, high-performance blogging platform built with ASP.NET Core Blazor and Azure CosmosDB. It features a dual-architecture design that separates public content (statically rendered for optimal performance) from administrative functionality (interactive server-side rendering).

## Technology Stack

### Core Technologies
- **.NET 10** with C# 14.0
- **ASP.NET Core Blazor** (Hybrid rendering)
  - Static rendering for public pages
  - Interactive Server rendering for admin
- **Azure CosmosDB** with Entity Framework Core
- **Azure Blob Storage** (for media files)
- **Telerik UI for Blazor** (admin components)

### Development Environment
- **Docker** for containerization
- **CosmosDB Emulator** for local development
- **xUnit** for testing with AutoMoq

## Architectural Patterns

### Display-Facade-Repository Pattern

Vilog implements a clean three-layer architecture:

1. **Display Layer** (Blazor Components)
   - Minimal logic, presentation focused
   - Razor components (.razor files)
   - Separate for Frontend and Admin

2. **Facade Layer** (Business Logic)
   - Coordinates operations between display and data layers
   - Provides view-specific data transformations
   - Handles complex query composition
   - Located in `Frontend/Facades` and `Admin/Facades`

3. **Repository Layer** (Data Access)
   - Generic repository pattern with `IRepository<TEntity>`
   - Specialized repositories for domain entities
   - Built-in paging and sorting support
   - Located in `Shared/Data/Repositories`

### Dual Architecture

The application is split into two distinct areas:

#### Frontend (Public Blog)
- **Location**: `Frontend/` folder
- **Rendering**: Static Server-Side Rendering (SSR)
- **Authentication**: None required
- **Purpose**: Public-facing blog content
- **Features**:
  - Blog post listing and detail pages
  - Category and tag browsing
  - Archive by date
  - Search functionality
  - RSS/Atom feeds
  - SEO-optimized with structured data

#### Admin (Content Management)
- **Location**: `Admin/` folder
- **Rendering**: Interactive Server
- **Authentication**: Cookie-based authentication
- **Purpose**: Content management interface
- **Features**:
  - Post creation and editing
  - Media management
  - Category and tag management
  - Analytics dashboard
  - Settings configuration

## Project Structure

```
Vilog/
??? Components/          # Root-level shared components
?   ??? App.razor       # Application entry point
?   ??? Routes.razor    # Main routing configuration
??? Frontend/           # Public blog components
?   ??? Pages/          # Public page components
?   ??? Components/     # Reusable frontend components
?   ??? Facades/        # Frontend business logic
?   ??? Infrastructure/ # Frontend interfaces
?   ??? Layout/         # Frontend layouts
?   ??? FrontendRoutes.razor
??? Admin/              # Admin area components
?   ??? Pages/          # Admin page components
?   ??? Components/     # Admin-specific components
?   ??? Facades/        # Admin business logic
?   ??? Infrastructure/ # Admin interfaces
?   ??? Layout/         # Admin layouts
?   ??? Services/       # Admin-specific services
?   ??? AdminRoutes.razor
??? Shared/             # Shared across frontend and admin
?   ??? Data/           # Data access layer
?   ?   ??? Entities/   # Domain entities
?   ?   ??? Repositories/ # Data repositories
?   ?   ??? Seeders/    # Database seeders
?   ??? Configuration/  # Configuration models
?   ??? Services/       # Shared business services
?   ??? Models/         # Shared data models
??? Api/                # API endpoints
?   ??? Endpoints/      # Minimal API endpoint definitions
??? wwwroot/            # Static assets
    ??? blog.scss       # Frontend styles
    ??? admin.scss      # Admin styles
    ??? img/            # Images and media
```

## Core Concepts

### Entity Model

All domain entities inherit from `BaseEntity`, which provides:
- **Id**: Unique identifier (GUID string)
- **PartitionKey**: CosmosDB partition key
- **CreatedAt**: Creation timestamp
- **UpdatedAt**: Last modification timestamp
- **IsDeleted**: Soft delete flag
- **DeletedAt**: Deletion timestamp

Primary entities:
- **BlogPost**: Core blog content with metadata
- **Comment**: User comments on posts
- **Category**: Post categorization
- **Tag**: Post tagging (embedded in BlogPost)

### Configuration System

Uses the IOptions pattern with strongly-typed configuration:

```csharp
services.AddVilogConfiguration(configuration);
```

Key configuration sections:
- **SiteMetadata**: SEO, social sharing, site identity
- **CosmosDbSettings**: Database configuration
- **AdminAuthenticationSettings**: Admin credentials (temporary)

### Data Access

#### Paging Support
All multi-entity queries use `PagingParameters`:
- `PageNumber`: Current page (1-based)
- `PageSize`: Items per page
- Returns: `PagedResult<T>` with items and metadata

#### Repository Operations
- `GetByIdAsync`: Single entity retrieval
- `GetAllAsync`: Paged entity listing
- `FindAsync`: Filtered, sorted, paged queries
- `AddAsync/UpdateAsync/DeleteAsync`: CRUD operations
- Soft delete support by default

### Service Registration

Modular registration through extension methods:

```csharp
// Shared services
builder.Services.AddVilogConfiguration(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddBlogServices();

// Frontend-specific
builder.Services.AddVilogFrontend();

// Admin-specific
builder.Services.AddVilogAdmin();
```

## Routing Strategy

### Frontend Routes
- `/` - Home page
- `/posts` - All posts listing
- `/posts/{slug}` - Individual post
- `/category/{slug}` - Posts by category
- `/tag/{tag}` - Posts by tag
- `/archive/{year}/{month?}` - Archive view
- `/search` - Search results

### Admin Routes
- `/admin` - Dashboard
- `/admin/login` - Login page
- `/admin/posts` - Post management
- `/admin/posts/edit/{id?}` - Post editor
- `/admin/pages` - Page management
- `/admin/settings` - Site settings
- `/admin/analytics` - Analytics dashboard

### API Endpoints
- `/feed/rss` - RSS feed
- `/feed/atom` - Atom feed
- `/sitemap.xml` - SEO sitemap
- `/robots.txt` - Robots exclusion

## Authentication & Authorization

### Current Implementation
- **Admin Authentication**: Cookie-based with custom `AdminAuthenticationStateProvider`
- **Hardcoded Credentials** (temporary):
  - Email: `eric@ericjohansson.se`
  - Password: `admin123!`
- **Session Duration**: 8 hours with sliding expiration

### Future Enhancements
- External authentication service (Azure AD, Auth0)
- Role-based access control
- Two-factor authentication
- API key management for webhooks

## Data Storage

### CosmosDB Configuration

#### Development (Emulator)
- **Connection Mode**: Gateway (required for localhost)
- **Endpoint Limiting**: Enabled
- **Certificate Validation**: Disabled (self-signed emulator cert)

#### Production
- **Connection Mode**: Direct (optimal performance)
- **Connection String**: Stored in Azure Key Vault
- **Database**: Auto-created on startup

### Database Initialization

On application startup:
1. `EnsureDatabaseCreatedAsync()` creates database and containers
2. `SeedDatabaseAsync()` populates sample data if empty

## Development Guidelines

### Code Conventions
- **Classes**: PascalCase
- **Methods**: PascalCase
- **Variables**: camelCase
- **Private fields**: _camelCase (underscore prefix)
- **Constants**: PascalCase

### Testing Requirements
- All business logic covered by unit tests
- Use `protected virtual` for methods requiring mocking
- xUnit with AutoMoq (no Fluent Assertions)
- Tests should verify outputs, not mock implementation details

### Error Handling
- Throw specific exceptions with context
- Log errors with Exceptionless
- Display user-friendly messages
- Include detailed error information for debugging

### Styling
- **SASS** for all styles (no CSS frameworks)
- **Component-scoped styles** using `.razor.scss`
- **Responsive design** (mobile-first)
- **No Bootstrap** or other CSS frameworks

## Performance Considerations

### Static Rendering
- Frontend pages statically rendered at request time
- No SignalR overhead for public pages
- Optimal SEO and load times

### Database Optimization
- Mandatory paging on all multi-entity queries
- Partition key usage for efficient queries
- Denormalized data where appropriate (e.g., category names in posts)

### Caching Strategy
- Static assets with versioning
- Browser caching for public content
- Server-side response caching for feeds and sitemap

## SEO & Social Features

### Implemented
- **Structured Data**: JSON-LD for blog posts and breadcrumbs
- **Open Graph**: Facebook/LinkedIn sharing
- **Twitter Cards**: Enhanced Twitter sharing
- **Sitemap.xml**: Dynamic XML sitemap generation
- **RSS/Atom Feeds**: Syndication support

### Metadata Configuration
All SEO metadata configured through `SiteMetadata`:
- Site title and description
- Author information
- Social media links
- Default images for sharing

## Extensibility Points

### Adding New Features
1. **New Entity**: Extend `BaseEntity`, add to `ApplicationDbContext`
2. **New Repository**: Implement `IRepository<TEntity>` or create specialized interface
3. **New Facade**: Create interface in `Infrastructure/`, implementation in `Facades/`
4. **New Page**: Add to `Frontend/Pages/` or `Admin/Pages/`
5. **New API**: Add endpoint in `Api/Endpoints/`, register in `ApiServiceExtensions`

### Plugin Architecture
The modular structure supports future plugin development:
- Self-contained feature folders
- Extension method registration
- Interface-based dependencies
- Configuration-driven behavior

## Deployment

### Docker Support
- Dockerfile configured for containerized deployment
- Multi-stage build for optimization
- Environment-based configuration

### Azure Deployment
- App Service or Container Apps
- CosmosDB (production instance)
- Blob Storage for media
- Key Vault for secrets
- Application Insights for monitoring

## Security Considerations

### Current
- HTTPS enforced
- Antiforgery tokens on forms
- Cookie HTTP-only flag
- Soft delete for data retention

### Planned
- Content Security Policy headers
- CORS configuration for APIs
- Rate limiting on public endpoints
- Media upload scanning

## Monitoring & Logging

### Logging
- Structured logging with `ILogger<T>`
- Scoped logging for request correlation
- Exception logging with Exceptionless

### Diagnostics
- Health check endpoints (planned)
- Application Insights integration (planned)
- Custom metrics for business events (planned)

## Future Roadmap

### Short-term
- Complete admin CRUD operations
- Image upload and management
- Markdown editor improvements
- Comment moderation

### Medium-term
- Multi-author support
- Draft/publish workflow
- Scheduled publishing
- Media library organization

### Long-term
- API for external integrations
- Webhook support
- Multi-site management
- Analytics dashboard

## Contributing

When contributing to Vilog:
1. Follow existing code patterns and conventions
2. Add unit tests for new functionality
3. Include XML documentation for public APIs
4. Use existing dependencies (avoid adding new libraries)
5. Ensure backward compatibility unless explicitly breaking
6. Update relevant README files in feature folders
