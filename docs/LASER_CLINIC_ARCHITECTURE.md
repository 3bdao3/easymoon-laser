# Laser Clinic Architecture

## Style

Modular monolith (unchanged). New vertical module **LaserClinic** owns the laser product domain. **Administration** remains the identity/RBAC boundary.

```
Angular SPA (RTL Arabic, pink/gray)
        │ JWT
        ▼
ErpClink.Api Host
  ├── Administration (auth, users)
  └── LaserClinic (customers, services, appointments, settings, dashboard)
```

Legacy ERP modules stay in the solution tree but are **not composed** into the host.

## LaserClinic layers

| Layer | Responsibility |
|-------|----------------|
| Domain | `Customer`, `LaserService`, `Appointment`, `AppointmentServiceLine`, `ClinicSettings`, status enum, overlap rule |
| Application | DTOs, validators, ports (`ICustomerService`, `ILaserServiceCatalog`, `ILaserAppointmentService`, `IClinicSettingsService`, `ILaserDashboardService`), `DurationCalculator`, `AvailabilityCalculator` |
| Infrastructure | EF Core `LaserClinicDbContext` (schema `laser`), services, seeder |
| Host | Thin REST controllers |

## Entities (normalized)

- **Customer** — FullName, PhoneNumber, Age?, Notes?, IsActive, audit
- **LaserService** — Name, MinDurationMinutes, MaxDurationMinutes, DisplayOrder, IsActive, Notes?
- **Appointment** — CustomerId, AppointmentDate, StartTime, EndTime, DurationMinutes, Status, Notes?, audit
- **AppointmentServiceLine** — AppointmentId, LaserServiceId, DurationMinutes (snapshot of recommended duration at booking)
- **ClinicSettings** — OpeningTime, ClosingTime, AppointmentSlotIntervalMinutes, DefaultBufferMinutes, IsActive

## Cross-cutting

- Permissions: `LaserClinic.Customers.*`, `LaserClinic.Services.*`, `LaserClinic.Appointments.*`, `LaserClinic.Settings.*`, `LaserClinic.Dashboard.View`, `LaserClinic.Reports.View`
- Errors: Arabic messages via `AppException`
- Future extension points: packages, payments, WhatsApp, multi-branch (OrganizationId can be added later without rewrite of core booking rules)

## API surface (v1)

| Method | Route |
|--------|-------|
| CRUD | `/api/v1/customers` |
| History | `/api/v1/customers/{id}/history` |
| CRUD | `/api/v1/laser-services` |
| CRUD + status | `/api/v1/laser-appointments` |
| Availability | `/api/v1/laser-appointments/availability` |
| Settings | `/api/v1/clinic-settings` |
| Dashboard | `/api/v1/laser-dashboard` |
