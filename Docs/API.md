# RTS ERP — API Design

Base URL: `/api/v1`. All endpoints (except `auth/login`, `auth/refresh`, `auth/forgot-password`) require `Authorization: Bearer <token>`. Responses use a consistent envelope for lists: `{ items, totalCount, page, pageSize }`. Errors follow RFC 7807 `ProblemDetails`.

## Auth
| Method | Endpoint | Description |
|---|---|---|
| POST | `/auth/login` | Email + password → access token + refresh cookie |
| POST | `/auth/refresh` | Rotate refresh token → new access token |
| POST | `/auth/logout` | Revoke refresh token |
| POST | `/auth/forgot-password` | Send reset link (stubbed/logged in demo) |
| POST | `/auth/reset-password` | Set new password with token |
| GET | `/auth/me` | Current user profile + permissions |
| PUT | `/auth/me` | Update own profile |

## Dashboard
| Method | Endpoint | Description |
|---|---|---|
| GET | `/dashboard/kpis` | Revenue, active projects, active tickets, employee count |
| GET | `/dashboard/revenue-trend` | Monthly revenue series for chart |
| GET | `/dashboard/recent-activity` | Cross-module activity feed |

## CRM
| Method | Endpoint | Description |
|---|---|---|
| GET | `/customers` | Paginated, filter by industry/owner, search by name |
| GET | `/customers/{id}` | Full detail incl. contacts, open quotations/tickets summary |
| POST | `/customers` | Create |
| PUT | `/customers/{id}` | Update |
| DELETE | `/customers/{id}` | Soft delete |
| GET | `/customers/{id}/contacts` | Contacts for a customer |
| POST | `/contacts` | Create contact |
| PUT | `/contacts/{id}` | Update contact |
| DELETE | `/contacts/{id}` | Delete contact |
| GET | `/leads` | Paginated, filter by status/owner |
| POST | `/leads` | Create |
| PUT | `/leads/{id}` | Update |
| POST | `/leads/{id}/convert` | Convert lead → customer |
| DELETE | `/leads/{id}` | Delete |

## Quotations
| Method | Endpoint | Description |
|---|---|---|
| GET | `/quotations` | Paginated, filter by status/customer |
| GET | `/quotations/{id}` | Full detail with lines |
| POST | `/quotations` | Create (with lines) |
| PUT | `/quotations/{id}` | Update |
| POST | `/quotations/{id}/send` | Mark Sent |
| POST | `/quotations/{id}/accept` | Mark Accepted |
| POST | `/quotations/{id}/reject` | Mark Rejected |
| GET | `/quotations/{id}/pdf` | Generate PDF (streamed) |
| DELETE | `/quotations/{id}` | Delete (Draft only) |

## Projects
| Method | Endpoint | Description |
|---|---|---|
| GET | `/projects` | Paginated, filter by status/customer |
| GET | `/projects/{id}` | Detail incl. members, progress, linked tasks summary |
| POST | `/projects` | Create |
| PUT | `/projects/{id}` | Update |
| PUT | `/projects/{id}/progress` | Update progress percent |
| POST | `/projects/{id}/members` | Add member |
| DELETE | `/projects/{id}/members/{employeeId}` | Remove member |
| DELETE | `/projects/{id}` | Soft delete |

## Tasks
| Method | Endpoint | Description |
|---|---|---|
| GET | `/tasks` | Filter by project/assignee/status, supports board grouping |
| GET | `/tasks/board` | Grouped by status column for Kanban |
| GET | `/tasks/calendar` | Date-ranged, for calendar view |
| GET | `/tasks/{id}` | Detail incl. comments |
| POST | `/tasks` | Create |
| PUT | `/tasks/{id}` | Update fields |
| PUT | `/tasks/{id}/status` | Move between kanban columns (status + position) |
| POST | `/tasks/{id}/comments` | Add comment |
| DELETE | `/tasks/{id}` | Delete |

## Help Desk
| Method | Endpoint | Description |
|---|---|---|
| GET | `/tickets` | Paginated, filter by status/priority/assignee |
| GET | `/tickets/{id}` | Detail incl. comments |
| POST | `/tickets` | Create |
| PUT | `/tickets/{id}` | Update status/priority/assignee |
| POST | `/tickets/{id}/comments` | Add comment (internal or customer-visible) |
| DELETE | `/tickets/{id}` | Delete |

## Inventory
| Method | Endpoint | Description |
|---|---|---|
| GET | `/products` | Paginated, filter by category |
| POST/PUT/DELETE | `/products/{id}` | CRUD |
| GET | `/licenses` | Filter by product/customer, seats used/total |
| POST/PUT/DELETE | `/licenses/{id}` | CRUD |
| GET | `/hardware` | Filter by status/customer/assignee |
| POST/PUT/DELETE | `/hardware/{id}` | CRUD |
| GET | `/suppliers` | Paginated |
| POST/PUT/DELETE | `/suppliers/{id}` | CRUD |

## Invoices
| Method | Endpoint | Description |
|---|---|---|
| GET | `/invoices` | Paginated, filter by status/customer |
| GET | `/invoices/{id}` | Detail with lines |
| POST | `/invoices` | Create (optionally from a Quotation) |
| PUT | `/invoices/{id}` | Update |
| PUT | `/invoices/{id}/payment` | Record payment (updates AmountPaid/Status) |
| DELETE | `/invoices/{id}` | Delete (Draft only) |

## Reports
| Method | Endpoint | Description |
|---|---|---|
| GET | `/reports/sales` | Sales report, date-ranged, filterable |
| GET | `/reports/projects` | Project status/progress summary |
| GET | `/reports/customers` | Customer activity/value summary |
| GET | `/reports/revenue` | Revenue breakdown, date-ranged |

## User Management
| Method | Endpoint | Description |
|---|---|---|
| GET | `/users` | Paginated |
| POST | `/users` | Create (invite) |
| PUT | `/users/{id}` | Update |
| PUT | `/users/{id}/status` | Activate/deactivate |
| GET | `/roles` | List roles + permission sets |
| PUT | `/roles/{id}/permissions` | Update role's permissions |
| GET | `/employees` | Paginated, filter by department |
| POST/PUT/DELETE | `/employees/{id}` | CRUD |

## Settings
| Method | Endpoint | Description |
|---|---|---|
| GET | `/settings/company` | Get company info |
| PUT | `/settings/company` | Update company info |
| GET | `/settings/general` | Get general settings |
| PUT | `/settings/general` | Update general settings |

## Conventions
- All list endpoints accept `?page=&pageSize=&search=&sortBy=&sortDir=` plus module-specific filters.
- Mutations return the created/updated resource (not just 204), so React Query can update its cache directly without a refetch.
- Soft-deletable entities never hard-delete via the API; `DELETE` sets `IsDeleted = true` and is excluded from all default queries.
