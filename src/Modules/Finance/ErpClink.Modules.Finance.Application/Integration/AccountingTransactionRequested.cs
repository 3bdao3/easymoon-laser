using ErpClink.BuildingBlocks.Application.IntegrationEvents;

namespace ErpClink.Modules.Finance.Application.Integration;

/// <summary>
/// Stable integration event contract for future Outbox delivery (STEP 24).
/// Not published via Outbox in STEP 18 — emitted to logs/collector only when a request is accepted.
/// </summary>
public sealed record AccountingTransactionRequested(
    Guid EventId,
    Guid OrganizationId,
    Guid? BranchId,
    string SourceModule,
    string SourceType,
    string SourceId,
    string EventType,
    Guid CorrelationId,
    DateTime OccurredAtUtc,
    string IdempotencyKey,
    Guid IntegrationRequestId) : IIntegrationEvent
{
    public static AccountingTransactionRequested From(
        AccountingPostingRequest request,
        Guid integrationRequestId,
        Guid correlationId) =>
        new(
            EventId: Guid.NewGuid(),
            OrganizationId: request.OrganizationId,
            BranchId: request.BranchId,
            SourceModule: request.SourceModule,
            SourceType: request.SourceType,
            SourceId: request.SourceId,
            EventType: request.EventType,
            CorrelationId: correlationId,
            OccurredAtUtc: request.OccurredAtUtc,
            IdempotencyKey: request.IdempotencyKey,
            IntegrationRequestId: integrationRequestId);
}
