# Laser Clinic Business Rules

## Duration

- Durations are stored as **minutes** (integers), never strings.
- Each `LaserService` has `MinDurationMinutes` and `MaxDurationMinutes` (`Max >= Min > 0`).
- **Recommended booking duration** (backend source of truth):
  - If `Min == Max` → use that value.
  - If ranged → use `MaxDurationMinutes` (conservative booking so sessions are not over-packed).
- Appointment total duration = **sum** of recommended durations for selected services (+ optional buffer applied only for scheduling math when configured).

## Buffer

- `ClinicSettings.DefaultBufferMinutes` defaults to `0` (optional comfort gap).
- When > 0, availability and conflict checks use `DurationMinutes + Buffer` as the occupied block length for **new** bookings; stored `DurationMinutes` remains the clinical session length without forcing buffer into the persisted duration unless product later requires it.
- Current implementation: buffer extends the **occupied end** used for conflict/availability (`End = Start + duration + buffer`) while `Appointment.DurationMinutes` stores the clinical sum; `EndTime` on the appointment stores clinical end (`Start + DurationMinutes`). Occupied window for conflicts uses `EndTime + buffer` vs other appointments' occupied windows.

**Simplification used in code (documented):** conflict and availability treat occupied interval as `[Start, End]` where `End = StartTime + DurationMinutes`. Buffer is applied by adding it into the **requested booking duration** when computing availability slots and when validating create (`effectiveDuration = duration + buffer`). The appointment stores clinical `DurationMinutes` and `EndTime = Start + clinical duration`; the **blocked** time uses effective duration so slots do not pack tighter than buffer allows.

## Working hours

- Default seed: **16:00 → 22:00**
- Slot interval seed: **15 minutes**
- Slots must fully fit inside `[OpeningTime, ClosingTime)`.

## Conflict rule

Two appointments conflict when both occupy the schedule and:

```
ExistingStart < NewEnd AND ExistingEnd > NewStart
```

Occupying statuses: `Pending`, `Confirmed`, `Attended`  
Non-occupying: `Cancelled`, `NoShow`

API rejects conflicts with: `هذا الموعد متعارض مع موعد آخر.`

## Appointment statuses

| Status | Meaning |
|--------|---------|
| Pending | Booked, not yet confirmed |
| Confirmed | Confirmed with customer |
| Attended | Session completed / customer attended |
| Cancelled | Cancelled |
| NoShow | Did not attend |

## Customers

- Search by name and phone.
- Duplicate phone among active customers → `رقم الهاتف مستخدم بالفعل.`
- Soft deactivate via `IsActive` (no hard delete of history).

## Seeded laser services

| Name | Min | Max |
|------|-----|-----|
| Face & Neck | 10 | 10 |
| Bikini | 5 | 5 |
| Bikini + Underarm + Line | 15 | 15 |
| Half Arm | 15 | 15 |
| Full Arm | 25 | 25 |
| Half Leg | 20 | 20 |
| Upper Half Leg | 30 | 30 |
| Full Leg | 30 | 45 |
| Half Body | 30 | 40 |
| Full Body without abdomen/back | 60 | 60 |
| Full Body + Abdomen + Back | 60 | 75 |
| Full Body + Abdomen + Back + Face & Neck | 75 | 90 |

("30 hours" typo in source data → **30 minutes**.)

## Excel notes

No Excel file was present in the repo. Rules above encode the described Excel replacement workflow.

**Availability example note:** An illustrative gap of “05:00–05:45” next to an existing “05:30–06:15” booking would violate the documented overlap rule; the implemented calculator correctly rejects such slots and only returns non-overlapping windows.
