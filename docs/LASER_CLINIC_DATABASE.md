# Laser Clinic Database Changes

## Strategy

Non-destructive. Legacy ERP schemas remain in the SQL database. The laser product uses a new schema.

## New schema: `laser`

Migration: `20260921054129_InitialLaserClinic`

| Table | Notes |
|-------|--------|
| Customers | Indexes on PhoneNumber, FullName |
| LaserServices | Seeded on first migrate |
| Appointments | Indexes on AppointmentDate, CustomerId, (Date, StartTime) |
| AppointmentServices | FK to Appointments (cascade), LaserServices (restrict) |
| ClinicSettings | Default 16:00–22:00, interval 15, buffer 0 |

## Host runtime

`Program.cs` migrates only Administration + LaserClinic.

## Rollback

Legacy module projects remain in the solution; Host ProjectReferences can be restored if needed.
