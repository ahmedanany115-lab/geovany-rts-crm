# RTS ERP — Development Roadmap

Work proceeds module by module, per the project rules: no application code until the architecture, schema, API, and UI map for that phase are approved. Each phase below ends with a working, demo-able slice.

## Phase 0 — Foundations (this deliverable)
- Architecture, folder structure, database schema/ERD, API design, UI navigation map ✅ (this document set)
- Once approved: scaffold both solutions (empty Clean Architecture solution + Next.js app with shadcn/ui, theme provider, layout shell) — no feature code yet.

## Phase 1 — Auth + Shell
- Backend: Identity setup, JWT issuance/refresh, seeded roles/permissions/25 demo users.
- Frontend: Login, Forgot Password, authenticated shell (Sidebar, Topbar, theme toggle, notifications drawer UI), Profile page.
- Outcome: can log in as different roles and see the shell render correctly per permissions.

## Phase 2 — Dashboard
- Backend: KPI aggregation queries, revenue-trend query, recent-activity query (initially against seed data).
- Frontend: KPI cards, charts (Recharts), activity feed — this is the first-impression screen, gets extra visual polish.

## Phase 3 — CRM
- Customers, Contacts, Leads (with lead-to-customer conversion), Customer Detail with tabs.

## Phase 4 — Quotations
- Quotation builder with line items, status workflow, PDF preview (react-pdf, client-rendered from real data).

## Phase 5 — Projects & Tasks
- Projects (list, detail, team members, progress/timeline).
- Tasks: Kanban board (drag-and-drop, status/position updates), Calendar view, comments.

## Phase 6 — Help Desk
- Ticket list/detail, status/priority workflow, internal vs customer-visible comments.

## Phase 7 — Inventory & Invoices
- Products, Licenses (seat tracking), Hardware assets, Suppliers.
- Invoices (from scratch or generated from an accepted Quotation), payment status tracking.

## Phase 8 — Reports
- Sales, Projects, Customers, Revenue reports — real queries against by-then-populous seed data, charts + tables, date range filters.

## Phase 9 — User Management & Settings
- Users, Roles & Permissions matrix, Employee directory.
- Company Info, General Settings.

## Phase 10 — Polish pass
- Empty/loading/error states audit across all modules, animation consistency pass, responsive QA (tablet/mobile breakpoints), dark/light mode contrast audit, seed data richness pass (realistic names/numbers/timelines so the demo doesn't feel synthetic).

## Sequencing rationale
Auth/Shell and Dashboard come first because they're what a stakeholder sees in the first 30 seconds. CRM → Quotations → Projects → Tasks → Help Desk follows the natural IT-solutions-company sales-to-delivery-to-support flow, so at any phase boundary the demo tells a coherent story rather than being a grab-bag of unconnected screens. Inventory/Invoices/Reports/User Management round out the back office once the core narrative modules exist.

---

**Next step:** review Architecture.md, FolderStructure.md, Database.md, API.md, and UI.md. Once approved (as a whole or with requested changes), Phase 0 scaffolding begins — still no feature code, just the two empty, correctly-wired project skeletons.
