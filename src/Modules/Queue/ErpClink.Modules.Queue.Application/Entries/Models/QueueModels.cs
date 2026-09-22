namespace ErpClink.Modules.Queue.Application.Entries.Models;

public sealed record CheckInRequest(Guid AppointmentId, string? Priority);

public sealed record SearchQueueRequest(
    DateOnly? Date,
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? PatientId,
    Guid? AppointmentId,
    Guid? DoctorId,
    Guid? ClinicId,
    string? Status,
    string? Priority,
    string? QueueNumber,
    string? SortBy,
    bool SortDescending = false,
    int Page = 1,
    int PageSize = 20);

public sealed record TodayQueueRequest(
    Guid? ClinicId,
    Guid? DoctorId,
    string? Status,
    string? Priority);

public sealed record QueueEntryDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string QueueNumber,
    Guid PatientId,
    Guid AppointmentId,
    Guid DoctorId,
    Guid ClinicId,
    DateOnly QueueDate,
    string Priority,
    string Status,
    DateTime CheckInTimeUtc,
    DateTime? CalledTimeUtc,
    DateTime? ServiceStartTimeUtc,
    DateTime? ServiceEndTimeUtc,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);

public sealed record QueueListItemDto(
    Guid Id,
    string QueueNumber,
    Guid PatientId,
    Guid AppointmentId,
    Guid DoctorId,
    Guid ClinicId,
    DateOnly QueueDate,
    string Priority,
    string Status,
    DateTime CheckInTimeUtc,
    DateTime? CalledTimeUtc,
    DateTime? ServiceStartTimeUtc,
    DateTime? ServiceEndTimeUtc);

public sealed record PagedQueueResult(
    IReadOnlyList<QueueListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
