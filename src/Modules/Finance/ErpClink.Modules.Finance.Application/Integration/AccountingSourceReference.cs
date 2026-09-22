namespace ErpClink.Modules.Finance.Application.Integration;

/// <summary>
/// Traceability from a future accounting transaction back to the owning business module.
/// </summary>
public sealed record AccountingSourceReference(
    Guid OrganizationId,
    string SourceModule,
    string SourceType,
    string SourceId,
    string EventType);
