namespace ErpClink.Modules.Finance.Application.Subledgers;

/// <summary>
/// AR party identity for V1 clinic billing: PatientId (no separate CRM Customer master).
/// </summary>
public interface IArCustomerLookup
{
    Task<ArCustomerSummary?> GetAsync(Guid partyId, CancellationToken cancellationToken = default);
}

public sealed record ArCustomerSummary(Guid Id, Guid OrganizationId, string DisplayName, bool IsActive);

/// <summary>
/// AP party identity: Procurement SupplierId (not duplicated in Finance).
/// </summary>
public interface IApSupplierLookup
{
    Task<ApSupplierSummary?> GetAsync(Guid partyId, CancellationToken cancellationToken = default);
}

public sealed record ApSupplierSummary(Guid Id, Guid OrganizationId, string DisplayName, string SupplierCode, bool IsActive);
