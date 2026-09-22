# Prescriptions & Medications Module

| Attribute | Value |
|-----------|--------|
| Document | `docs/modules/prescriptions.md` |
| Module | Prescriptions |
| Schema | `prescriptions` |
| Migration | `AddPrescriptionsModule` |

---

## Responsibility

Doctor-authored prescriptions tied to a Medical Visit, plus an organization medication catalog.

Medical Visit owns the clinical encounter. Prescription owns prescription data. Inventory/Pharmacy will consume medication references later — **not** implemented here.

## Domain

### Prescription
Statuses: `Draft` → `Issued`; `Draft|Issued` → `Cancelled`  
Issued prescriptions are immutable (header + items).

### PrescriptionItem
Owned by Prescription. `MedicationNameSnapshot` preserves display name at prescribe time.

### Medication
Org-scoped catalog: Code, Name, GenericName, Strength, DosageForm, Route, IsActive. Prefer deactivate over delete.

## Business rules (V1)

| Rule | Decision |
|------|----------|
| One active prescription per Medical Visit | Enforced (Draft/Issued unique index) |
| Walk-in without Medical Visit | Not supported (unresolved) |
| Issued cancellation | Allowed (wrong-issue correction); no reinstatement |
| Medication catalog | Organization-scoped |

## Medical Visit integration

`IMedicalVisitPrescriptionPort` (MedicalVisits.Application)  
PatientId/DoctorId taken from visit — never trusted from client.  
Visit must not be Cancelled.

## Numbering

`RX-{yyyy}-{000001}` per organization via `PrescriptionNumberSequences` + UPDLOCK.

## Concurrency

- Unique active prescription per visit → 409
- Unique prescription numbers (UPDLOCK + PK-race retry)
- Draft updates: `RowVersion` → 409
- Empty issue rejected with **400** (not a state transition conflict)

## Permissions

Prescriptions: `View|Create|Update|Issue|Cancel`  
Medications: `View|Create|Update|Activate|Deactivate`

Doctor: full set. Receptionist/Nurse: View.

## APIs

Prescriptions: create, get, by-number, search, patient history, update, items CRUD, issue, cancel  
Medications: create, get, search, update, activate, deactivate

## Unresolved / future

- Walk-in prescriptions
- Refills / renewals / expiry
- Controlled substances workflow
- PDF printing
- Pharmacy dispensing / Inventory stock check
- Required clinical notes before issue

## Out of scope

Billing, Inventory, Procurement, Notifications, Angular UI, Outbox.
