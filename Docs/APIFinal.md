# RTS ERP — API Design (Final)

Supersedes API.md. Adds Notifications and Search endpoints, formalizes DTO/validation/response conventions, and documents the authorization model concretely. All endpoints from API.md are unchanged unless listed in §7 (delta summary).

Base URL: `/api/v1`. JWT bearer auth on everything except `auth/login`, `auth/refresh`, `auth/forgot-password`, `auth/reset-password`.

## 1. Response Models

**List envelope** (backed by `Shared.Pagination.PaginatedList<T>`):
```json
{
  "items": [ /* T[] */ ],
  "totalCount": 137,
  "page": 1,
  "pageSize": 20
}
```

**Single resource:** the DTO directly, no wrapper — e.g. `GET /customers/{id}` returns a `CustomerDetailDto` body, not `{ data: {...} }`. Keeps the frontend's React Query hooks simple (`useQuery<CustomerDetailDto>`).

**Error (RFC 7807 ProblemDetails):**
```json
{
  "type": "https://rtserp.dev/errors/validation",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "CompanyName": ["CompanyName is required."] }
}
```
`NotFoundException` → 404, `ValidationException` → 400 with `errors`, unhandled → 500 with a generic title (no stack trace leaked), all mapped by the global exception middleware.

## 2. New: Notifications
| Method | Endpoint | Description |
|---|---|---|
| GET | `/notifications` | Current user's notifications, paginated, newest-first, `?unreadOnly=true` filter |
| PUT | `/notifications/{id}/read` | Mark one as read |
| PUT | `/notifications/read-all` | Mark all as read |
| GET | `/notifications/unread-count` | Lightweight count for the topbar badge (polled) |

## 3. New: Search
| Method | Endpoint | Description |
|---|---|---|
| GET | `/search?q=` | Top N matches across Customers, Projects, Tickets, Tasks — returns `{ type, id, title, subtitle, link }[]` grouped by type |

## 4. Updated: Auth
| Method | Endpoint | Description |
|---|---|---|
| POST | `/auth/login` | Unchanged contract; now backed by real `RefreshTokenService` — issues access token (body) + refresh token (httpOnly cookie, `RefreshTokens` row created) |
| POST | `/auth/refresh` | Reads refresh cookie, validates against `RefreshTokens`, rotates (revokes old, issues new), returns new access token |
| POST | `/auth/logout` | Revokes the current refresh token row |
| POST | `/auth/logout-all` | New — revokes all of the user's `RefreshTokens` rows ("log out of all devices") |

## 5. DTO & Validation Conventions

- **Naming:** `{Entity}Dto` for read shapes, `Create{Entity}Command`/`Update{Entity}Command` for writes — commands double as the request body shape, avoiding a separate `Create{Entity}Request` DTO that just gets mapped 1:1 into the command anyway.
- **Validation:** one `FluentValidation` validator class per Command, colocated in the same folder (`CreateCustomer/CreateCustomerCommandValidator.cs`). Validators check shape/format only (required fields, string lengths, valid enum values); cross-entity rules (e.g., "CustomerId must exist") are validated inside the handler where a DB check is already happening, not duplicated in the validator.
- **Mapping:** AutoMapper `Profile` classes per module, entity ⇄ DTO only — never DTO ⇄ DTO or entity ⇄ entity.

## 6. Authorization Model

Every controller action declares a required permission via a custom attribute wrapping `[Authorize]`:
```csharp
[HttpPost]
[RequirePermission("crm.customers.write")]
public async Task<ActionResult<CustomerDto>> Create(CreateCustomerCommand command)
```
`RequirePermissionAttribute` resolves to an `IAuthorizationHandler` that checks the `permission` claims embedded in the JWT at login (from `RolePermissions`, resolved once at token issuance — not re-queried from the DB per request, keeping authorization checks fast and stateless per request). A role change takes effect on the user's next login/refresh, not instantly — an accepted, documented tradeoff for a 25-user demo; flagged here so it isn't mistaken for a bug later.

## 7. Delta summary vs. API.md

- Added: `/notifications/*` (4 endpoints), `/search`.
- Added: `/auth/logout-all`.
- Formalized: response envelope/error shape (previously described in one line, now concrete), DTO/validation/mapping conventions, and the permission-claim authorization mechanism.
- No changes to any CRM/Quotations/Projects/Tasks/HelpDesk/Inventory/Invoices/Reports/Users/Settings endpoint — all stand as originally specified in API.md.

---

**Waiting for your approval to proceed to Step 5**: UI review — improved navigation, UX, layouts, and the complete navigation map, updated for Notifications and Search now having real endpoints behind them.
