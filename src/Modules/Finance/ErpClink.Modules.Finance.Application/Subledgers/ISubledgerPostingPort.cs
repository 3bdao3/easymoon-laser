namespace ErpClink.Modules.Finance.Application.Subledgers;

public sealed record SubledgerPostRequest(
    Guid OrganizationId,
    Guid? BranchId,
    Guid PartyId,
    string SourceModule,
    string SourceType,
    string SourceId,
    string EventType,
    DateOnly TransactionDate,
    decimal Amount,
    string CurrencyCode,
    string Direction,
    string? Description,
    string? PartyDisplayNameSnapshot,
    DateOnly? DueDate,
    Guid? CorrelationId,
    string IdempotencyKey);

public enum SubledgerPostOutcome
{
    Accepted = 0,
    AlreadyProcessed = 1,
    Failed = 2
}

public sealed record SubledgerPostResult(
    SubledgerPostOutcome Outcome,
    Guid? TransactionId,
    Guid CorrelationId,
    string? Status,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static SubledgerPostResult Accepted(Guid transactionId, Guid correlationId, string status) =>
        new(SubledgerPostOutcome.Accepted, transactionId, correlationId, status, null, null);

    public static SubledgerPostResult AlreadyProcessed(Guid transactionId, Guid correlationId, string status) =>
        new(SubledgerPostOutcome.AlreadyProcessed, transactionId, correlationId, status, null, null);

    public static SubledgerPostResult Failed(Guid correlationId, string errorCode, string errorMessage) =>
        new(SubledgerPostOutcome.Failed, null, correlationId, null, errorCode, errorMessage);
}

/// <summary>
/// Posts immutable AR/AP movements. Does not create General Ledger journals.
/// </summary>
public interface ISubledgerPostingPort
{
    Task<SubledgerPostResult> PostArAsync(SubledgerPostRequest request, CancellationToken cancellationToken = default);
    Task<SubledgerPostResult> PostApAsync(SubledgerPostRequest request, CancellationToken cancellationToken = default);
}
