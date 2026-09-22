# Doctors & Clinics Module

| Attribute | Value |
|-----------|--------|
| Document | `docs/modules/doctors-and-clinics.md` |
| Module | Doctors |
| Schema | `doctors` |
| Migration | `AddDoctorsAndClinicsModule` |

---

## Doctor aggregate

`Doctor` is a business profile aggregate root (not an Identity user).

Owned fields include:

- Organization/branch boundaries
- Server-generated `DoctorNumber`
- Optional `UserId` link to `ApplicationUser`
- Name / display name / specialty / license / contact
- Active flag + audit metadata

Authentication credentials remain in Administration/Identity. This module never stores passwords and does not auto-create login accounts.

## Nurse (clinical staff)

`Nurse` is modeled as a **separate** entity/aggregate in the same module (not a premature `ClinicalStaff` base type).

- Distinct numbering and profile fields
- Same org/branch + active lifecycle pattern as Doctor
- Persistence table exists for future nurse APIs
- Nurse application services/APIs are **deferred** (foundation only in this step)

Future clinical staff types can be added as explicit entities without rewriting Doctor/Clinic.

## Specialty

Data-driven specialty catalog per organization:

- Code (unique per org), Name, Description, IsActive
- Doctors reference specialty by `SpecialtyId`
- Not a full medical taxonomy

## Clinic aggregate

`Clinic` represents a physical/logical place where future appointments/visits occur.

- Code unique per organization
- Name, description, location/room text
- Active lifecycle (inactive clinics must not receive new active doctor assignments)

Room/resource scheduling is out of scope.

## Doctor ↔ Clinic assignment

`DoctorClinicAssignment` is an explicit many-to-many relationship entity:

- A doctor may work in multiple clinics
- A clinic may have multiple doctors
- Active assignment uniqueness: filtered unique index on `(DoctorId, ClinicId) WHERE IsActive = 1`
- StartDate / optional EndDate (EndDate ≥ StartDate)
- Deactivate assignment instead of hard delete

## Doctor number strategy

- Abstraction: `IDoctorNumberGenerator`
- Default: `SequentialDoctorNumberGenerator`
- Temporary format: `D-{yyyy}-{000001}` per organization/year
- Unique per `(OrganizationId, DoctorNumber)`
- Generated server-side only; not based on DB identity

Format is intentionally replaceable.

## Lifecycle

### Doctor

```
Register (Active) → Update → Deactivate (Inactive) → Activate (Active)
```

Physical delete is not a normal operation.

### Clinic

```
Create (Active) → Update → Deactivate → Activate
```

Deactivating a clinic does not delete historical assignments.

## Domain rules

| Rule | Behavior |
|------|----------|
| Duplicate doctor number in org | DB unique + conflict |
| Duplicate clinic code in org | `409` (`clinics.code_exists`) |
| Inactive doctor → new assignment | `400` |
| Inactive clinic → new assignment | `400` |
| Duplicate active assignment | `409` |
| EndDate &lt; StartDate | `400` |
| Cross-org assignment | Clinic not found in org scope (`404`) / forbidden |
| Cross-org doctor/clinic get | `404` (no existence leak) |

## Authorization permissions

| Permission | Purpose |
|------------|---------|
| `Doctors.View` | Get/search doctors + list assignments |
| `Doctors.Create` | Register doctor |
| `Doctors.Update` | Update doctor |
| `Doctors.Activate` | Activate doctor |
| `Doctors.Deactivate` | Deactivate doctor |
| `Doctors.AssignClinic` | Assign / deactivate clinic assignment |
| `Clinics.View` | Get/search clinics |
| `Clinics.Create` | Create clinic |
| `Clinics.Update` | Update clinic |
| `Clinics.Activate` | Activate clinic |
| `Clinics.Deactivate` | Deactivate clinic |
| `Specialties.View` | List specialties |
| `Specialties.Manage` | Create/update/activate/deactivate specialties |

Permission checks are data-driven via `[HasPermission]`. No hard-coded role checks in controllers.

## Organization / branch boundaries

- `OrganizationId` / `BranchId` come from `IOrganizationContext` (config in V1), never from the client
- All queries are organization-scoped
- Foreign-org records return `404`

## Domain events

| Event | When |
|-------|------|
| `DoctorRegistered` | Doctor created |
| `DoctorActivated` | Doctor activated |
| `DoctorDeactivated` | Doctor deactivated |
| `DoctorAssignedToClinic` | Active assignment created |
| `ClinicCreated` | Clinic created |

Handlers for Scheduling/Appointments/notifications are intentionally not implemented yet. Events are logged + collected for tests.

## API surface

### Doctors

- `POST /api/v1/doctors`
- `GET /api/v1/doctors/{id}`
- `GET /api/v1/doctors/by-number/{doctorNumber}`
- `GET /api/v1/doctors/search`
- `PUT /api/v1/doctors/{id}`
- `POST /api/v1/doctors/{id}/activate`
- `POST /api/v1/doctors/{id}/deactivate`
- `GET /api/v1/doctors/{id}/clinics`
- `POST /api/v1/doctors/{id}/clinics`
- `POST /api/v1/doctors/{id}/clinics/{clinicId}/deactivate`

### Clinics

- `POST /api/v1/clinics`
- `GET /api/v1/clinics/{id}`
- `GET /api/v1/clinics/search`
- `PUT /api/v1/clinics/{id}`
- `POST /api/v1/clinics/{id}/activate`
- `POST /api/v1/clinics/{id}/deactivate`

### Specialties

- `GET /api/v1/specialties`
- `POST /api/v1/specialties`
- `PUT /api/v1/specialties/{id}`
- `POST /api/v1/specialties/{id}/activate`
- `POST /api/v1/specialties/{id}/deactivate`

## Unresolved decisions

1. Final doctor/clinic numbering format for production (current format is temporary).
2. Whether linking `UserId` should validate that the Identity user exists and belongs to the same org.
3. Whether nurse APIs ship in the same module next, or after Scheduling foundations.
4. Whether clinic `BranchId` can differ from the creator’s default branch when multi-branch UI exists.
5. Working hours / doctor schedule model (belongs to Scheduling, not this module).

## Out of scope (this step)

Scheduling, appointments, queue, medical visits, prescriptions, notifications, full Angular UI.
