using ErpClink.Modules.Scheduling.Application.Schedules.Models;

namespace ErpClink.Modules.Scheduling.Application.Schedules;

public interface IDoctorScheduleService
{
    Task<DoctorScheduleDto> CreateAsync(CreateDoctorScheduleRequest request, CancellationToken cancellationToken = default);
    Task<DoctorScheduleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorScheduleDto>> ListByDoctorAsync(Guid doctorId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<DoctorScheduleDto> UpdateAsync(Guid id, UpdateDoctorScheduleRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Availability engine for future Appointments: slots for Doctor X in Clinic Y on Date Z.
/// Slots are calculated, not persisted.
/// </summary>
public interface IScheduleAvailabilityService
{
    Task<DoctorAvailabilityDto> GetAvailabilityAsync(
        Guid doctorId,
        DateOnly date,
        Guid? clinicId = null,
        CancellationToken cancellationToken = default);
}
