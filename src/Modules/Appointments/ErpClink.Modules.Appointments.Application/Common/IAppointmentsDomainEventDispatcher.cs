using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Appointments.Application.Common;

public interface IAppointmentsDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
