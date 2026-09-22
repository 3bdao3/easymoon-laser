# Laser Clinic Migration Plan

| Attribute | Value |
|-----------|--------|
| Status | Active |
| Date | 2026-09-21 |
| Product | ErpClink → Laser Clinic Management System |

---

## 1. Existing architecture summary

ErpClink is a **modular monolith** (ASP.NET Core 9 + Angular 19 + SQL Server):

- **Host:** `src/Host/ErpClink.Api` — composition root, controllers, Swagger, JWT
- **BuildingBlocks:** Domain / Application / AspNetCore / Infrastructure (auth helpers, `AppException`, org context, storage)
- **Modules:** per-vertical Domain → Application → Infrastructure with **per-schema DbContexts** on one database
- **Frontend:** `frontend/erpclink-web` — feature folders, permission-gated routes, teal/slate design tokens in `styles.scss`

Registration pattern: `AddXxxModule` + `MigrateXxxModuleAsync` in `Program.cs`.

---

## 2. Existing modules

| Module | Schema / role | Laser decision |
|--------|---------------|----------------|
| Administration | Identity, JWT, RBAC | **Keep** (active) |
| Patients | Clinical patient registry | **Inactive** — replaced by Laser `Customer` |
| Doctors / Clinics / Specialties | Staff & rooms | **Inactive** |
| Scheduling | Doctor working hours / slots | **Inactive** — replaced by `ClinicSettings` + availability service |
| Appointments | Doctor/clinic bookings | **Inactive** — replaced by Laser appointments |
| Queue | Day-of queue | **Inactive** |
| MedicalVisits | Clinical encounters | **Inactive** |
| Prescriptions / Medications | Pharmacy | **Inactive** |
| Services / Packages | Billable catalog | **Inactive** — replaced by `LaserService` |
| Billing | Invoices/payments | **Inactive** (future extension) |
| Procurement / Inventory / Assets | ERP stock/assets | **Inactive** |
| Finance | GL / journals / AR-AP | **Inactive** |
| Reports | ERP reporting façade | **Inactive** — new slim laser reports |

Legacy module **projects remain in the solution** for reference and future extraction. They are **unregistered from the Host DI** and **removed from navigation/routes**.

---

## 3. Modules to keep (active runtime)

1. **Administration** — login, JWT, refresh, users/roles/permissions
2. **LaserClinic** (new) — Customers, LaserServices, Appointments, ClinicSettings, Dashboard, Availability

---

## 4. Modules to remove (from active experience)

All ERP/clinical modules listed as Inactive above:

- Not registered in `Program.cs`
- Host ProjectReferences removed for those Infrastructure projects
- Legacy controllers excluded from compile
- Angular routes/nav items removed
- Database schemas left in place (non-destructive); no drop migrations in this phase

---

## 5. Modules to refactor

| Area | Action |
|------|--------|
| Design system | Teal → pink/gray laser palette |
| Sidebar / routes | Laser-only Arabic nav |
| Dashboard | Today’s appointments + occupancy |
| Permissions | Add `LaserClinic.*` codes; seed for SuperAdmin / Receptionist |
| Api.Tests | New laser tests; legacy API tests excluded or retired |

---

## 6. Reusable infrastructure

- JWT + Identity + refresh tokens
- `AppException` + exception middleware
- `[HasPermission]` / permission claims
- FluentValidation patterns
- EF Core SQL Server + migrations pattern
- Angular auth guards, interceptors, layout shell, shared UI (buttons, cards, toast, confirm)
- BuildingBlocks auditable entities / aggregate roots

---

## 7. Database changes

**New schema:** `laser`

| Table | Purpose |
|-------|---------|
| `Customers` | Simple clinic customers |
| `LaserServices` | Configurable areas/durations |
| `Appointments` | Bookings with status lifecycle |
| `AppointmentServices` | Appointment ↔ LaserService lines |
| `ClinicSettings` | Working hours, slot interval, buffer |

Indexes: `Customers.PhoneNumber`, `Customers.FullName`, `Appointments.AppointmentDate`, `Appointments.CustomerId`, start/end times.

**Not dropped:** existing ERP schemas (safe retention).

---

## 8. Frontend changes

New features under `features/`:

- `customers`, `laser-appointments`, `calendar`, `laser-services`, `laser-reports`, `laser-settings`
- Guided **حجز جديد** wizard
- Replace `MAIN_NAV_ITEMS` with laser nav
- Replace routes in `app.routes.ts`
- Apply pink/gray tokens globally

---

## 9. Backend changes

- New module: `src/Modules/LaserClinic/{Domain,Application,Infrastructure}`
- Controllers: Customers, LaserServices, LaserAppointments, ClinicSettings, LaserDashboard
- Duration + availability + conflict logic in Application/Domain services (not controllers / Angular)
- Seed laser services + default clinic hours (16:00–22:00)

---

## 10. Risks / dependencies

| Risk | Mitigation |
|------|------------|
| Breaking Host by removing DI while controllers remain | Exclude legacy controllers from compile |
| Api.Tests depend on old modules | Exclude legacy tests; add LaserClinic unit + API tests |
| Existing DB has ERP data | Leave schemas; laser uses new schema |
| SuperAdmin missing new permissions | Seed `LaserClinic.*` and map to roles |
| Excel file not in repo | Business rules taken from master prompt; documented in BUSINESS_RULES |

---

## 11. Implementation order

1. Audit ✓  
2. This plan + architecture + business rules docs  
3. Create LaserClinic module (domain → app → infra → seed)  
4. Wire Host (Admin + LaserClinic only)  
5. Controllers + permissions  
6. Unit tests (duration, conflict, availability)  
7. Angular features + nav + pink theme  
8. Builds + final report  

---

## 12. Excel workflow mapping

Excel was not found in the repository. Mapping from the master prompt / typical laser Excel:

| Excel activity | App feature |
|----------------|-------------|
| Customer name/phone list | Customers |
| Service area + minutes | Laser Services |
| Manual duration sum | Auto duration service |
| Date + time columns | Appointments + availability |
| Status columns | AppointmentStatus enum |
| Daily sheet | Dashboard + Calendar day view |
| History per customer | Customer history API + UI |
