namespace ErpClink.Modules.Appointments.Application.Appointments.Models;

public sealed record BookAppointmentRequest(
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    DateOnly Date,
    TimeOnly StartTime,
    string? Reason,
    string? Notes);

public sealed record CancelAppointmentRequest(string? Reason);

public sealed record RescheduleAppointmentRequest(
    DateOnly Date,
    TimeOnly StartTime);

public sealed record SearchAppointmentsRequest(
    Guid? PatientId,
    Guid? DoctorId,
    Guid? ClinicId,
    DateOnly? Date,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Status,
    string? AppointmentNumber,
    string? SortBy,
    bool SortDescending = false,
    int Page = 1,
    int PageSize = 20);

public sealed record AppointmentDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string AppointmentNumber,
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status,
    string? Reason,
    string? Notes,
    string? CancellationReason,
    DateTime? CancelledAtUtc,
    string? CancelledBy,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);

public sealed record AppointmentListItemDto(
    Guid Id,
    string AppointmentNumber,
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status);

public sealed record PagedAppointmentsResult(
    IReadOnlyList<AppointmentListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
