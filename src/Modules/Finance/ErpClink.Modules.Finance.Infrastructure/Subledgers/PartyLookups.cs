using ErpClink.Modules.Finance.Application.Subledgers;
using ErpClink.Modules.Patients.Application.Contracts;
using ErpClink.Modules.Procurement.Application.Contracts;

namespace ErpClink.Modules.Finance.Infrastructure.Subledgers;

public sealed class ArCustomerLookup : IArCustomerLookup
{
    private readonly IPatientLookup _patients;

    public ArCustomerLookup(IPatientLookup patients) => _patients = patients;

    public async Task<ArCustomerSummary?> GetAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var p = await _patients.GetAsync(partyId, cancellationToken);
        if (p is null) return null;
        // Display uses patient number — Patients module remains master of clinical identity.
        return new ArCustomerSummary(p.Id, p.OrganizationId, p.PatientNumber, p.IsActive);
    }
}

public sealed class ApSupplierLookupAdapter : IApSupplierLookup
{
    private readonly ISupplierLookup _suppliers;

    public ApSupplierLookupAdapter(ISupplierLookup suppliers) => _suppliers = suppliers;

    public async Task<ApSupplierSummary?> GetAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var s = await _suppliers.GetByIdAsync(partyId, cancellationToken);
        if (s is null) return null;
        return new ApSupplierSummary(s.Id, s.OrganizationId, s.Name, s.SupplierCode, s.IsActive);
    }
}
