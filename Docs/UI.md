# RTS ERP — UI Navigation Map

## Shell layout (authenticated area)

```
┌─────────────────────────────────────────────────────────────┐
│ Topbar: [Logo] [Global Search]     [Notifications] [Theme] [Avatar▾] │
├───────────┬───────────────────────────────────────────────────┤
│           │                                                     │
│  Sidebar  │                  Page content                       │
│           │                                                     │
│ Dashboard │                                                     │
│ CRM ▾     │                                                     │
│ Quotations│                                                     │
│ Projects  │                                                     │
│ Tasks ▾   │                                                     │
│ Help Desk │                                                     │
│ Inventory ▾│                                                    │
│ Invoices  │                                                     │
│ Reports ▾ │                                                     │
│ Users ▾   │                                                     │
│ Settings  │                                                     │
│           │                                                     │
│ [collapse]│                                                     │
└───────────┴───────────────────────────────────────────────────┘
```

- Sidebar: collapsible, icon-only when collapsed, active-route highlight, sub-items expand for CRM/Tasks/Inventory/Reports/Users.
- Topbar: global search (⌘K command palette style), notification bell with unread badge → drawer, theme toggle, avatar menu (Profile, Settings, Logout).
- Breadcrumbs under the topbar on detail pages (e.g. `Projects / Acme Website Revamp`).

## Route map

| Route | Screen | Key components |
|---|---|---|
| `/login` | Login | LoginForm (email, password, remember me) |
| `/forgot-password` | Forgot Password | Email input, sent-confirmation state |
| `/dashboard` | Dashboard | KPI cards (Revenue, Active Projects, Active Tickets, Employees), Revenue trend chart, Project status chart, Recent Activity feed |
| `/crm/customers` | Customer list | DataTable (search, filter by industry/owner), "New Customer" drawer |
| `/crm/customers/[id]` | Customer detail | Overview tab, Contacts tab, Quotations tab, Tickets tab, Activity tab |
| `/crm/contacts` | Contact list | DataTable, linked customer column |
| `/crm/leads` | Lead pipeline | Kanban by status (New → Contacted → Qualified → Converted/Lost) + list toggle |
| `/quotations` | Quotation list | DataTable with status badges, "New Quotation" |
| `/quotations/new` | Create Quotation | Customer picker, line-item builder (add product/qty/price), live total, save as draft/send |
| `/quotations/[id]` | Quotation detail | Header + lines, status timeline, "Preview PDF" (side panel/modal), Send/Accept/Reject actions |
| `/projects` | Project list | Card grid or table toggle, progress bars, status filter |
| `/projects/[id]` | Project detail | Overview (progress, budget, dates), Team Members tab, Timeline/Gantt-lite tab, linked Tasks tab |
| `/tasks/board` | Kanban board | Columns: Todo/In Progress/In Review/Done, drag-and-drop cards, priority + assignee avatar on card, filter by project/assignee |
| `/tasks/calendar` | Calendar view | Month/week toggle, tasks plotted by due date, click → task detail modal |
| `/helpdesk` | Ticket list | DataTable, status/priority badges, filter by assignee |
| `/helpdesk/[id]` | Ticket detail | Description, comment thread (internal notes visually distinct), status/priority/assignee controls |
| `/inventory/products` | Product list | DataTable by category |
| `/inventory/licenses` | License list | DataTable, seats-used progress bar, expiry warning badges |
| `/inventory/hardware` | Hardware list | DataTable, status badges, assigned-to column |
| `/inventory/suppliers` | Supplier list | DataTable, linked products count |
| `/invoices` | Invoice list | DataTable, payment status badges (Paid/Overdue/Partial), "Record Payment" action |
| `/reports/sales` | Sales report | Date range picker, chart + summary table |
| `/reports/projects` | Project report | Status distribution chart, table |
| `/reports/customers` | Customer report | Value/activity ranking table |
| `/reports/revenue` | Revenue report | Trend chart, breakdown by module/category |
| `/users` | User list | DataTable, role badge, active/inactive toggle |
| `/users/roles` | Roles & Permissions | Role list → permission matrix (checkbox grid per module) |
| `/users/employees` | Employee directory | Card grid, department filter, org info |
| `/settings/company` | Company Info | Logo upload, name/address/tax id form |
| `/settings/general` | General Settings | Currency, fiscal year, notification defaults |
| `/profile` | User Profile | Avatar, name, password change, notification preferences |

## Design system notes

- **Color palette:** neutral slate base (backgrounds/borders) + a single confident primary accent (indigo/blue) for actions and active states; semantic colors reserved strictly for status (green=success/paid, amber=warning/pending, red=overdue/critical, blue=info) — status badges are the main color signal so they stay legible in both themes.
- **Typography:** Inter (or Geist), tight heading tracking, consistent type scale via Tailwind (`text-sm` body default for data-dense screens, `text-2xl`/`font-semibold` page titles).
- **Cards & tables:** subtle shadow + 1px border (not heavy drop-shadows), generous but not wasteful padding, sticky table headers on scroll, row hover states, skeleton loaders (not spinners) for perceived performance.
- **Motion:** Framer Motion for panel/drawer/modal transitions and page-level fade/slide on route change — kept short (150–250ms) and consistent, never decorative-only.
- **Empty/loading/error states** designed per list view, not an afterthought — this is one of the biggest tells between "demo" and "prototype."
