# Medical Visits Module

| Attribute | Value |
|-----------|--------|
| Document | `docs/modules/medical-visits.md` |
| Module | MedicalVisits |
| Schema | `medical_visits` |
| Migration | `AddMedicalVisitsModule` |

---

## Responsibility

Clinical encounter source of truth for a checked-in appointment.

**Queue.Completed ≠ MedicalVisit.Completed**

| Concept | Meaning |
|---------|---------|
| Queue.Completed | Patient left the operational queue/service stage |
| MedicalVisit.Completed | Clinical consultation finished |

## Domain

`MedicalVisit` aggregate:

Statuses: `Open` → `InProgress` → `Completed`  
Also: `Open|InProgress` → `Cancelled`  
`Completed → Cancelled` is rejected (no reversal in V1).

Clinical fields (extensible, not full EMR):

- ChiefComplaint
- ClinicalNotes
- ExaminationFindings
- DiagnosisNotes
- FollowUpNotes

Vital signs are **not** implemented in this step (future clinical extension).

Walk-ins without appointment are **unresolved** / out of scope.

## State machine

```
Open → InProgress → Completed
Open → Completed
Open → Cancelled
InProgress → Completed | Cancelled
```

Updating clinical notes on `Open` automatically moves to `InProgress`.

## Start flow

`POST /api/v1/medical-visits/start`

1. `IAppointmentLookup` — must exist, org/branch match, status **CheckedIn**
2. `IPatientLookup` — patient exists in org
3. `IQueueVisitPort` — active queue for appointment (or explicit QueueEntryId)
4. Queue must be Waiting / Called / InService; port ensures **InService** (Waiting→Called→InService if needed)
5. Create visit + VisitNumber; filtered unique index prevents duplicate active visit per appointment

## Visit numbering

Scope: **Organization + Year**  
Format: `V-{yyyy}-{000001}`  
Table: `VisitNumberSequences` with UPDLOCK allocation  
Unique: `(OrganizationId, VisitNumber)`

## Concurrency

| Invariant | Protection |
|-----------|------------|
| One active visit per appointment | Filtered unique `IX_MedicalVisits_ActiveAppointment` |
| Unique visit numbers | Sequence + unique index |
| Clinical notes updates | `RowVersion` → `409 medical_visits.concurrency_conflict` |

## Integration contracts

- `IAppointmentLookup` (Appointments.Application)
- `IQueueVisitPort` (Queue.Application)
- `IPatientLookup` (Patients.Application)
- No Infrastructure references to Appointments / Queue / Patients / Doctors

## Permissions

`MedicalVisits.View|Create|Update|Complete|Cancel`

Doctor role: full set. Receptionist/Nurse: View. Admin/SuperAdmin: all.

## APIs

- `POST /api/v1/medical-visits/start`
- `GET /api/v1/medical-visits/{id}`
- `GET /api/v1/medical-visits/by-number/{visitNumber}`
- `GET /api/v1/medical-visits/patients/{patientId}/history`
- `GET /api/v1/medical-visits/search`
- `PUT /api/v1/medical-visits/{id}/clinical-notes`
- `POST /api/v1/medical-visits/{id}/complete`
- `POST /api/v1/medical-visits/{id}/cancel`

## Domain events

`MedicalVisitStarted|ClinicalNotesUpdated|Completed|Cancelled`  
Outbox deferred (log + test collector).

## Unresolved

- Walk-in visits without appointment
- Vital signs capture
- Diagnosis coding / ICD
- Required clinical fields for completion (currently optional notes)
- Org timezone (UTC business clock)
- Doctor-scoped visit visibility beyond RBAC permissions

## Out of scope

Prescriptions, billing, inventory, notifications, Angular clinical UI.
