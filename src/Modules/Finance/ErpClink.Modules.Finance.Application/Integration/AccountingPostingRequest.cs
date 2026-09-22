namespace ErpClink.Modules.Finance.Application.Integration;

/// <summary>
/// Application-level request to register an accounting posting intent.
/// Independent of <c>JournalEntry</c> / Finance Infrastructure entities.
/// </summary>
public sealed record AccountingPostingRequest(
    Guid OrganizationId,
    Guid? BranchId,
    string SourceModule,
    string SourceType,
    string SourceId,
    string EventType,
    DateTime OccurredAtUtc,
    Guid? CorrelationId,
    string IdempotencyKey,
    string? Description = null,
    string? CurrencyCode = null)
{
    public AccountingSourceReference ToSourceReference() =>
        new(OrganizationId, SourceModule, SourceType, SourceId, EventType);
}
