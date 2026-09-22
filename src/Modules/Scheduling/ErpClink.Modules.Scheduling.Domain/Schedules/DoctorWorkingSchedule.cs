using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Scheduling.Domain.Schedules;

public sealed class DoctorScheduleCreatedDomainEvent : IDomainEvent
{
    public DoctorScheduleCreatedDomainEvent(Guid scheduleId, Guid doctorId, Guid clinicId, DateTime occurredOnUtc)
    {
        ScheduleId = scheduleId;
        DoctorId = doctorId;
        ClinicId = clinicId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid ScheduleId { get; }
    public Guid DoctorId { get; }
    public Guid ClinicId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class DoctorScheduleUpdatedDomainEvent : IDomainEvent
{
    public DoctorScheduleUpdatedDomainEvent(Guid scheduleId, DateTime occurredOnUtc)
    {
        ScheduleId = scheduleId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid ScheduleId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class DoctorScheduleActivatedDomainEvent : IDomainEvent
{
    public DoctorScheduleActivatedDomainEvent(Guid scheduleId, DateTime occurredOnUtc)
    {
        ScheduleId = scheduleId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid ScheduleId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class DoctorScheduleDeactivatedDomainEvent : IDomainEvent
{
    public DoctorScheduleDeactivatedDomainEvent(Guid scheduleId, DateTime occurredOnUtc)
    {
        ScheduleId = scheduleId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid ScheduleId { get; }
    public DateTime OccurredOnUtc { get; }
}

/// <summary>
/// Recurring doctor working hours in clinic-local time (DayOfWeek + TimeOnly).
/// Not stored as UTC DateTime — appointment timezone handling belongs to Appointments.
/// </summary>
public sealed class DoctorWorkingSchedule : AggregateRoot
{
    public const int MinSlotDurationMinutes = 5;
    public const int MaxSlotDurationMinutes = 240;

    private DoctorWorkingSchedule()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid ClinicId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int SlotDurationMinutes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    public static DoctorWorkingSchedule Create(
        Guid organizationId,
        Guid branchId,
        Guid doctorId,
        Guid clinicId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string? createdBy,
        DateTime utcNow)
    {
        ValidateRange(startTime, endTime, slotDurationMinutes, effectiveFrom, effectiveTo);

        var schedule = new DoctorWorkingSchedule
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = slotDurationMinutes,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IsActive = true
        };
        schedule.SetCreated(createdBy, utcNow);
        schedule.RaiseDomainEvent(new DoctorScheduleCreatedDomainEvent(schedule.Id, doctorId, clinicId, utcNow));
        return schedule;
    }

    public void Update(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string? updatedBy,
        DateTime utcNow)
    {
        ValidateRange(startTime, endTime, slotDurationMinutes, effectiveFrom, effectiveTo);
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new DoctorScheduleUpdatedDomainEvent(Id, utcNow));
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new DoctorScheduleActivatedDomainEvent(Id, utcNow));
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new DoctorScheduleDeactivatedDomainEvent(Id, utcNow));
    }

    public bool IsEffectiveOn(DateOnly date)
    {
        if (date < EffectiveFrom) return false;
        if (EffectiveTo.HasValue && date > EffectiveTo.Value) return false;
        return true;
    }

    public bool OverlapsWith(DoctorWorkingSchedule other)
    {
        if (DoctorId != other.DoctorId || ClinicId != other.ClinicId) return false;
        if (DayOfWeek != other.DayOfWeek) return false;
        if (!EffectiveDateRangesOverlap(EffectiveFrom, EffectiveTo, other.EffectiveFrom, other.EffectiveTo))
            return false;
        return TimesOverlap(StartTime, EndTime, other.StartTime, other.EndTime);
    }

    public static bool TimesOverlap(TimeOnly startA, TimeOnly endA, TimeOnly startB, TimeOnly endB) =>
        startA < endB && startB < endA;

    public static bool EffectiveDateRangesOverlap(
        DateOnly fromA, DateOnly? toA, DateOnly fromB, DateOnly? toB)
    {
        var endA = toA ?? DateOnly.MaxValue;
        var endB = toB ?? DateOnly.MaxValue;
        return fromA <= endB && fromB <= endA;
    }

    private static void ValidateRange(
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
    {
        if (startTime >= endTime)
            throw new InvalidOperationException("StartTime must be before EndTime.");

        if (slotDurationMinutes < MinSlotDurationMinutes || slotDurationMinutes > MaxSlotDurationMinutes)
            throw new InvalidOperationException(
                $"SlotDurationMinutes must be between {MinSlotDurationMinutes} and {MaxSlotDurationMinutes}.");

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new InvalidOperationException("EffectiveTo cannot be before EffectiveFrom.");
    }
}
