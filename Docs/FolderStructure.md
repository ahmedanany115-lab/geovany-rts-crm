# RTS ERP — Folder Structure

## Backend — `rts-erp-api/`

```
rts-erp-api/
├── src/
│   ├── RTSErp.Domain/
│   │   ├── Entities/
│   │   │   ├── Common/
│   │   │   │   └── BaseEntity.cs
│   │   │   ├── Crm/
│   │   │   │   ├── Customer.cs
│   │   │   │   ├── Contact.cs
│   │   │   │   └── Lead.cs
│   │   │   ├── Quotations/
│   │   │   │   ├── Quotation.cs
│   │   │   │   └── QuotationLine.cs
│   │   │   ├── Projects/
│   │   │   │   ├── Project.cs
│   │   │   │   └── ProjectMember.cs
│   │   │   ├── Tasks/
│   │   │   │   ├── TaskItem.cs
│   │   │   │   ├── TaskComment.cs
│   │   │   │   └── TaskLabel.cs
│   │   │   ├── HelpDesk/
│   │   │   │   ├── Ticket.cs
│   │   │   │   └── TicketComment.cs
│   │   │   ├── Inventory/
│   │   │   │   ├── Product.cs
│   │   │   │   ├── License.cs
│   │   │   │   ├── HardwareAsset.cs
│   │   │   │   └── Supplier.cs
│   │   │   ├── Invoicing/
│   │   │   │   ├── Invoice.cs
│   │   │   │   └── InvoiceLine.cs
│   │   │   ├── Identity/
│   │   │   │   ├── ApplicationUser.cs
│   │   │   │   ├── Role.cs
│   │   │   │   ├── Permission.cs
│   │   │   │   └── Employee.cs
│   │   │   └── Settings/
│   │   │       └── CompanySettings.cs
│   │   ├── Enums/
│   │   │   ├── LeadStatus.cs
│   │   │   ├── QuotationStatus.cs
│   │   │   ├── ProjectStatus.cs
│   │   │   ├── TaskPriority.cs
│   │   │   ├── TicketStatus.cs
│   │   │   └── InvoiceStatus.cs
│   │   ├── Events/
│   │   │   └── (domain events, e.g. QuotationApprovedEvent.cs)
│   │   └── Common/
│   │       ├── Result.cs
│   │       └── DomainException.cs
│   │
│   ├── RTSErp.Application/
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IApplicationDbContext.cs
│   │   │   │   ├── IRepository.cs
│   │   │   │   ├── IUnitOfWork.cs
│   │   │   │   ├── ICurrentUserService.cs
│   │   │   │   └── IDateTimeProvider.cs
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   └── AuthorizationBehavior.cs
│   │   │   ├── Mappings/
│   │   │   │   └── MappingProfile.cs
│   │   │   └── Exceptions/
│   │   │       ├── NotFoundException.cs
│   │   │       └── ValidationException.cs
│   │   ├── Crm/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateCustomer/
│   │   │   │   ├── UpdateCustomer/
│   │   │   │   └── ConvertLeadToCustomer/
│   │   │   ├── Queries/
│   │   │   │   ├── GetCustomers/
│   │   │   │   ├── GetCustomerById/
│   │   │   │   └── GetLeads/
│   │   │   └── Dtos/
│   │   │       ├── CustomerDto.cs
│   │   │       └── LeadDto.cs
│   │   ├── Quotations/ (Commands/ Queries/ Dtos/ — same pattern)
│   │   ├── Projects/   (Commands/ Queries/ Dtos/)
│   │   ├── Tasks/      (Commands/ Queries/ Dtos/)
│   │   ├── HelpDesk/   (Commands/ Queries/ Dtos/)
│   │   ├── Inventory/  (Commands/ Queries/ Dtos/)
│   │   ├── Invoicing/  (Commands/ Queries/ Dtos/)
│   │   ├── Dashboard/  (Queries/ Dtos/ — aggregation queries only)
│   │   ├── Reports/    (Queries/ Dtos/)
│   │   ├── Identity/   (Commands/ Queries/ Dtos/ — login, refresh, users, roles)
│   │   └── Settings/   (Commands/ Queries/ Dtos/)
│   │
│   ├── RTSErp.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/          (IEntityTypeConfiguration per entity)
│   │   │   ├── Migrations/
│   │   │   ├── Repositories/
│   │   │   │   └── Repository.cs        (generic base)
│   │   │   ├── UnitOfWork.cs
│   │   │   └── Seed/
│   │   │       └── DbSeeder.cs          (25 mock users + demo data)
│   │   ├── Identity/
│   │   │   ├── JwtTokenService.cs
│   │   │   └── CurrentUserService.cs
│   │   └── Services/
│   │       └── DateTimeProvider.cs
│   │
│   └── RTSErp.Api/
│       ├── Controllers/
│       │   ├── v1/
│       │   │   ├── AuthController.cs
│       │   │   ├── CustomersController.cs
│       │   │   ├── LeadsController.cs
│       │   │   ├── QuotationsController.cs
│       │   │   ├── ProjectsController.cs
│       │   │   ├── TasksController.cs
│       │   │   ├── TicketsController.cs
│       │   │   ├── InventoryController.cs
│       │   │   ├── InvoicesController.cs
│       │   │   ├── DashboardController.cs
│       │   │   ├── ReportsController.cs
│       │   │   ├── UsersController.cs
│       │   │   └── SettingsController.cs
│       │   └── BaseApiController.cs
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs
│       │   └── SwaggerExtensions.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs
│
├── tests/
│   ├── RTSErp.Application.UnitTests/
│   ├── RTSErp.Domain.UnitTests/
│   └── RTSErp.Api.IntegrationTests/
│
├── docker-compose.yml
└── RTSErp.sln
```

## Frontend — `rts-erp-web/`

```
rts-erp-web/
├── src/
│   ├── app/
│   │   ├── (auth)/
│   │   │   ├── login/page.tsx
│   │   │   └── forgot-password/page.tsx
│   │   ├── (dashboard)/                      # authenticated shell layout
│   │   │   ├── layout.tsx                    # sidebar + topbar
│   │   │   ├── dashboard/page.tsx
│   │   │   ├── crm/
│   │   │   │   ├── customers/page.tsx
│   │   │   │   ├── customers/[id]/page.tsx
│   │   │   │   ├── contacts/page.tsx
│   │   │   │   └── leads/page.tsx
│   │   │   ├── quotations/
│   │   │   │   ├── page.tsx
│   │   │   │   ├── new/page.tsx
│   │   │   │   └── [id]/page.tsx
│   │   │   ├── projects/
│   │   │   │   ├── page.tsx
│   │   │   │   └── [id]/page.tsx
│   │   │   ├── tasks/
│   │   │   │   ├── board/page.tsx
│   │   │   │   └── calendar/page.tsx
│   │   │   ├── helpdesk/
│   │   │   │   ├── page.tsx
│   │   │   │   └── [id]/page.tsx
│   │   │   ├── inventory/
│   │   │   │   ├── products/page.tsx
│   │   │   │   ├── licenses/page.tsx
│   │   │   │   ├── hardware/page.tsx
│   │   │   │   └── suppliers/page.tsx
│   │   │   ├── invoices/page.tsx
│   │   │   ├── reports/
│   │   │   │   ├── sales/page.tsx
│   │   │   │   ├── projects/page.tsx
│   │   │   │   ├── customers/page.tsx
│   │   │   │   └── revenue/page.tsx
│   │   │   ├── users/
│   │   │   │   ├── page.tsx
│   │   │   │   ├── roles/page.tsx
│   │   │   │   └── employees/page.tsx
│   │   │   ├── settings/
│   │   │   │   ├── company/page.tsx
│   │   │   │   └── general/page.tsx
│   │   │   └── profile/page.tsx
│   │   ├── api/                              # route handlers (BFF-lite: token refresh proxy etc.)
│   │   │   └── auth/refresh/route.ts
│   │   ├── layout.tsx                        # root layout, ThemeProvider, QueryClientProvider
│   │   └── globals.css
│   │
│   ├── features/                             # feature-based, mirrors backend modules
│   │   ├── auth/
│   │   │   ├── components/ (LoginForm, ForgotPasswordForm)
│   │   │   ├── hooks/ (useLogin, useAuth)
│   │   │   ├── api/ (authApi.ts)
│   │   │   └── types.ts
│   │   ├── crm/
│   │   │   ├── components/ (CustomerTable, CustomerForm, LeadKanban, ContactList)
│   │   │   ├── hooks/ (useCustomers, useLeads)
│   │   │   ├── api/
│   │   │   └── types.ts
│   │   ├── quotations/ (components/ hooks/ api/ types.ts — incl. QuotationPdfPreview)
│   │   ├── projects/   (components/ hooks/ api/ types.ts)
│   │   ├── tasks/      (components/ hooks/ api/ types.ts — KanbanBoard, TaskCalendar)
│   │   ├── helpdesk/   (components/ hooks/ api/ types.ts)
│   │   ├── inventory/  (components/ hooks/ api/ types.ts)
│   │   ├── invoices/   (components/ hooks/ api/ types.ts)
│   │   ├── dashboard/  (components/ — KpiCard, RevenueChart, ActivityFeed)
│   │   ├── reports/    (components/ hooks/ api/)
│   │   ├── users/      (components/ hooks/ api/ — Roles, Permissions, Employees)
│   │   └── settings/   (components/ hooks/ api/)
│   │
│   ├── components/
│   │   ├── ui/                               # shadcn/ui primitives (button, dialog, table...)
│   │   ├── layout/
│   │   │   ├── Sidebar.tsx
│   │   │   ├── Topbar.tsx
│   │   │   ├── NotificationsDrawer.tsx
│   │   │   └── ThemeToggle.tsx
│   │   ├── charts/                           # Recharts wrappers
│   │   └── data-table/                       # reusable table w/ sort, filter, pagination
│   │
│   ├── lib/
│   │   ├── api-client.ts                     # fetch wrapper, auth header injection
│   │   ├── query-client.ts
│   │   ├── utils.ts
│   │   └── validations/                      # zod schemas per feature
│   │
│   ├── stores/                                # Zustand stores (ui-state.ts, sidebar-state.ts)
│   ├── types/                                  # shared/global types
│   └── mocks/                                  # mock data for dashboard/reports demo polish
│
├── public/
├── tailwind.config.ts
├── components.json                             # shadcn config
├── next.config.js
└── package.json
```

## Naming & organization conventions

- Backend: one folder per module under `Application/`, each with `Commands/`, `Queries/`, `Dtos/` — a module's entire application logic lives in one place (matches "work module by module").
- Frontend: `app/` holds routing only; actual UI logic lives in `features/<module>/`, imported into pages — keeps routes thin and features testable/reusable.
- Every Command/Query is its own folder with `Command.cs` + `CommandHandler.cs` + `CommandValidator.cs` (or Query equivalents) — one class, one file, one responsibility.
