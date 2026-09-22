namespace ErpClink.Modules.Finance.Domain.Integration;

/// <summary>
/// Durable idempotency / correlation record for future accounting postings.
/// Does not create journal entries by itself.
/// </summary>
public sealed class AccountingIntegrationRequest
{
    private AccountingIntegrationRequest()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string SourceModule { get; private set; } = string.Empty;
    public string SourceType { get; private set; } = string.Empty;
    public string SourceId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid CorrelationId { get; private set; }
    public string? Description { get; private set; }
    public string? CurrencyCode { get; private set; }
    public AccountingIntegrationStatus Status { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static AccountingIntegrationRequest CreatePending(
        Guid organizationId,
        Guid? branchId,
        string sourceModule,
        string sourceType,
        string sourceId,
        string eventType,
        string idempotencyKey,
        Guid correlationId,
        string? description,
        string? currencyCode,
        DateTime occurredAtUtc,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModule);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (correlationId == Guid.Empty)
            throw new ArgumentException("CorrelationId is required.", nameof(correlationId));

        return new AccountingIntegrationRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            SourceModule = sourceModule.Trim(),
            SourceType = sourceType.Trim(),
            SourceId = sourceId.Trim(),
            EventType = eventType.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            CorrelationId = correlationId,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? null : currencyCode.Trim().ToUpperInvariant(),
            Status = AccountingIntegrationStatus.Pending,
            OccurredAtUtc = occurredAtUtc,
            CreatedAtUtc = utcNow
        };
    }

    public void MarkProcessing(DateTime utcNow)
    {
        if (Status is AccountingIntegrationStatus.Succeeded)
            throw new InvalidOperationException("Cannot process a succeeded integration request.");

        Status = AccountingIntegrationStatus.Processing;
        UpdatedAtUtc = utcNow;
        ErrorCode = null;
        ErrorMessage = null;
    }

    public void MarkSucceeded(DateTime utcNow)
    {
        Status = AccountingIntegrationStatus.Succeeded;
        ProcessedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        ErrorCode = null;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        Status = AccountingIntegrationStatus.Failed;
        ProcessedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        ErrorCode = errorCode.Trim();
        ErrorMessage = errorMessage.Trim().Length <= 2000
            ? errorMessage.Trim()
            : errorMessage.Trim()[..2000];
    }

    /// <summary>
    /// Safe retry representation: Failed → Pending. Does not invent journal success.
    /// </summary>
    public void ResetForRetry(Guid correlationId, DateTime utcNow)
    {
        if (Status != AccountingIntegrationStatus.Failed)
            throw new InvalidOperationException("Only failed integration requests can be retried.");

        Status = AccountingIntegrationStatus.Pending;
        CorrelationId = correlationId == Guid.Empty ? CorrelationId : correlationId;
        ProcessedAtUtc = null;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAtUtc = utcNow;
    }
}
