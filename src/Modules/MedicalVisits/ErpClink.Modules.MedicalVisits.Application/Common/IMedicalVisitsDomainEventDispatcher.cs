using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.MedicalVisits.Application.Common;

public interface IMedicalVisitsDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
