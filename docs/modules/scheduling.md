# Scheduling Module

| Attribute | Value |
|-----------|--------|
| Document | `docs/modules/scheduling.md` |
| Module | Scheduling |
| Schema | `scheduling` |
| Migration | `AddSchedulingModule` |

---

## Purpose

Scheduling owns **doctor working hours** and the **availability/slot engine** that future Appointments will consume.

It does **not** own appointments, queue tickets, or medical visits.

Doctors/Clinics remain owned by the Doctors module. Scheduling stores `DoctorId` / `ClinicId` and validates via `IDoctorClinicLookup`.

## Domain

### DoctorWorkingSchedule

Recurring working period for a doctor at a clinic:

- `DayOfWeek`, `StartTime`, `EndTime` (`TimeOnly` — **clinic local business time**, not UTC)
- `SlotDurationMinutes` (5–240)
- `EffectiveFrom` / optional `EffectiveTo`
- Active lifecycle + audit

Multiple non-overlapping periods on the same day are allowed (e.g. 09–13 and 17–21).

### ClinicHoliday (minimum SCH-03)

Org (optionally branch) closed date. Availability returns no slots when the date is an active holiday.

Holiday management APIs are intentionally thin / deferred; persistence + availability exclusion exist as the extension point.

### DoctorScheduleException (minimum SCH-04)

- `Unavailable` — suppresses slots for that doctor/date
- `ExtraHours` — adds an extra working period for that date

Full leave-management UX is deferred; domain model supports availability overrides.

## Slot generation

- Abstraction: `IScheduleAvailabilityService`
- Calculator: `SlotGenerator` (deterministic, in-memory)
- Slots are **not persisted**
- A slot is emitted only when the full duration fits inside the working window

Example: 09:00–13:00 / 30m → 09:00 … 12:30 (8 slots).  
Example: 09:00–10:15 / 30m → 09:00, 09:30 (10:00 excluded).

## Overlap rules

Validated in application/domain (not only DB unique indexes):

Same Doctor + Clinic + DayOfWeek + overlapping effective date ranges + overlapping local times → `409 scheduling.overlap`.

## Lifecycle

```
Create (Active) → Update → Deactivate → Activate
```

No physical delete as a normal operation.

## Cross-module contract

`IDoctorClinicLookup` (Doctors.Application):

- Doctor/clinic existence + active flag (org-scoped)
- Active `DoctorClinicAssignment` covering the effective date

## Organization / branch

- `OrganizationId` / `BranchId` from `IOrganizationContext`
- Foreign doctor/clinic → `404`
- Cross-org combination forbidden

## Timezone assumptions

| Concept | Representation |
|---------|----------------|
| Recurring working hours | `DayOfWeek` + `TimeOnly` (local clinic time) |
| Audit timestamps | UTC DateTime |
| Appointment instants | Deferred to Appointments module |

## Permissions

| Code | Use |
|------|-----|
| `Scheduling.View` | Get/list schedules |
| `Scheduling.Create` | Create schedule |
| `Scheduling.Update` | Update schedule |
| `Scheduling.Activate` | Activate |
| `Scheduling.Deactivate` | Deactivate |
| `Scheduling.Availability.View` | Availability query |

## Domain events

- `DoctorScheduleCreated`
- `DoctorScheduleUpdated`
- `DoctorScheduleActivated`
- `DoctorScheduleDeactivated`

No Appointment/Notification handlers yet.

## API

- `POST /api/v1/scheduling/schedules`
- `GET /api/v1/scheduling/schedules/{id}`
- `GET /api/v1/scheduling/doctors/{doctorId}/schedules`
- `PUT /api/v1/scheduling/schedules/{id}`
- `POST /api/v1/scheduling/schedules/{id}/activate`
- `POST /api/v1/scheduling/schedules/{id}/deactivate`
- `GET /api/v1/scheduling/doctors/{doctorId}/availability?date=&clinicId=`

## Future appointment integration

Appointments should call:

`IScheduleAvailabilityService.GetAvailabilityAsync(doctorId, date, clinicId)`

then subtract booked slots / blocks. Double-booking enforcement belongs to Appointments (SCH-05 partial).

## Unresolved decisions

1. Org timezone settings for interpreting local `TimeOnly` across branches.
2. Whether holidays require dedicated manage permissions (`Holidays.Manage`) vs Scheduling.* .
3. Full holiday/exception management APIs and policies (override book on holiday).
4. Manual slot blocks (SCH-06 — Should).
5. Overbooking policy.

## Out of scope

Appointments, queue, visits, prescriptions, notifications, calendar UI.
