# ErpClink Angular Frontend

| Attribute | Value |
|-----------|--------|
| App | `frontend/erpclink-web` |
| Framework | Angular 19 (standalone) |
| API | ASP.NET Core Modular Monolith via `/api` proxy |

---

## Architecture

```
src/app/
  core/           auth, guards, interceptors, permissions, shared services
  layout/         main shell, sidebar, topbar
  shared/         UI primitives, directives, models, utils
  features/       lazy-loaded domain modules
```

Business orchestration and HTTP live in feature `*.api.ts` / `*-api.service.ts` files. Components stay presentation-focused.

## Authentication

1. `POST /api/auth/login` → store access + refresh tokens + user (roles/permissions) via `TokenStorage` (localStorage).
2. `authInterceptor` attaches `Authorization: Bearer`.
3. On **401**: single shared refresh (`POST /api/auth/refresh`), retry original request once (`X-Auth-Retry`), logout + navigate to `/login` if refresh fails.
4. Login/refresh URLs are never token-attached or refresh-retried in a loop.
5. `authGuard` protects `/app/**`; `guestGuard` keeps authenticated users off `/login`.
6. `permissionGuard(code)` protects feature routes; sidebar uses the same permission codes for visibility.

**UI permissions are not security.** Backend `HasPermission` remains authoritative.

## Permissions

Codes match backend `PermissionCodes` (e.g. `Patients.View`, `Prescriptions.Issue`).  
Helpers: `PermissionService`, `*appHasPermission` structural directive.

## API communication

- Dev proxy: `proxy.conf.json` → `http://localhost:5284`
- `environment.apiBaseUrl` is empty; requests use `/api/...`
- Errors: `application/problem+json` → `ApiErrorService` + toast (Arabic defaults for 401/403/409)

## Error handling

| Status | Default UX |
|--------|------------|
| 400 | Validation / bad request detail |
| 401 | Session expired → login |
| 403 | No permission message |
| 404 | Not found |
| 409 | Conflict / concurrency refresh hint |
| 500 | Generic server error |

## Feature modules

| Feature | Routes (under `/app`) | Status |
|---------|----------------------|--------|
| Dashboard | `dashboard` | Shell + permission shortcuts (no fake KPIs) |
| Patients | `patients` | List/create/edit/detail/allergies/activate |
| Doctors | `doctors` | List/create/edit/detail/clinic assign |
| Clinics | `clinics` | List/create/edit/activate |
| Specialties | `specialties` | List/create/edit/activate |
| Scheduling | `scheduling` | Schedules + availability |
| Appointments | `appointments` | List/wizard/detail + transitions |
| Queue | `queue` | Today board + check-in + actions |
| Medical visits | `medical-visits` | Start/list/detail/history/notes |
| Prescriptions | `prescriptions` | Draft/issue/cancel + medications |
| Medications | `medications` | Catalog CRUD-ish + activate |
| Administration | `administration` | Users basic management |

## How to run

```bash
# Terminal 1 — API (http://localhost:5284)
dotnet run --project src/Host/ErpClink.Api

# Terminal 2 — Angular
cd frontend/erpclink-web
npm install
npm start
```

Open `http://localhost:4200`.

## How to build

```bash
cd frontend/erpclink-web
npm run build
```

## How to test

```bash
cd frontend/erpclink-web
npx ng test --watch=false --browsers=ChromeHeadless
```

## Known limitations

- No dashboard analytics APIs yet (empty states only).
- Display names for patient/doctor/clinic on lists often show IDs unless separately loaded (no aggregate list DTOs yet).
- Prescription PDF printing not implemented (backend deferred).
- No Billing / Inventory / Notifications / WhatsApp UI.
- RTL is default (`lang=ar` `dir=rtl`); full bilingual toggle not built.
- Tokens in `localStorage` — acceptable for V1 clinic LAN apps; harden for stricter threat models later.

## Backend dependencies (not invented on FE)

- Dashboard metrics endpoints
- Aggregate “display name” joins on list APIs (optional enhancement)
- Prescription document/PDF pipeline
