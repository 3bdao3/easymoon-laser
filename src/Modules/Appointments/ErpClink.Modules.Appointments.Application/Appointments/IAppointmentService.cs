using ErpClink.Modules.Appointments.Application.Appointments.Models;

namespace ErpClink.Modules.Appointments.Application.Appointments;

public interface IAppointmentService
{
    Task<AppointmentDto> BookAsync(BookAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<AppointmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppointmentDto?> GetByNumberAsync(string appointmentNumber, CancellationToken cancellationToken = default);
    Task<PagedAppointmentsResult> SearchAsync(SearchAppointmentsRequest request, CancellationToken cancellationToken = default);
    Task ConfirmAsync(Guid id, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, CancelAppointmentRequest request, CancellationToken cancellationToken = default);
    Task RescheduleAsync(Guid id, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default);
    Task MarkNoShowAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAppointmentNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
