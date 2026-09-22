namespace ErpClink.Modules.Finance.Domain.Integration;

/// <summary>
/// Lifecycle of an accounting integration request.
/// Succeeded is reserved for when a journal is actually created by a future mapping worker.
/// </summary>
public enum AccountingIntegrationStatus
{
    Pending = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3
}
