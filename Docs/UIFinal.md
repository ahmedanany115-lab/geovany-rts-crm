# RTS ERP — UI Navigation & UX (Final)

Supersedes UI.md. The route map and design system notes are unchanged and still stand; this document covers what's new (Notifications/Search now backed by real data) and tightens a few UX details that matter for a customer-facing demo.

## 1. Notifications — from static UI to real feature

- **NotificationBell** polls `GET /notifications/unread-count` every 30s (simple, reliable for a 25-user demo — no need for SignalR/websockets here).
- Clicking opens **NotificationsDrawer**: `GET /notifications` paginated list, unread rows visually distinct (left accent bar + subtle background tint, not just bold text — bold-only unread indicators are easy to miss at a glance).
- Each notification's `LinkUrl` navigates to the relevant record on click and marks it read (`PUT /notifications/{id}/read`) in the same action — no separate "mark read" click required.
- Empty state: "You're all caught up" with a checkmark illustration, not a blank drawer — first impression matters here since it's one click away at all times.

## 2. Global Search — command palette

- `⌘K` (or `Ctrl+K`) opens a centered modal, not a dropdown from the topbar input — matches the pattern this UI is deliberately imitating (Linear, Dynamics 365's search).
- Debounced (300ms) calls to `GET /search?q=`, results grouped by type with a small icon per group (customer/project/ticket/task), keyboard-navigable (arrow keys + enter), recent searches shown when the input is empty.
- Selecting a result navigates directly to that record's detail page and closes the palette.

## 3. UX tightening

- **Optimistic updates** for high-frequency interactions specifically: Kanban card drag (status/position), marking a notification read, checking off a task. Everything else (forms, creates) waits for the server response before navigating away — optimistic-everywhere makes errors confusing to recover from, so it's scoped to interactions where the "undo" cost of a rare failure is low and visible.
- **Toasts**: success toasts are brief (3s) and non-blocking; error toasts persist until dismissed and always state what to do next ("Couldn't save — check your connection and try again"), never just "Error 500."
- **Confirmation dialogs**: only for destructive + irreversible actions (delete, not soft-delete-reversible ones like archiving) — status changes and updates don't get a confirm dialog, since over-confirming trains users to click through them without reading.
- **Keyboard accessibility**: all interactive elements reachable via Tab, kanban drag-and-drop has a keyboard-operable fallback (select card → arrow keys to move between columns) — not just a nice-to-have, this is the kind of detail that separates "impressive demo" from "obviously just a prototype" when a technical stakeholder pokes at it.

## 4. Delta summary vs. UI.md

- Notifications and Search sections above replace the one-line descriptions in the original shell layout notes with real interaction specs.
- Added an explicit optimistic-update policy (previously implied, not stated).
- Added toast and confirmation-dialog conventions (previously unspecified).
- Route map, color palette, typography, card/table/motion conventions from UI.md are unchanged and still apply.

---

**Waiting for your approval to proceed to Step 6**: generate the Visual Studio solution — every backend project, folder, DI wiring, configuration, `Program.cs`, `appsettings.json`, `launchSettings.json`, package references — with no business logic implemented yet.
