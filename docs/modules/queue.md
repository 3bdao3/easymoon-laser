# Queue Module

| Attribute | Value |
|-----------|--------|
| Document | `docs/modules/queue.md` |
| Module | Queue |
| Schema | `queue` |
| Migration | `AddQueueModule` |

---

## Responsibility

Day-of clinic flow: check-in → waiting → call → in service → leave queue.

Does **not** own medical documentation. `QueueStatus.Completed` means the patient left the queue/service stage. Clinical completion belongs to Medical Visits.

## Domain

`QueueEntry` aggregate with statuses:

| Status | Meaning |
|--------|---------|
| Waiting | Checked in |
| Called | Called to room |
| InService | Service started |
| Completed | Left queue/service stage |
| Skipped | Called/waiting but not present (terminal; no requeue in V1) |
| Cancelled | Cancelled from queue (terminal) |

Priority: `Normal` (default) / `Urgent` — triage policy unresolved.

## State machine

```
Waiting → Called → InService → Completed
Waiting → Skipped | Cancelled
Called → InService | Skipped | Cancelled
InService → Completed | Cancelled
```

No requeue from Skipped unless product defines it later.

## Check-in flow

1. Load appointment via `IAppointmentLookup`
2. Eligibility: org match; status Scheduled/Confirmed; appointment date = business today
3. Transaction: generate queue number + insert `QueueEntry` + `IAppointmentCheckInPort.CheckInAsync`
4. Appointment module owns `CheckedIn` transition

Unresolved: early/late check-in window — currently **same calendar date only** (`IBusinessClock.Today`).

## Queue numbering

Scope: **Organization + Branch + Clinic + Date**  
Format: `Q-{yyyyMMdd}-{0001}`  
DB unique: `(OrganizationId, BranchId, QueueDate, ClinicId, QueueNumber)`  
Sequence table: `QueueNumberSequences`

## Concurrency

| Invariant | Protection |
|-----------|------------|
| One active entry per appointment | Filtered unique `IX_QueueEntries_ActiveAppointment` |
| Unique queue numbers | Sequence row + unique index |
| Call / transitions | `RowVersion` → `409 queue.concurrency_conflict` |

## Integration contracts

- `IAppointmentLookup` / `IAppointmentCheckInPort` (Appointments.Application)
- No references to Appointments/Patients/Doctors Infrastructure from Queue

## Permissions

`Queue.View|CheckIn|Call|StartService|Complete|Skip|Cancel`

## APIs

- `POST /api/v1/queue/check-in`
- `GET /api/v1/queue/{id}`
- `GET /api/v1/queue/today`
- `GET /api/v1/queue/search`
- `POST /api/v1/queue/{id}/call|start-service|complete|skip|cancel`

`call-next` deferred (policy ambiguity).

## Domain events

`QueueEntryCheckedIn|Called|ServiceStarted|Completed|Skipped|Cancelled`  
Outbox deferred.

## Unresolved

- Check-in early/late window
- Priority triage rules
- Requeue after Skip
- Call-next selection policy
- Org timezone (UTC business clock)

## Out of scope

Medical Visits, prescriptions, billing, notifications, Angular queue UI, walk-in without appointment.
