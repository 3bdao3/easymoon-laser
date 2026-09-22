using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Doctors.Application.Contracts;
using ErpClink.Modules.Scheduling.Application.Schedules;
using ErpClink.Modules.Scheduling.Application.Schedules.Models;
using ErpClink.Modules.Scheduling.Domain.Availability;
using ErpClink.Modules.Scheduling.Domain.Exceptions;
using ErpClink.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Scheduling.Infrastructure.Schedules;

public sealed class ScheduleAvailabilityService : IScheduleAvailabilityService
{
    private readonly SchedulingDbContext _db;
    private readonly IDoctorClinicLookup _lookup;
    private readonly IOrganizationContext _org;

    public ScheduleAvailabilityService(
        SchedulingDbContext db,
        IDoctorClinicLookup lookup,
        IOrganizationContext org)
    {
        _db = db;
        _lookup = lookup;
        _org = org;
    }

    public async Task<DoctorAvailabilityDto> GetAvailabilityAsync(
        Guid doctorId,
        DateOnly date,
        Guid? clinicId = null,
        CancellationToken cancellationToken = default)
    {
        var doctor = await _lookup.GetDoctorAsync(doctorId, cancellationToken)
            ?? throw new AppException("doctors.not_found", "Doctor was not found.", 404);

        if (!doctor.IsActive)
        {
            return new DoctorAvailabilityDto(doctorId, clinicId, date, false, null, false, []);
        }

        var holiday = await _db.ClinicHolidays.AsNoTracking()
            .Where(h =>
                h.OrganizationId == _org.OrganizationId &&
                h.IsActive &&
                h.Date == date &&
                (h.BranchId == null || h.BranchId == _org.BranchId))
            .Select(h => new { h.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (holiday is not null)
        {
            return new DoctorAvailabilityDto(doctorId, clinicId, date, true, holiday.Name, false, []);
        }

        var exceptions = await _db.DoctorScheduleExceptions.AsNoTracking()
            .Where(e =>
                e.OrganizationId == _org.OrganizationId &&
                e.DoctorId == doctorId &&
                e.Date == date &&
                e.IsActive &&
                (clinicId == null || e.ClinicId == null || e.ClinicId == clinicId))
            .ToListAsync(cancellationToken);

        if (exceptions.Any(e => e.Type == ScheduleExceptionType.Unavailable))
        {
            return new DoctorAvailabilityDto(doctorId, clinicId, date, false, null, true, []);
        }

        if (clinicId.HasValue)
        {
            var clinic = await _lookup.GetClinicAsync(clinicId.Value, cancellationToken)
                ?? throw new AppException("clinics.not_found", "Clinic was not found.", 404);
            if (!clinic.IsActive)
                return new DoctorAvailabilityDto(doctorId, clinicId, date, false, null, false, []);

            var assigned = await _lookup.HasActiveAssignmentAsync(doctorId, clinicId.Value, date, cancellationToken);
            if (!assigned)
                return new DoctorAvailabilityDto(doctorId, clinicId, date, false, null, false, []);
        }

        var day = date.DayOfWeek;
        var schedulesQuery = _db.DoctorWorkingSchedules.AsNoTracking()
            .Where(s =>
                s.OrganizationId == _org.OrganizationId &&
                s.DoctorId == doctorId &&
                s.IsActive &&
                s.DayOfWeek == day &&
                s.EffectiveFrom <= date &&
                (s.EffectiveTo == null || s.EffectiveTo >= date));

        if (clinicId.HasValue)
            schedulesQuery = schedulesQuery.Where(s => s.ClinicId == clinicId.Value);

        var schedules = await schedulesQuery
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        if (clinicId is null && schedules.Count > 0)
        {
            // When clinic not specified, only include schedules where assignment is active that day.
            var filtered = new List<Domain.Schedules.DoctorWorkingSchedule>();
            foreach (var schedule in schedules)
            {
                if (await _lookup.HasActiveAssignmentAsync(doctorId, schedule.ClinicId, date, cancellationToken))
                    filtered.Add(schedule);
            }
            schedules = filtered;
        }

        var slots = new List<AvailabilitySlotDto>();
        foreach (var schedule in schedules)
        {
            foreach (var slot in SlotGenerator.Generate(schedule.StartTime, schedule.EndTime, schedule.SlotDurationMinutes))
                slots.Add(new AvailabilitySlotDto(slot.Start, slot.End));
        }

        foreach (var extra in exceptions.Where(e =>
                     e.Type == ScheduleExceptionType.ExtraHours &&
                     e.StartTime.HasValue &&
                     e.EndTime.HasValue &&
                     e.SlotDurationMinutes.HasValue &&
                     (!clinicId.HasValue || e.ClinicId == clinicId)))
        {
            foreach (var slot in SlotGenerator.Generate(
                         extra.StartTime!.Value, extra.EndTime!.Value, extra.SlotDurationMinutes!.Value))
                slots.Add(new AvailabilitySlotDto(slot.Start, slot.End));
        }

        slots = slots
            .OrderBy(s => s.Start)
            .ThenBy(s => s.End)
            .Distinct()
            .ToList();

        return new DoctorAvailabilityDto(doctorId, clinicId, date, false, null, false, slots);
    }
}
