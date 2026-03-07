# Viblog

> **Status: Active Development**  
> The long-term goal is a self-contained headless CMS/blog engine distributed as a NuGet package. Right now the engine implementation and a developer-testing website live together in the same solution while the architecture is being shaped.

---

> **Opinionated by design**  
> Viblog makes deliberate technology choices and does not try to be storage- or provider-agnostic. The selected stack — Azure (CosmosDB, Blob Storage), Auth0, Telerik UI — is treated as a first-class dependency, not a plugin point.  
>  
> That said, every external concern is hidden behind an interface in `Viblog.Infrastructure`, so it is _technically possible_ to provide alternative implementations (a different database provider, a different identity service, etc.). What you will find harder to escape are the conventions and data-shape assumptions that have been designed around those services — CosmosDB partition key strategy, Auth0 claim mappings, Telerik component contracts — and any replacement will need to respect those or carry its own migration cost.

---

## What is Viblog?

Viblog is a blogging/CMS engine built with ASP.NET Core Blazor and Azure services. It provides a full blog platform with:

- A **statically rendered public frontend** optimised for SEO and performance
- An **interactive server Blazor admin panel** for content management
- **Draft/Live content versioning** with scheduled publishing
- **Markdown authoring** with syntax-highlighted code blocks
- **Media management** backed by Azure Blob Storage
- **RSS/Atom feeds** and XML sitemaps out of the box
- **Audit logging** for all content changes
- **Auth0 OIDC** authentication for the admin area

### Blog features

| Feature | Status |
|---|---|
| Blog posts (draft/live/scheduled) | ✅ |
| Pages (standalone content) | ✅ |
| Categories & tags | ✅ |
| Archive | ✅ |
| Full-text search | ✅ |
| RSS & Atom feeds | ✅ |
| XML sitemap | ✅ |
| Structured data (JSON-LD) | ✅ |
| Media library | ✅ |
| Content version history | ✅ |
| Audit log | ✅ |
| User management (Auth0 sync) | ✅ |
| Analytics (in-engine, no third-party cookies) | 🔜 |

---

## Technology Stack

| Concern | Technology |
|---|---|
| Framework | .NET 10, ASP.NET Core Blazor |
| Rendering | Static SSR (public) + Interactive Server (admin) |
| Database | Azure CosmosDB via EF Core |
| Media storage | Azure Blob Storage |
| Authentication | Auth0 (OpenID Connect) |
| UI components | Telerik UI for Blazor |
| Markdown | Markdig |
| Syntax highlighting | ColorCode.HTML |
| Image processing | SkiaSharp |
| Local dev orchestration | .NET Aspire (CosmosDB & Storage emulators) |
| Tests | xUnit + AutoMoq |

---

## Solution Layout

```
Viblog.slnx
│
├── Viblog/                          # Main application (devtest site + engine)
│   ├── Frontend/                    # Statically rendered public blog
│   │   ├── Facades/                 # Read-side facades (list, detail, search, feeds…)
│   │   └── Models/                  # View models for the public UI
│   ├── Admin/                       # Interactive server Blazor admin panel
│   │   ├── Authentication/          # Auth0 sync & authentication state provider
│   │   ├── Facades/                 # Write-side facades (posts, pages, users, media…)
│   │   ├── Services/                # Dialog, messaging, audit, background workers
│   │   └── Workers/                 # Background services (content publishing)
│   ├── Shared/                      # Services and configuration shared across areas
│   │   ├── Configuration/           # Strongly-typed options (SiteMetadata, CosmosDb…)
│   │   ├── Services/                # Markdown, sitemap, search, media, content pipeline
│   │   └── Data/Seeders/            # Development data seeders
│   └── Api/                         # Minimal API endpoints (feeds, sitemap, media)
│
├── Viblog.Infrastructure/           # Contracts: interfaces, entities, models
│   ├── Shared/Data/Entities/        # Domain entities (BlogPost, Page, MediaItem…)
│   ├── Shared/Data/Repositories/    # Repository interfaces
│   ├── Shared/Services/             # Service interfaces
│   ├── Admin/Facades/               # Admin facade interfaces
│   └── Frontend/Facades/            # Frontend facade interfaces
│
├── Viblog.Data/
│   ├── Viblog.Data.CosmosDb/        # EF Core / CosmosDB repository implementations
│   └── Viblog.Data.AzureStorage/    # Azure Blob Storage media repository
│
├── Viblog.Tests/                    # xUnit unit & integration tests
│   ├── Facades/                     # Facade tests
│   ├── Services/                    # Service tests
│   ├── Api/Endpoints/               # Endpoint tests
│   ├── Admin/Authentication/        # Auth tests
│   └── Integration/                 # Integration tests
│
└── Aspire/
    ├── AppHost/                     # .NET Aspire orchestration (local dev)
    └── AppHost.ServiceDefaults/     # Shared Aspire service defaults
```

---

## Architecture

### Display-Facade-Repository pattern

All data access flows through three layers:

```
Razor component / endpoint
        │
    Facade              ← thin orchestration; one per feature area
        │
    Service             ← business/domain logic (optional intermediate)
        │
    Repository          ← data access (CosmosDB, Blob Storage)
```

- **Facades** are the only thing Blazor components and API endpoints call directly.
- **Repositories** implement interfaces declared in `Viblog.Infrastructure`, keeping data-layer implementations swappable.
- **Services** hold reusable logic (Markdown rendering, sitemap building, content scheduling, etc.).

### Content pipeline

Blog posts and pages follow a **Draft/Live** versioning model:

1. Authors edit the **Draft** — changes are saved but never visible publicly.
2. Publishing copies Draft → **Live** and records a version snapshot in `BlogPostVersion` / `PageVersion`.
3. **Scheduled publishing** is handled by `ContentSchedulingService` and a background worker (`ContentPublishingBackgroundService`).
4. All content mutations are written to the **audit log**.

### Rendering split

| Area | Render mode | Auth |
|---|---|---|
| Public blog (`/`, `/post/…`, `/category/…`, etc.) | Static SSR | None |
| Admin panel (`/admin/…`) | Interactive Server | Auth0 OIDC |
| API endpoints (feeds, sitemap, media) | Minimal API | None / API key |

### Data layer

`Viblog.Data.CosmosDb` provides EF Core repository implementations using the CosmosDB provider. The `ApplicationDbContext` is registered via .NET Aspire's `AddCosmosDbContext` helper, which wires up the connection string automatically from Aspire resource references.

`Viblog.Data.AzureStorage` implements `IMediaStorageRepository` using the Azure Blob Storage SDK.

---

## Getting Started (local development)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for CosmosDB & Storage emulators)
- An [Auth0](https://auth0.com/) tenant (for admin authentication)
- A Telerik UI for Blazor licence (trial or paid)

### Run with .NET Aspire

The recommended way to run locally is through the Aspire AppHost, which starts CosmosDB and Azure Storage emulators automatically via Docker.

```powershell
cd Aspire\AppHost
dotnet run
```

The Aspire dashboard will open and show you the running resources and their endpoints.

### Configuration

Copy `Viblog\appsettings.json` and create `appsettings.Development.json` (or use User Secrets). Minimum required settings:

```json
{
  "Auth0": {
    "Domain": "<your-tenant>.auth0.com",
    "ClientId": "<client-id>",
    "ClientSecret": "<client-secret>",
    "ManagementApiClientId": "<mgmt-client-id>",
    "ManagementApiClientSecret": "<mgmt-client-secret>"
  },
  "SiteMetadata": {
    "SiteName": "My Blog",
    "BaseUrl": "https://localhost:5001",
    "Author": "Your Name"
  }
}
```

Store secrets with User Secrets, never in source control:

```powershell
cd Viblog
dotnet user-secrets set "Auth0:ClientSecret" "<value>"
```

See [`Viblog/Docs/Auth0-README.md`](Viblog/Docs/Auth0-README.md) for full Auth0 setup instructions.

---

## Running Tests

```powershell
dotnet test Viblog.Tests
```

Tests use xUnit and AutoMoq. Integration tests spin up in-memory fakes and do not require live Azure services.

---

## Project Roadmap

The current repository mixes the blog engine implementation with a devtest website used to develop and exercise features. The intended future state:

1. **Extract the engine** into one or more NuGet packages (`Viblog.Core`, `Viblog.Data.CosmosDb`, `Viblog.Data.AzureStorage`, …).
2. **Provide host integration packages** so a host application adds Viblog with a few `builder.Services.AddViblog…()` calls.
3. **Decouple the devtest site** into a separate sample/reference project.
4. **In-engine analytics** — visitor and engagement tracking built directly into the engine with no third-party scripts or cookies required.

---

## Contributing

Contributions are welcome. Please follow the conventions below.

### Coding conventions

- **Display-Facade-Repository** — keep logic out of Razor components; facades are the entry point.
- **Interfaces live in `Viblog.Infrastructure`** — implementations live in the appropriate project.
- **Async all the way** — all I/O methods must be `async`, accept a `CancellationToken`, and end with `Async`.
- **Nullable enabled** — use `ArgumentNullException.ThrowIfNull` for guards; avoid `!` suppressors.
- **XML docs on all public members**.
- **No logic in views** — the Razor component should only bind and display; any calculation belongs in a facade or service.
- **SASS for styling** — no Bootstrap or other CSS frameworks.

### Tests

- All public logic must have unit tests in `Viblog.Tests`.
- Mirror the class under test: `BlogPostDetailFacade` → `BlogPostDetailFacadeTests`.
- Use xUnit `[Fact]` / `[Theory]`, AutoMoq for mocks, Arrange-Act-Assert structure.
- Do not use FluentAssertions (licence concern); use xUnit's built-in assertions.

### Pull requests

1. Branch from the current development branch.
2. Keep changes focused — one feature or fix per PR.
3. Ensure `dotnet build` and `dotnet test` pass before opening a PR.
4. Describe *what* changed and *why* in the PR description.
