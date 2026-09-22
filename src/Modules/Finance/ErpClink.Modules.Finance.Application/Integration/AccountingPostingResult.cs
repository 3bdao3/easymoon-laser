namespace ErpClink.Modules.Finance.Application.Integration;

/// <summary>
/// Port result — no EF entities or journal entries exposed.
/// </summary>
public sealed record AccountingPostingResult(
    AccountingPostingOutcome Outcome,
    Guid? IntegrationRequestId,
    Guid CorrelationId,
    string? Status,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static AccountingPostingResult Accepted(Guid integrationRequestId, Guid correlationId, string status) =>
        new(AccountingPostingOutcome.Accepted, integrationRequestId, correlationId, status, null, null);

    public static AccountingPostingResult AlreadyProcessed(Guid integrationRequestId, Guid correlationId, string status) =>
        new(AccountingPostingOutcome.AlreadyProcessed, integrationRequestId, correlationId, status, null, null);

    public static AccountingPostingResult Failed(Guid correlationId, string errorCode, string errorMessage, Guid? integrationRequestId = null, string? status = null) =>
        new(AccountingPostingOutcome.Failed, integrationRequestId, correlationId, status, errorCode, errorMessage);
}
