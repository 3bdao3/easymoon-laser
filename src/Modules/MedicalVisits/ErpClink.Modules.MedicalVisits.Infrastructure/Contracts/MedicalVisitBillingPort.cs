using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.MedicalVisits.Application.Contracts;
using ErpClink.Modules.MedicalVisits.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.MedicalVisits.Infrastructure.Contracts;

public sealed class MedicalVisitBillingPort : IMedicalVisitBillingPort
{
    private readonly MedicalVisitsDbContext _db;
    private readonly IOrganizationContext _org;

    public MedicalVisitBillingPort(MedicalVisitsDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<MedicalVisitBillingLookup?> GetAsync(
        Guid medicalVisitId,
        CancellationToken cancellationToken = default)
    {
        return await _db.MedicalVisits.AsNoTracking()
            .Where(v => v.Id == medicalVisitId && v.OrganizationId == _org.OrganizationId)
            .Select(v => new MedicalVisitBillingLookup(
                v.Id, v.OrganizationId, v.BranchId, v.PatientId, v.VisitDate, v.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
