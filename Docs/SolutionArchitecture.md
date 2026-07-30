# RTS ERP — Solution Architecture (v2)

Supersedes the layering description in Architecture.md/FolderStructure.md with the production-ready version, incorporating the fixes approved in `ArchitectureReview_1.md`: a `Shared` project, `RefreshTokens`, `Notifications`, `ActivityLogs`, `Attachments`, global search support, and confirmed use of AutoMapper + Swagger.

## 1. Backend Solution Structure

Five projects instead of four — `RTSErp.Shared` added beneath `Domain` as the true dependency root:

```
RTSErp.sln
├── src/
│   ├── RTSErp.Shared          (zero dependencies — referenced by every other project)
│   ├── RTSErp.Domain          (references: Shared)
│   ├── RTSErp.Application     (references: Domain, Shared)
│   ├── RTSErp.Infrastructure  (references: Application, Domain, Shared)
│   └── RTSErp.Api             (references: Application, Infrastructure, Shared)
└── tests/
    ├── RTSErp.Domain.UnitTests
    ├── RTSErp.Application.UnitTests
    └── RTSErp.Api.IntegrationTests
```

**Dependency direction:** `Shared ← Domain ← Application ← Infrastructure/Api`. Infrastructure and Api both sit at the outer edge and never reference each other. This is the same Clean Architecture rule as before, just with one more layer at the bottom.

### Why a separate `Shared` project instead of putting generic types in `Domain/Common`
`Domain` should hold business concepts (entities, value objects, domain events) — a `PaginatedList<T>` or a generic `Result<T>` wrapper isn't a business concept, it's plumbing every layer needs, including ones that shouldn't otherwise depend on `Domain` (e.g., a lightweight `Api` DTO validator). Splitting it out keeps `Domain` conceptually pure and gives `Api` a way to use `Result<T>`/`PaginatedList<T>` in response models without pulling in the whole domain model.

## 2. Project Responsibilities

| Project | Responsible for | Must NOT contain |
|---|---|---|
| `RTSErp.Shared` | `Result<T>`, `PaginatedList<T>`, `Constants` (claim types, cache keys), generic `Guard` clauses, generic extension methods (string/date helpers) | Anything business-specific, anything EF/ASP.NET-specific |
| `RTSErp.Domain` | Entities, value objects, enums, domain events, domain-level business rules/invariants (e.g., a Quotation can't be Accepted if already Expired) | EF Core attributes/fluent config, DTOs, MediatR, any framework reference |
| `RTSErp.Application` | Commands/Queries (MediatR), DTOs, FluentValidation validators, pipeline behaviors, interfaces that Infrastructure implements (`IApplicationDbContext`, `IFileStorageService`, `INotificationService`, `IActivityLogger`) | EF Core implementation, controllers, JWT logic |
| `RTSErp.Infrastructure` | `ApplicationDbContext`, entity configurations, migrations, repository implementations, `JwtTokenService`, `RefreshTokenService`, `FileStorageService` (local disk for demo), `ActivityLogger`, `NotificationDispatcher`, `DbSeeder` | Business rules, MediatR handlers |
| `RTSErp.Api` | Controllers, middleware, Swagger config, DI composition root (`Program.cs`), auth policy registration | Business logic, direct EF Core queries |

## 3. Frontend Structure

Unchanged in shape from FolderStructure.md's `rts-erp-web/`, with three additions to reflect the approved fixes:

```
src/features/
├── notifications/   (components/ hooks/ api/ — NotificationBell, NotificationsDrawer, useNotifications)
├── search/          (components/ hooks/ api/ — CommandPalette, useGlobalSearch)
└── activity/        (components/ — ActivityFeed, reused by Dashboard and Customer Detail's Activity tab)
```

`components/layout/NotificationsDrawer.tsx` (already scaffolded in the original structure) now has a real feature module behind it instead of being static UI. Same for the topbar's global search, which becomes a real `⌘K` command palette wired to `GET /search`.

## 4. Backend Folder Structure — deltas from FolderStructure.md

**New Domain entities** (`RTSErp.Domain/Entities/`):
```
Identity/
  └── RefreshToken.cs
Notifications/
  └── Notification.cs
ActivityLog/
  └── ActivityLog.cs
Attachments/
  └── Attachment.cs
```

**New Application module folders** (`RTSErp.Application/`):
```
Notifications/  (Queries/ — GetMyNotifications, MarkAsRead)
Search/         (Queries/ — GlobalSearch)
```
Activity logging and file attachments are cross-cutting, not user-facing "modules" with their own screens beyond what's embedded in Dashboard/Customer/Ticket/Project views — so they get infrastructure (a pipeline behavior + a service interface) rather than their own Commands/Queries folder tree. `IActivityLogger.Log(...)` is called from inside existing command handlers (e.g., `CreateCustomerCommandHandler` logs after a successful create); `IFileStorageService` is injected wherever an upload happens (ticket comments, company logo, avatar).

**New Infrastructure pieces**:
```
Infrastructure/
├── Identity/
│   └── RefreshTokenService.cs      (issue, rotate, revoke, reuse-detection)
├── Notifications/
│   └── NotificationDispatcher.cs   (writes Notification rows from domain events)
├── Logging/
│   └── ActivityLogger.cs           (implements IActivityLogger)
└── Files/
    └── LocalFileStorageService.cs  (implements IFileStorageService; swappable for Azure Blob later)
```

**New Api controllers**:
```
NotificationsController.cs   (GET /notifications, PUT /notifications/{id}/read)
SearchController.cs          (GET /search)
```

**Updated `AuthController`** now has a real `RefreshTokenService` behind `/auth/refresh` and `/auth/logout` instead of a stub.

## 5. Configuration Strategy

- **`appsettings.json`** — non-sensitive defaults committed to source control (logging levels, CORS allowed origins list shape, JWT issuer/audience, pagination defaults).
- **`appsettings.Development.json`** — local SQL Server connection string, verbose logging, Swagger enabled unconditionally.
- **`appsettings.Production.json`** — placeholders only; actual secrets never committed.
- **Secrets (connection string, JWT signing key, file storage root path)** — .NET User Secrets locally (`dotnet user-secrets`), environment variables in any hosted environment. `Program.cs` reads via `IConfiguration`, never hardcoded.
- **Strongly-typed options pattern** — `JwtSettings`, `FileStorageSettings`, `SeedDataSettings` bound via `IOptions<T>`, registered in `RTSErp.Api/Extensions/ServiceCollectionExtensions.cs`, not read ad-hoc from `IConfiguration` inside services.
- **Frontend `.env.local`** — `NEXT_PUBLIC_API_BASE_URL` only; nothing sensitive lives client-side since the refresh token is an httpOnly cookie set by the API, not read by the frontend.
- **CORS** — explicit allowed-origin list (the Next.js dev/prod origin), credentials enabled (required for the httpOnly refresh cookie to be sent cross-origin in local dev where ports differ).

## 6. Dependency Injection Structure

Composition root is `RTSErp.Api/Program.cs`, delegating to per-layer extension methods so `Program.cs` itself stays thin:

```csharp
// RTSErp.Application/DependencyInjection.cs
services.AddMediatR(...)                      // registers all Commands/Queries + handlers by assembly scan
services.AddValidatorsFromAssembly(...)        // FluentValidation
services.AddAutoMapper(...)                    // MappingProfile
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ActivityLoggingBehavior<,>))  // new

// RTSErp.Infrastructure/DependencyInjection.cs
services.AddDbContext<ApplicationDbContext>(...)
services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>())
services.AddScoped(typeof(IRepository<>), typeof(Repository<>))
services.AddScoped<IUnitOfWork, UnitOfWork>()
services.AddScoped<IJwtTokenService, JwtTokenService>()
services.AddScoped<IRefreshTokenService, RefreshTokenService>()   // new
services.AddScoped<IFileStorageService, LocalFileStorageService>() // new
services.AddScoped<IActivityLogger, ActivityLogger>()              // new
services.AddScoped<INotificationService, NotificationDispatcher>() // new
services.AddIdentity<ApplicationUser, ApplicationRole>(...)

// RTSErp.Api/Program.cs
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();   // permission-based policies
builder.Services.AddSwaggerWithJwtSupport();
builder.Services.AddRateLimiter(...);          // scoped to /auth/*, from the review
```

Each layer owns its own registration via an `IServiceCollection` extension method (`AddApplication()`, `AddInfrastructure()`), so `Program.cs` reads as a table of contents, not an implementation.

## 7. Shared Libraries

`RTSErp.Shared` contents:

```
RTSErp.Shared/
├── Results/
│   └── Result.cs, Result{T}.cs        # operation outcome wrapper, used by handlers and Api responses
├── Pagination/
│   └── PaginatedList{T}.cs            # backs the { items, totalCount, page, pageSize } envelope from API.md
├── Constants/
│   ├── ClaimTypes.cs                  # custom claim type strings (e.g. "permission")
│   └── CacheKeys.cs
├── Extensions/
│   ├── StringExtensions.cs
│   └── DateTimeExtensions.cs
└── Guards/
    └── Guard.cs                        # argument validation helpers used in Domain entity constructors
```

No frontend equivalent is needed as a separate package for a single-app demo — the frontend's `src/types/` and `src/lib/utils.ts` serve the same purpose without the overhead of a publishable shared package, which would only pay off if there were a second consuming app.

## 8. What changed vs. the original docs (summary)

- Added `RTSErp.Shared` as a 5th backend project.
- Added `RefreshToken`, `Notification`, `ActivityLog`, `Attachment` domain entities and their supporting Infrastructure services — full table/column definitions land in the Step 3 database update.
- Added `NotificationsController` and `SearchController`, plus a rate limiter scoped to `/auth/*`.
- Confirmed AutoMapper (removed the "or manual mapping" hedge) and Swagger with JWT bearer support in the composition root.
- Frontend gets three new feature folders (`notifications`, `search`, `activity`) backing UI that previously had no data layer behind it.

---

**Waiting for approval to proceed to Step 3**: database review + final tables/relationships/indexes/constraints/normalization review/ERD, updated to include `RefreshTokens`, `Notifications`, `ActivityLogs`, `Attachments`, and the permission seed list.
