namespace ErpClink.Modules.Finance.Application.Integration;

/// <summary>
/// Application port for future modules to request accounting processing without Finance Infrastructure coupling.
/// STEP 18: registers idempotent intents only — does not create journal entries or account mappings.
/// </summary>
public interface IAccountingPostingPort
{
    Task<AccountingPostingResult> RequestPostingAsync(
        AccountingPostingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a processing failure for investigation / safe retry. Does not create journals.
    /// </summary>
    Task<AccountingPostingResult> MarkFailedAsync(
        Guid organizationId,
        string idempotencyKey,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
