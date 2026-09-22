namespace ErpClink.BuildingBlocks.Application.IntegrationEvents;

/// <summary>
/// Stable cross-module / async contract. Payloads must not leak EF entities.
/// Delivery via Outbox + background worker is deferred (STEP 24).
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAtUtc { get; }
    Guid CorrelationId { get; }
}
