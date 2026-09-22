using ErpClink.Modules.LaserClinic.Application.Customers;
using ErpClink.Modules.LaserClinic.Domain.Appointments;

namespace ErpClink.Modules.LaserClinic.Application.Appointments;

public sealed record AppointmentServiceLineDto(
    Guid Id,
    Guid LaserServiceId,
    string ServiceName,
    int DurationMinutes,
    int? PulsesConsumed);

public sealed record LaserAppointmentDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int DurationMinutes,
    LaserAppointmentStatus Status,
    string? Notes,
    IReadOnlyList<AppointmentServiceLineDto> Services,
    decimal? AmountPaid = null);

public sealed record ServiceDurationOverrideDto(
    Guid ServiceId,
    int DurationMinutes,
    int? PulsesConsumed = null);

public sealed record RecordSessionRequest(
    int DurationMinutes,
    int? PulsesConsumed,
    decimal? AmountPaid,
    bool MarkAttended = true,
    decimal? PackagePrice = null);

public sealed record SessionDetailDto(
    LaserAppointmentDto Appointment,
    CustomerPulseBalanceDto PulseBalance,
    decimal TotalAmountPaid,
    int AvailablePulses);

public sealed record CreateLaserAppointmentRequest(
    Guid CustomerId,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    IReadOnlyList<Guid> LaserServiceIds,
    string? Notes,
    IReadOnlyList<ServiceDurationOverrideDto>? ServiceDurationOverrides = null);

public sealed record UpdateLaserAppointmentStatusRequest(LaserAppointmentStatus Status);

public sealed record UpdateLaserAppointmentRequest(
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    IReadOnlyList<Guid> LaserServiceIds,
    string? Notes,
    IReadOnlyList<ServiceDurationOverrideDto>? ServiceDurationOverrides = null);

public sealed record BookingCustomerInput(string FullName, string PhoneNumber, int? Age, string? Notes);

public sealed record CreateBookingWithCustomerRequest(
    Guid? ExistingCustomerId,
    BookingCustomerInput? Customer,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    IReadOnlyList<Guid> LaserServiceIds,
    string? AppointmentNotes,
    IReadOnlyList<ServiceDurationOverrideDto>? ServiceDurationOverrides = null);

public sealed record SlotCheckQuery(
    DateOnly Date,
    TimeOnly StartTime,
    IReadOnlyList<Guid> LaserServiceIds,
    IReadOnlyDictionary<Guid, int>? ServiceDurationOverrides = null);

public sealed record SlotCheckResultDto(
    bool IsAvailable,
    int ClinicalDurationMinutes,
    TimeOnly EndTime,
    string MessageAr);

public sealed record AvailabilityQuery(
    DateOnly Date,
    IReadOnlyList<Guid> LaserServiceIds,
    Guid? ExcludeAppointmentId = null,
    IReadOnlyDictionary<Guid, int>? ServiceDurationOverrides = null);

public enum AvailabilitySlotStatus
{
    Available = 0,
    Booked = 1,
    Unavailable = 2
}

public sealed record AvailabilitySlotDto(
    TimeOnly StartTime,
    TimeOnly EndTime,
    int DurationMinutes,
    AvailabilitySlotStatus Status,
    Guid? AppointmentId,
    string? CustomerName,
    string? ServiceNames,
    int? BookedDurationMinutes);

public sealed record AvailabilityResultDto(
    DateOnly Date,
    int ClinicalDurationMinutes,
    int BufferMinutes,
    int BlockedDurationMinutes,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    int SlotIntervalMinutes,
    IReadOnlyList<AvailabilitySlotDto> Slots);

public sealed record CustomerHistoryDto(
    Guid CustomerId,
    string FullName,
    string PhoneNumber,
    IReadOnlyList<LaserAppointmentDto> Upcoming,
    IReadOnlyList<LaserAppointmentDto> Previous,
    CustomerPulseBalanceDto PulseBalance);

public interface ILaserAppointmentAppService
{
    Task<IReadOnlyList<LaserAppointmentDto>> ListAsync(DateOnly? date, Guid? customerId, CancellationToken cancellationToken = default);
    Task<LaserAppointmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LaserAppointmentDto> CreateAsync(CreateLaserAppointmentRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<LaserAppointmentDto> CreateBookingWithCustomerAsync(CreateBookingWithCustomerRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<SlotCheckResultDto> CheckSlotAsync(SlotCheckQuery query, CancellationToken cancellationToken = default);
    Task<LaserAppointmentDto> UpdateStatusAsync(Guid id, LaserAppointmentStatus status, string? userId, CancellationToken cancellationToken = default);
    Task<LaserAppointmentDto> UpdateAsync(Guid id, UpdateLaserAppointmentRequest request, string? userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, string? userId, CancellationToken cancellationToken = default);
    Task<AvailabilityResultDto> GetAvailabilityAsync(AvailabilityQuery query, CancellationToken cancellationToken = default);
    Task<CustomerHistoryDto> GetCustomerHistoryAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<SessionDetailDto?> GetSessionDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SessionDetailDto> RecordSessionAsync(Guid id, RecordSessionRequest request, string? userId, CancellationToken cancellationToken = default);
}
