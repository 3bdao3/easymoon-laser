# Authentication & Authorization

| Attribute | Value |
|-----------|--------|
| Document | `docs/authentication.md` |
| Related | `docs/architecture.md` (ADR-002, ADR-003) |
| Module | Administration |

---

## Overview

V1 uses **ASP.NET Core Identity (IdentityCore)** + **JWT bearer access tokens** + **rotating hashed refresh tokens**, with **data-driven permissions** bound to roles.

Authorization is **API-authoritative**. Angular guards are UX only.

---

## Authentication flow

### Login

`POST /api/auth/login`

```json
{ "email": "admin@erpclink.local", "password": "..." }
```

Success `200`:

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresAtUtc": "...",
  "user": {
    "id": "...",
    "email": "...",
    "userName": "...",
    "fullName": "...",
    "isActive": true,
    "roles": ["SuperAdmin"],
    "permissions": ["Administration.Users.View", "..."]
  }
}
```

Failures:

- `400` invalid request
- `401` invalid credentials / inactive / locked out

### Access token

- Short-lived JWT (default **15 minutes**, configurable)
- Claims include: user id, email/name, roles, permission codes (`permission` claim)
- No passwords, hashes, or secrets in claims
- Validated via issuer, audience, signing key, lifetime

### Refresh token

- Opaque random token returned to client
- **SHA-256 hash** stored in `admin.RefreshTokens`
- Default lifetime **7 days** (configurable)
- On refresh: old token revoked, new token issued (rotation)
- Reuse of a revoked token revokes all active tokens for that user

`POST /api/auth/refresh`

```json
{ "refreshToken": "..." }
```

### Logout

`POST /api/auth/logout`

```json
{ "refreshToken": "..." }
```

Revokes the refresh token. Access tokens remain valid until expiry (keep TTL short).

---

## Roles

Seeded (configurable / evolvable):

- SuperAdmin, Admin, Receptionist, Doctor, Nurse
- Accountant, Pharmacist, InventoryManager, ProcurementManager

Roles are Identity roles (`ApplicationRole`). They are not the sole authorization mechanism.

---

## Permissions

Permissions are rows in `admin.Permissions`:

- Code (unique), Name, Module, Description, IsActive

Linked via `admin.RolePermissions` (many-to-many).

Examples:

- `Administration.Users.View|Create|Update|Delete`
- `Patients.View|Create|Update|Delete`
- `Appointments.View|Create|Update|Cancel`
- `Finance.Invoices.View|Create`, `Finance.Payments.Create`
- `Inventory.Items.View`, `Inventory.Stock.Adjust`

Effective permissions = union of permissions for the user’s roles.

---

## Authorization flow

1. Request carries `Authorization: Bearer <accessToken>`
2. JWT authentication populates claims (including `permission`)
3. `[HasPermission("Patients.View")]` maps to dynamic policy `Permission:Patients.View`
4. `PermissionAuthorizationHandler` succeeds only if claim exists
5. Missing auth → `401`; authenticated without permission → `403`

Cross-cutting abstraction: `ICurrentUser` (BuildingBlocks) — do not use `HttpContext` from domain/application modules.

---

## User management foundation

Protected admin endpoints (`/api/admin/users`):

| Method | Path | Permission |
|--------|------|------------|
| GET | `/api/admin/users` | Administration.Users.View |
| GET | `/api/admin/users/{id}` | Administration.Users.View |
| POST | `/api/admin/users` | Administration.Users.Create |
| PUT | `/api/admin/users/{id}` | Administration.Users.Update |
| POST | `/api/admin/users/{id}/activate` | Administration.Users.Update |
| POST | `/api/admin/users/{id}/deactivate` | Administration.Users.Update |
| POST | `/api/admin/users/{id}/roles` | Administration.Roles.Manage |
| DELETE | `/api/admin/users/{id}/roles/{roleName}` | Administration.Roles.Manage |

---

## Seed configuration

Idempotent seed on startup:

1. Migrate database
2. Seed roles
3. Seed permissions
4. Seed role↔permission map
5. Seed SuperAdmin **only if** credentials configured

### Configuration

```json
{
  "Authentication": {
    "Jwt": {
      "Issuer": "ErpClink",
      "Audience": "ErpClink.Client",
      "SigningKey": "<long-random-secret-min-32-chars>",
      "AccessTokenMinutes": 15,
      "RefreshTokenDays": 7
    },
    "InitialAdmin": {
      "Email": "admin@erpclink.local",
      "Password": "<from-env-or-user-secrets>",
      "FullName": "System Super Admin"
    }
  }
}
```

### Environment variables (recommended for secrets)

```text
ConnectionStrings__DefaultConnection=...
Authentication__Jwt__SigningKey=...
Authentication__InitialAdmin__Email=...
Authentication__InitialAdmin__Password=...
```

**Do not commit production secrets.** `appsettings.json` may contain local-dev placeholders only.

---

## Swagger authentication

1. Run API in Development
2. Open Swagger UI
3. `POST /api/auth/login`
4. Copy `accessToken`
5. Click **Authorize** → `Bearer <token>`
6. Call protected endpoints

---

## Security decisions

| Decision | Choice |
|----------|--------|
| AuthN | IdentityCore + JWT (ADR-002) |
| AuthZ | Roles → Permissions + `[HasPermission]` (ADR-003) |
| Refresh storage | Hashed only |
| Passwords | Identity hasher; never returned |
| API vs UI | Server enforces permissions |
| Identity hosting | IdentityCore (no cookie scheme conflict with JWT API) |

---

## Migration

```bash
dotnet ef migrations add InitialIdentityAndAuthorization ^
  --project src/Modules/Administration/ErpClink.Modules.Administration.Infrastructure ^
  --startup-project src/Host/ErpClink.Api ^
  --context AdministrationDbContext ^
  --output-dir Persistence/Migrations
```

Applied automatically on API startup via seeder (`Database.Migrate()`).

---

## Probe endpoints

- `GET /api/v1/secure/me` — authenticated current user claims summary
- `GET /api/v1/secure/patients-view` — requires `Patients.View`
