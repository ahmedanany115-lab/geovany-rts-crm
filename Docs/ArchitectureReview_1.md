# RTS ERP — Architecture Review Report

Review of Architecture.md, FolderStructure.md, Database.md, API.md, UI.md, and Roadmap.md against the full requirement set, checking for missing pieces, inconsistencies, and risk before any code is generated.

## 1. Problems

| # | Problem | Where | Severity |
|---|---|---|---|
| 1 | No `RefreshTokens` table. The architecture commits to refresh-token rotation, but there's nowhere to persist/revoke them — as written, refresh is unimplementable and logout can't actually invalidate a session. | Database.md | High |
| 2 | No `Notifications` entity or endpoints. UI.md specifies a notification bell + drawer with an unread badge; API.md has nothing backing it — it would have to be hardcoded/fake, which undercuts the "not a mockup" goal. | Database.md, API.md | High |
| 3 | No `ActivityLog` entity. Dashboard's "Recent Activity" and Customer Detail's "Activity" tab both need a cross-module event feed; nothing generates or stores those events currently. | Database.md, API.md | Medium |
| 4 | No file/attachment storage strategy. Company logo upload, user avatars, and (implicitly) ticket/project attachments are all in the UI spec with no backing entity, storage location, or endpoint. | Database.md, API.md, UI.md | Medium |
| 5 | No global search endpoint. UI.md specifies a ⌘K-style global search in the topbar; API.md has only per-module search via list-endpoint query params, which can't power a cross-module search box. | API.md | Medium |
| 6 | `Requirements.md` and `Modules.md` don't exist as standalone files. Their content is currently folded into Architecture.md's intro and the original module list, which breaks the doc set convention already established (`Requirements.md`, `Modules.md`, `Database.md`, `API.md`, `UI.md`). | Doc set overall | Low |
| 7 | No explicit Quotation→Invoice conversion endpoint. Invoices can reference a `QuotationId` on create, but there's no `POST /quotations/{id}/convert-to-invoice` mirroring the Lead→Customer conversion pattern already used elsewhere — inconsistent API design language. | API.md | Low |
| 8 | AutoMapper is listed as "or manual mapping extensions" in Architecture.md — this brief commits to AutoMapper explicitly; the hedge should be resolved now, not left ambiguous into the coding phase. | Architecture.md | Low |
| 9 | No rate limiting called out on `/auth/login` or `/auth/forgot-password`. For a system presented as "commercial-grade," brute-force protection on auth endpoints is a normal customer question in a security review. | API.md | Medium |
| 10 | Permissions are modeled (`Permissions`, `RolePermissions`) but never enumerated — there's no seed list of what permissions actually exist per module, so the Roles & Permissions screen has nothing concrete to render. | Database.md | Medium |

## 2. Recommendations

- Add `RefreshTokens` table: `Id, UserId (FK), Token (hashed), ExpiresAt, CreatedAt, RevokedAt (nullable), ReplacedByToken (nullable), CreatedByIp`. Standard rotation-with-reuse-detection pattern.
- Add `Notifications` table: `Id, UserId (FK), Type, Title, Message, Link, IsRead, CreatedAt`. Populated by domain events (e.g., ticket assigned, quotation accepted) via a MediatR notification handler — this also gives Recent Activity a natural source.
- Add `ActivityLogs` table: `Id, EntityType, EntityId, Action, Description, ActorId (FK), CreatedAt`. A pipeline behavior on state-changing commands writes to it, so every module gets activity logging for free rather than bolting it on per-module later.
- Add `Attachments` table: `Id, EntityType, EntityId, FileName, FileUrl, ContentType, SizeBytes, UploadedById, CreatedAt`, generic enough to serve tickets, projects, and quotations. For the demo, back it with local disk storage behind an `IFileStorageService` interface (swap-in point for Azure Blob later without touching callers).
- Add `GET /search?q=` — a lightweight endpoint that queries top N matches across Customers, Projects, Tickets, and Tasks by name/title, returns a typed union result for the command palette.
- Split `Requirements.md` and `Modules.md` out as their own files so the doc set matches what was specified — thin documents that reference the fuller detail in Architecture.md/Database.md rather than duplicating it.
- Add `POST /quotations/{id}/convert-to-invoice` for API consistency with the Lead conversion pattern; keep the existing "create invoice with optional QuotationId" as a manual fallback.
- Commit to AutoMapper in Architecture.md — remove the "or manual mapping" hedge.
- Add ASP.NET Core's built-in rate limiter middleware scoped to `/auth/*`.
- Add a seed list of permissions to Database.md as an appendix — one row per module × action (`crm.customers.read/write/delete`, `invoices.read/write`, etc.) so Roles & Permissions has real data to render from day one.

## 3. Missing Features

- Notifications module (entity + API + real-time-ish polling or SignalR — polling is enough for a demo)
- Activity log / audit trail
- File attachment support (logo, avatar, ticket/project attachments)
- Global search
- Refresh token persistence (session management, "log out of all devices" becomes possible once this exists)
- Explicit permission seed data

None of these are new modules in the 12-module list — they're supporting infrastructure the existing modules quietly depend on but don't currently have.

## 4. Improvements

- **Response envelope:** define one concrete example DTO shape (list envelope + single-resource shape + `ProblemDetails` error shape) in API.md so every controller returns identically-shaped responses without each handler re-deciding the format.
- **Currency:** `CompanySettings.DefaultCurrency` exists, but Quotations/Invoices don't carry a currency field. For a single-company demo this is fine to leave implicit (everything is the company's default currency) — flagged as a deliberate scope cut, not an oversight, unless you want per-customer currency.
- **Testing note in Roadmap:** FolderStructure.md includes test projects, but Roadmap.md doesn't mention test deliverables per phase, which conflicts with the coding rule that every module ships with "Testing." Recommend each phase's acceptance criteria explicitly include unit tests for handlers + one integration test for the controller, not a separate testing phase.

## 5. Final Architecture Score: 8/10

The layering, module boundaries, and data model are sound and consistent with the stated Clean Architecture / CQRS-lite approach, and the UI/API/DB docs agree with each other everywhere I checked except the gaps above. The deductions are entirely about supporting infrastructure (notifications, activity log, attachments, refresh token persistence, search) that the UI spec assumes exists but the data/API layer doesn't yet back — exactly the kind of gap this review step is meant to catch before code gets written. None of it changes the architecture; it's additive.

---

**Waiting for approval before continuing**, per the workflow. If approved, next step is: fold these additions into Database.md and API.md (updated versions), split out Requirements.md/Modules.md, then move to Phase 0 scaffolding — still no feature code.
