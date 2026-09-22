namespace ErpClink.Modules.Scheduling.Application.Schedules.Models;

public sealed record CreateDoctorScheduleRequest(
    Guid DoctorId,
    Guid ClinicId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

public sealed record UpdateDoctorScheduleRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

public sealed record DoctorScheduleDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    Guid DoctorId,
    Guid ClinicId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);

public sealed record AvailabilitySlotDto(TimeOnly Start, TimeOnly End);

public sealed record DoctorAvailabilityDto(
    Guid DoctorId,
    Guid? ClinicId,
    DateOnly Date,
    bool IsHoliday,
    string? HolidayName,
    bool IsDoctorUnavailable,
    IReadOnlyList<AvailabilitySlotDto> Slots);
