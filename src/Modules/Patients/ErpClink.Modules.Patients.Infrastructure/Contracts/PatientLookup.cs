using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.Patients.Application.Contracts;
using ErpClink.Modules.Patients.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Patients.Infrastructure.Contracts;

public sealed class PatientLookup : IPatientLookup
{
    private readonly PatientsDbContext _db;
    private readonly IOrganizationContext _org;

    public PatientLookup(PatientsDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<PatientLookupResult?> GetAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _db.Patients.AsNoTracking()
            .Where(p => p.Id == patientId && p.OrganizationId == _org.OrganizationId)
            .Select(p => new PatientLookupResult(p.Id, p.OrganizationId, p.BranchId, p.IsActive, p.PatientNumber))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
