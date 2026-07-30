# RTS ERP — System Architecture

## 1. Overview

RTS ERP is a demo enterprise resource planning system for an IT Solutions company (~25 users). It is built to look and behave like a commercial-grade product (Odoo / Dynamics 365 / ERPNext tier of polish) while remaining a scoped demo: real architecture, real patterns, mock data where a full backend isn't justified yet.

Two independently deployable applications:

- **rts-erp-web** — Next.js 14+ (App Router) frontend, TypeScript, Tailwind, shadcn/ui
- **rts-erp-api** — ASP.NET Core 8 Web API, Clean Architecture, EF Core, SQL Server

They communicate over a versioned REST API secured with JWT bearer tokens. No BFF layer for the demo — Next.js server components/route handlers call the API directly (server-side) or the browser calls it via React Query (client-side), depending on the data.

## 2. Guiding Principles

- **Clean Architecture** on the backend: dependencies point inward (Domain has zero dependencies; Application depends on Domain; Infrastructure/API depend on Application).
- **Repository + Unit of Work pattern** over EF Core to keep persistence swappable and testable, with CQRS-flavored use cases (MediatR) rather than fat controllers.
- **SOLID** throughout — especially single-responsibility handlers/services and interface-driven dependencies for DI.
- **Feature-based frontend structure** rather than type-based, so a module (e.g. CRM) is self-contained: components, hooks, types, API calls live together.
- **Contract-first modules** — every module ships with its DB schema, API contract, and UI spec before code, per the project's development rules.
- **Mock-data-first for breadth, real-backend for depth** — Dashboard/Reports/some list views can run on mock/seeded data; Auth, CRM, Projects, Tasks, Help Desk get real end-to-end plumbing since they carry the architectural weight of the demo.

## 3. Backend Architecture (Clean Architecture, 4 layers)

```
┌─────────────────────────────────────────────┐
│                  API Layer                   │  Controllers, Middleware, Filters,
│         (RTSErp.Api)                         │  JWT config, Swagger, DI composition root
└───────────────────┬───────────────────────────┘
                     │ depends on
┌───────────────────▼───────────────────────────┐
│              Application Layer                 │  Use cases (Commands/Queries via MediatR),
│         (RTSErp.Application)                    │  DTOs, Validators (FluentValidation),
│                                                  │  Interfaces (IRepository, IEmailService...)
└───────────────────┬───────────────────────────┘
                     │ depends on
┌───────────────────▼───────────────────────────┐
│               Domain Layer                      │  Entities, Value Objects, Enums,
│         (RTSErp.Domain)                          │  Domain Events, Business Rules
│               (zero dependencies)                │
└─────────────────────────────────────────────────┘
                     ▲
                     │ implements interfaces from Application
┌─────────────────────────────────────────────────┐
│            Infrastructure Layer                  │  EF Core DbContext, Repositories,
│         (RTSErp.Infrastructure)                   │  Identity/JWT, external services,
│                                                    │  migrations
└─────────────────────────────────────────────────┘
```

**Why MediatR + CQRS-lite instead of plain services:** with 12 modules, controllers-calling-services tends to bloat into god-services. Commands/Queries per use case keep each handler single-purpose, make cross-cutting concerns (validation, logging, authorization) pluggable via pipeline behaviors, and map cleanly to the "module by module" delivery rule — a module's slice is a folder of Commands/Queries, nothing else touches it.

### Cross-cutting concerns
- **Validation:** FluentValidation validators run as a MediatR pipeline behavior before the handler executes.
- **Logging:** Serilog, structured, request/response logging middleware + MediatR logging behavior.
- **Exception handling:** global exception middleware maps domain/validation exceptions to consistent `ProblemDetails` responses.
- **AuthZ:** policy-based authorization (roles + permissions, see User Management module) via `[Authorize]` + custom `IAuthorizationHandler`s.
- **AutoMapper** (or manual mapping extensions) for Entity ⇄ DTO.

## 4. Frontend Architecture

Next.js App Router, feature-based:

- **Server Components** for initial data-heavy pages (Dashboard, Reports, list pages) — fetch on the server, stream HTML, better perceived performance and SEO-irrelevant-but-still-good practice.
- **Client Components + React Query** for interactive views (Kanban board, forms, modals, anything with mutations/optimistic updates).
- **React Hook Form + Zod** for all forms, resolvers shared with backend DTO shapes conceptually.
- **shadcn/ui** as the component primitive layer (Radix-based, unstyled-by-default, styled via Tailwind tokens) — this is what gives the "premium SaaS" look without a heavy custom design system.
- **next-themes** for dark/light mode.
- **Zustand** (lightweight) for UI-only client state (sidebar collapsed, active theme, notification drawer) — React Query owns server state, so no Redux needed.

### Rendering strategy per module
| Module | Strategy |
|---|---|
| Dashboard | Server Component shell + client charts (Recharts) hydrated with mock/aggregated data |
| CRM / Projects / Tasks / Help Desk / Invoices / Inventory | Server Component list (initial fetch) + client-side table interactions (sort/filter/paginate via React Query) |
| Kanban board, Calendar | Fully client component (drag-and-drop needs client state) |
| Settings / User Management | Client forms, React Hook Form + Zod, mutations via React Query |

## 5. Authentication & Security

- JWT bearer access token (short-lived, ~15 min) + refresh token (httpOnly cookie, rotated).
- ASP.NET Core Identity for user store (password hashing, lockout policy) even though the demo has ~25 seeded users.
- Role-based + permission-based authorization: Roles (Admin, Manager, Employee, Support Agent, Read-Only) map to granular Permissions (e.g. `crm.customers.write`) resolved at login and embedded as claims.
- CORS locked to the frontend origin.
- Frontend stores access token in memory (not localStorage) with silent refresh via httpOnly refresh cookie, to reduce XSS token theft surface — reasonable for a demo that still wants to look production-minded.

## 6. Environments & Deployment (demo scope)

- Local dev: `docker-compose` with SQL Server container + API + (Next.js runs via `npm run dev`, not containerized, for fast HMR).
- Config via `appsettings.{Environment}.json` + `.env.local` on the frontend.
- No CI/CD pipeline required for the demo, but the folder structure and layering are chosen so one could be added without restructuring.

## 7. What's "real" vs "mocked" in this demo

| Layer | Real | Mocked |
|---|---|---|
| Auth, CRM, Projects, Tasks, Help Desk | Full EF Core + SQL Server + API | — |
| Dashboard KPIs/charts | Aggregation queries against seeded data | Some trend/forecast numbers computed client-side for visual polish |
| Reports | Real queries against seeded data | Export-to-PDF/Excel stubbed |
| Quotations PDF preview | Real quotation data | PDF rendered client-side (react-pdf) from real data, not stored/emailed |
| Invoices payment status | Real entity + status field | Payment gateway integration not implemented (status changed manually) |

This keeps every module demonstrably wired to a real data model (so it doesn't feel like a static mockup) while being honest about what's out of scope for a demo timeline.

## 8. Next steps

Folder structure, database schema/ERD, API design, and UI navigation map follow in the accompanying docs. No application code is written until these are reviewed and approved, per the project's development rules.
