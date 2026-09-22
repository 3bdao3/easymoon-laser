using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Doctors.Application.Contracts;
using ErpClink.Modules.Scheduling.Application.Common;
using ErpClink.Modules.Scheduling.Application.Schedules;
using ErpClink.Modules.Scheduling.Application.Schedules.Models;
using ErpClink.Modules.Scheduling.Domain.Schedules;
using ErpClink.Modules.Scheduling.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Scheduling.Infrastructure.Schedules;

public sealed class DoctorScheduleService : IDoctorScheduleService
{
    private readonly SchedulingDbContext _db;
    private readonly IDoctorClinicLookup _lookup;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IDateTimeProvider _clock;
    private readonly ISchedulingDomainEventDispatcher _events;
    private readonly IValidator<CreateDoctorScheduleRequest> _createValidator;
    private readonly IValidator<UpdateDoctorScheduleRequest> _updateValidator;

    public DoctorScheduleService(
        SchedulingDbContext db,
        IDoctorClinicLookup lookup,
        IOrganizationContext org,
        ICurrentUser user,
        IDateTimeProvider clock,
        ISchedulingDomainEventDispatcher events,
        IValidator<CreateDoctorScheduleRequest> createValidator,
        IValidator<UpdateDoctorScheduleRequest> updateValidator)
    {
        _db = db;
        _lookup = lookup;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<DoctorScheduleDto> CreateAsync(
        CreateDoctorScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);
        await EnsureDoctorClinicAssignableAsync(request.DoctorId, request.ClinicId, request.EffectiveFrom, cancellationToken);

        try
        {
            var schedule = DoctorWorkingSchedule.Create(
                _org.OrganizationId,
                _org.BranchId,
                request.DoctorId,
                request.ClinicId,
                request.DayOfWeek,
                request.StartTime,
                request.EndTime,
                request.SlotDurationMinutes,
                request.EffectiveFrom,
                request.EffectiveTo,
                _user.UserId,
                _clock.UtcNow);

            await EnsureNoOverlapAsync(schedule, excludeId: null, cancellationToken);

            _db.DoctorWorkingSchedules.Add(schedule);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(schedule, cancellationToken);
            return Map(schedule);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("scheduling.validation_failed", ex.Message, 400);
        }
    }

    public async Task<DoctorScheduleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schedule = await OrgSchedules().AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        return schedule is null ? null : Map(schedule);
    }

    public async Task<IReadOnlyList<DoctorScheduleDto>> ListByDoctorAsync(
        Guid doctorId,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var doctor = await _lookup.GetDoctorAsync(doctorId, cancellationToken);
        if (doctor is null)
            throw new AppException("doctors.not_found", "Doctor was not found.", 404);

        var query = OrgSchedules().AsNoTracking().Where(s => s.DoctorId == doctorId);
        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var items = await query
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<DoctorScheduleDto> UpdateAsync(
        Guid id,
        UpdateDoctorScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);
        var schedule = await GetRequiredAsync(id, cancellationToken);

        try
        {
            schedule.Update(
                request.DayOfWeek,
                request.StartTime,
                request.EndTime,
                request.SlotDurationMinutes,
                request.EffectiveFrom,
                request.EffectiveTo,
                _user.UserId,
                _clock.UtcNow);

            await EnsureNoOverlapAsync(schedule, excludeId: schedule.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(schedule, cancellationToken);
            return Map(schedule);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("scheduling.validation_failed", ex.Message, 400);
        }
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schedule = await GetRequiredAsync(id, cancellationToken);
        await EnsureDoctorClinicAssignableAsync(schedule.DoctorId, schedule.ClinicId, schedule.EffectiveFrom, cancellationToken);

        schedule.Activate(_user.UserId, _clock.UtcNow);
        await EnsureNoOverlapAsync(schedule, excludeId: schedule.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await DispatchAsync(schedule, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schedule = await GetRequiredAsync(id, cancellationToken);
        schedule.Deactivate(_user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        await DispatchAsync(schedule, cancellationToken);
    }

    private async Task EnsureDoctorClinicAssignableAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly onDate,
        CancellationToken cancellationToken)
    {
        var doctor = await _lookup.GetDoctorAsync(doctorId, cancellationToken)
            ?? throw new AppException("doctors.not_found", "Doctor was not found.", 404);
        if (!doctor.IsActive)
            throw new AppException("doctors.inactive", "Inactive doctor cannot receive a new active schedule.", 400);

        var clinic = await _lookup.GetClinicAsync(clinicId, cancellationToken)
            ?? throw new AppException("clinics.not_found", "Clinic was not found.", 404);
        if (!clinic.IsActive)
            throw new AppException("clinics.inactive", "Inactive clinic cannot receive a new active schedule.", 400);

        if (doctor.OrganizationId != clinic.OrganizationId || doctor.OrganizationId != _org.OrganizationId)
            throw new AppException("scheduling.cross_organization", "Cross-organization schedule is forbidden.", 403);

        var assigned = await _lookup.HasActiveAssignmentAsync(doctorId, clinicId, onDate, cancellationToken);
        if (!assigned)
            throw new AppException(
                "scheduling.assignment_required",
                "Doctor must have an active assignment to the clinic for the effective date.",
                400);
    }

    private async Task EnsureNoOverlapAsync(
        DoctorWorkingSchedule candidate,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (!candidate.IsActive) return;

        var peers = await OrgSchedules()
            .Where(s =>
                s.IsActive &&
                s.DoctorId == candidate.DoctorId &&
                s.ClinicId == candidate.ClinicId &&
                s.DayOfWeek == candidate.DayOfWeek &&
                (excludeId == null || s.Id != excludeId.Value))
            .ToListAsync(cancellationToken);

        if (peers.Any(candidate.OverlapsWith))
            throw new AppException(
                "scheduling.overlap",
                "An overlapping active schedule already exists for this doctor, clinic, and day.",
                409);
    }

    private IQueryable<DoctorWorkingSchedule> OrgSchedules() =>
        _db.DoctorWorkingSchedules.Where(s => s.OrganizationId == _org.OrganizationId);

    private async Task<DoctorWorkingSchedule> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await OrgSchedules().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (schedule is null)
            throw new AppException("scheduling.not_found", "Schedule was not found.", 404);
        return schedule;
    }

    private async Task DispatchAsync(DoctorWorkingSchedule schedule, CancellationToken cancellationToken)
    {
        var events = schedule.DomainEvents.ToList();
        schedule.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static DoctorScheduleDto Map(DoctorWorkingSchedule s) =>
        new(s.Id, s.OrganizationId, s.BranchId, s.DoctorId, s.ClinicId, s.DayOfWeek,
            s.StartTime, s.EndTime, s.SlotDurationMinutes, s.IsActive, s.EffectiveFrom, s.EffectiveTo,
            s.CreatedAtUtc, s.CreatedBy, s.UpdatedAtUtc, s.UpdatedBy);

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new AppException("scheduling.validation_failed", string.Join(" ", result.Errors.Select(e => e.ErrorMessage)), 400);
    }
}
