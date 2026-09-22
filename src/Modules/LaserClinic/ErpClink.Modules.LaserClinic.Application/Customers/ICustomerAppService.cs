using ErpClink.Modules.LaserClinic.Domain.Appointments;

namespace ErpClink.Modules.LaserClinic.Application.Customers;

public sealed record CustomerPulseBalanceDto(
    int? PackageTotal,
    int? Remaining,
    int Consumed,
    decimal? PackagePriceTotal = null,
    decimal AmountPaid = 0,
    decimal RemainingAmount = 0,
    int? PackageDurationTotalMinutes = null,
    int DurationUsedMinutes = 0,
    int RemainingDurationMinutes = 0);

public sealed record CustomerDto(
    Guid Id,
    string FullName,
    string PhoneNumber,
    int? Age,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    CustomerPulseBalanceDto PulseBalance);

public sealed record CustomerNextAppointmentDto(
    Guid Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int DurationMinutes,
    LaserAppointmentStatus Status,
    IReadOnlyList<string> ServiceNames);

public sealed record CustomerLastAppointmentDto(
    Guid Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int DurationMinutes,
    LaserAppointmentStatus Status,
    IReadOnlyList<string> ServiceNames);

public sealed record CustomerListItemDto(
    Guid Id,
    string FullName,
    string PhoneNumber,
    int? Age,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    CustomerNextAppointmentDto? NextAppointment,
    CustomerLastAppointmentDto? LastAppointment = null);

public sealed record CreateCustomerRequest(string FullName, string PhoneNumber, int? Age, string? Notes);

public sealed record UpdateCustomerRequest(string FullName, string PhoneNumber, int? Age, string? Notes);

public enum CustomerListSort
{
    Name = 0,
    NextAppointment = 1,
    Newest = 2,
    Oldest = 3,
    LastAppointment = 4
}

public interface ICustomerAppService
{
    Task<IReadOnlyList<CustomerListItemDto>> SearchAsync(
        string? query,
        bool? isActive = true,
        CustomerListSort sort = CustomerListSort.Name,
        CancellationToken cancellationToken = default);

    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, string? userId, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, string? userId, CancellationToken cancellationToken = default);
}
