# Appointments Module

| Attribute | Value |
|-----------|--------|
| Document | `docs/modules/appointments.md` |
| Module | Appointments |
| Schema | `appointments` |
| Migration | `AddAppointmentsModule` |

---

## Purpose

Appointments owns the **booking lifecycle**. Scheduling owns availability. Doctors/Patients own profiles.

## Appointment aggregate

Stores IDs only (PatientId, DoctorId, ClinicId) — no duplicated profile data.

Server-owned: OrganizationId, BranchId, AppointmentNumber, Status, audit, cancellation metadata.

## Appointment number

- Abstraction: `IAppointmentNumberGenerator`
- Default: `A-{yyyy}-{000001}` per organization/year
- Server-generated; never accepted from client

## Status lifecycle (this step)

```
Scheduled ──confirm──► Confirmed ──no-show──► NoShow
    │                      │
    └────── cancel ────────┴──► Cancelled

Reschedule: in-place slot change while Scheduled or Confirmed (history retained)
```

**V1 statuses:** Scheduled → Confirmed → Cancelled / NoShow; Queue check-in → CheckedIn.

**Deferred (Medical Visits):** Completed, InProgress.

## Booking workflow

1. Validate request
2. Reject past date/time (`IBusinessClock`)
3. Patient/doctor/clinic active + same org via `IPatientLookup` / `IDoctorClinicLookup`
4. Active doctor↔clinic assignment on date
5. Slot must exist in `IScheduleAvailabilityService` result (exact Start/End)
6. Pre-check no active booking on slot
7. Transaction: generate number + insert
8. Unique filtered index enforces concurrency safety

## Double-booking / concurrency strategy

**Application:** reject if active appointment exists for Doctor+Clinic+Date+StartTime.

**Database (authoritative across instances):**

Filtered unique index `IX_Appointments_ActiveSlot` on  
`(OrganizationId, DoctorId, ClinicId, AppointmentDate, StartTime)`  
`WHERE Status IN ('Scheduled', 'Confirmed', 'CheckedIn')`

Cancelled / NoShow release the slot. CheckedIn keeps the slot until a later medical/visit flow releases it. Concurrent inserts → one success, others `409 appointments.slot_conflict`.

Model uses **fixed schedule slots** (from Scheduling), so uniqueness on exact StartTime is sufficient (no variable-duration interval overlap needed).

## Timezone

`IBusinessClock` / `UtcBusinessClock`: development default treats **UTC as clinic business time**. Org timezone settings remain unresolved.

## Permissions

| Code | Operation |
|------|-----------|
| `Appointments.View` | Get/search |
| `Appointments.Create` | Book |
| `Appointments.Confirm` | Confirm |
| `Appointments.Cancel` | Cancel |
| `Appointments.Reschedule` | Reschedule |
| `Appointments.NoShow` | Mark no-show |

`Appointments.Update` remains seeded for future demographic-style edits; booking mutations use explicit commands.

## Domain events

`AppointmentBooked`, `AppointmentConfirmed`, `AppointmentCancelled`, `AppointmentRescheduled`, `AppointmentMarkedNoShow`, `AppointmentCheckedIn`

Check-in is invoked by the Queue module via `IAppointmentCheckInPort` (application contract). Queue owns queue state; Appointments owns the CheckedIn transition.

Outbox/notifications deferred (log + test collector only).

## API

- `POST /api/v1/appointments`
- `GET /api/v1/appointments/{id}`
- `GET /api/v1/appointments/by-number/{appointmentNumber}`
- `GET /api/v1/appointments/search`
- `POST /api/v1/appointments/{id}/confirm|cancel|reschedule|no-show`

## Unresolved decisions

1. Org timezone for past/same-day rules
2. Auto-confirm policy
3. Overbook permission (BR-APT-03)
4. Whether availability API should subtract booked slots (currently booking-time conflict only)
5. Check-in / Complete ownership with Queue & Visits

## Out of scope

Queue, check-in, medical visits, prescriptions, finance, notifications, UI.
