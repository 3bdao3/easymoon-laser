# Patients Module

| Attribute | Value |
|-----------|--------|
| Document | `docs/modules/patients.md` |
| Module | Patients |
| Schema | `patients` |

---

## Patient aggregate

`Patient` is the aggregate root. It owns:

- Demographics and primary contact fields
- Optional address + emergency contact fields (on the aggregate for V1 simplicity)
- `PatientAllergy` children
- `PatientMedicalHistoryItem` children (patient-level history only — not visit records)

Server-owned fields: `OrganizationId`, `BranchId`, `PatientNumber`, audit metadata (`Created*` / `Updated*`).

## Lifecycle

```
Register (Active)
  → Update demographics / allergies / history
  → Deactivate (Inactive)
  → Activate (Active)
```

Physical delete is not a normal operation.

## Registration workflow

1. Validate request (FluentValidation)
2. Resolve `OrganizationId` / `BranchId` from `IOrganizationContext` (config — never client)
3. Enforce NationalId uniqueness within organization when provided
4. Detect possible duplicates (phone OR name+DOB); return `409` unless `AllowPossibleDuplicate=true`
5. Generate `PatientNumber` via `IPatientNumberGenerator`
6. Persist patient
7. Dispatch `PatientRegisteredDomainEvent` (in-process; outbox deferred)

## Patient number strategy

- Abstraction: `IPatientNumberGenerator`
- Default implementation: `SequentialPatientNumberGenerator`
- Temporary format: `P-{yyyy}-{000001}` per organization/year
- Generated server-side only
- Unique per `(OrganizationId, PatientNumber)`

Format is intentionally replaceable without changing the aggregate.

## Search

`GET /api/v1/patients/search` supports:

- Free-text `query` (number, name, phone, national id)
- Field filters
- Pagination
- Whitelisted sorting (`patientNumber`, `firstName`, `lastName`, `phone`, `dob`)
- Organization-scoped queries only

## Duplicate handling

| Rule | Behavior |
|------|----------|
| Same NationalId in org | Hard conflict `409` (`patients.national_id_exists`) + unique filtered index |
| Same phone OR same first+last+DOB | Soft conflict `409` (`patients.possible_duplicate`) unless `AllowPossibleDuplicate=true` |

National ID required? **Unresolved** (product open question). When null, uniqueness constraint does not apply.

Patient merge: **Out of scope for V1**.

## Allergies

- Nested under patient
- Fields: Name, Reaction, Severity (`AllergySeverity` enum), Notes, IsActive, audit
- Deactivate instead of hard delete
- Inactive patients cannot receive new allergies / demographic updates

## Patient visit context (derived)

Patient 360 shows **derived** relationship / treatment / encounter labels from Medical Visits history via  
`GET /api/v1/medical-visits/patients/{patientId}/context`.

These are **not** stored on `Patient` and must not be confused with administrative `IsActive` (نشط / موقوف):

| Concept | Source |
|---------|--------|
| Admin file state | `Patient.IsActive` |
| New / Returning | Derived from non-cancelled visits |
| In treatment / Treatment completed | Derived from Open/InProgress vs Completed visits |
| First visit / Follow-up / Consultation | Derived encounter context for an **active** visit |

Completing treatment on Patient 360 completes the **active medical visit** (`POST .../medical-visits/{id}/complete`) — it does not deactivate the patient.



Patient-level medical file metadata lives in `PatientDocument` (schema `patients`). Binary content is stored via BuildingBlocks `IFileStorage` (local filesystem by default; object storage later).

Nested routes under `/api/v1/patients/{patientId}/documents`:

| Permission | Use |
|------------|-----|
| `Patients.Documents.View` | List/summary/download/preview |
| `Patients.Documents.Manage` | Upload, edit metadata, deactivate |

Deactivate soft-deletes metadata (`IsActive=false`); storage bytes are retained for audit. `MedicalVisitId` is optional for future visit linkage (no cross-module FK).

## Authorization permissions

| Permission | Use |
|------------|-----|
| `Patients.View` | Get/search |
| `Patients.Create` | Register |
| `Patients.Update` | Update demographics |
| `Patients.Activate` | Activate |
| `Patients.Deactivate` | Deactivate |
| `Patients.Allergies.View` | List allergies |
| `Patients.Allergies.Manage` | Add/update/deactivate allergies |
| `Patients.Documents.View` | List/download patient documents |
| `Patients.Documents.Manage` | Upload/edit/deactivate documents |
| `Patients.Delete` | Legacy seeded; not used by current endpoints |

## Organization / branch boundaries

- All reads/writes filter by `IOrganizationContext.OrganizationId`
- Cross-org access returns **404** (no existence leak)
- Client cannot supply OrganizationId/BranchId/CreatedBy/UpdatedBy

## Audit behavior

- Set from `ICurrentUser` + `IDateTimeProvider`
- Never accepted from request body

## Domain events

`PatientRegisteredDomainEvent` contains:

- PatientId, OrganizationId, BranchId, PatientNumber, OccurredOnUtc

Dispatched in-process after successful save. **Outbox not implemented in this step** (documented integration point for ADR-012).

## Unresolved decisions

1. Final MRN/patient-number business format
2. Whether National ID is mandatory
3. Exact duplicate-matching policy thresholds
4. Patient merge workflow
5. Shared vs branch-scoped patient master (currently org-shared + registration BranchId)
6. Whether emergency contact / address should become separate entities later
7. Full AuditLog module (foundation audit fields only here)

## API endpoints

See Host `PatientsController` under `/api/v1/patients`.
