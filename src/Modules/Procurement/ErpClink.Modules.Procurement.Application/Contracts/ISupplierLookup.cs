namespace ErpClink.Modules.Procurement.Application.Contracts;

public interface ISupplierLookup
{
    Task<SupplierSummaryLookup?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken = default);
}

public sealed record SupplierSummaryLookup(
    Guid Id,
    Guid OrganizationId,
    string SupplierCode,
    string Name,
    bool IsActive);
