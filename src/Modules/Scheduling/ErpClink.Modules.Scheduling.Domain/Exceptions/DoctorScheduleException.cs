using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Scheduling.Domain.Schedules;

namespace ErpClink.Modules.Scheduling.Domain.Exceptions;

public enum ScheduleExceptionType
{
    Unavailable = 0,
    ExtraHours = 1
}

/// <summary>
/// Doctor-specific day override (leave / extra hours). Minimum model for SCH-04.
/// ExtraHours adds an additional working period for that date; Unavailable suppresses slots.
/// </summary>
public sealed class DoctorScheduleException : AggregateRoot
{
    private DoctorScheduleException()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid? ClinicId { get; private set; }
    public DateOnly Date { get; private set; }
    public ScheduleExceptionType Type { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public int? SlotDurationMinutes { get; private set; }
    public string? Reason { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static DoctorScheduleException CreateUnavailable(
        Guid organizationId,
        Guid branchId,
        Guid doctorId,
        Guid? clinicId,
        DateOnly date,
        string? reason,
        string? createdBy,
        DateTime utcNow)
    {
        var entity = new DoctorScheduleException
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            Date = date,
            Type = ScheduleExceptionType.Unavailable,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            IsActive = true
        };
        entity.SetCreated(createdBy, utcNow);
        return entity;
    }

    public static DoctorScheduleException CreateExtraHours(
        Guid organizationId,
        Guid branchId,
        Guid doctorId,
        Guid clinicId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        string? reason,
        string? createdBy,
        DateTime utcNow)
    {
        if (startTime >= endTime)
            throw new InvalidOperationException("StartTime must be before EndTime.");
        if (slotDurationMinutes < DoctorWorkingSchedule.MinSlotDurationMinutes
            || slotDurationMinutes > DoctorWorkingSchedule.MaxSlotDurationMinutes)
            throw new InvalidOperationException("Invalid slot duration.");

        var entity = new DoctorScheduleException
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            Date = date,
            Type = ScheduleExceptionType.ExtraHours,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = slotDurationMinutes,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            IsActive = true
        };
        entity.SetCreated(createdBy, utcNow);
        return entity;
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
    }
}
