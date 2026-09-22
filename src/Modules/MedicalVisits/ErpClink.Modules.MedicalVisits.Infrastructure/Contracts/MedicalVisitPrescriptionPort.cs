using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.MedicalVisits.Application.Contracts;
using ErpClink.Modules.MedicalVisits.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.MedicalVisits.Infrastructure.Contracts;

public sealed class MedicalVisitPrescriptionPort : IMedicalVisitPrescriptionPort
{
    private readonly MedicalVisitsDbContext _db;
    private readonly IOrganizationContext _org;

    public MedicalVisitPrescriptionPort(MedicalVisitsDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<MedicalVisitPrescriptionLookup?> GetAsync(
        Guid medicalVisitId,
        CancellationToken cancellationToken = default)
    {
        return await _db.MedicalVisits.AsNoTracking()
            .Where(v => v.Id == medicalVisitId && v.OrganizationId == _org.OrganizationId)
            .Select(v => new MedicalVisitPrescriptionLookup(
                v.Id, v.OrganizationId, v.BranchId, v.PatientId, v.DoctorId, v.ClinicId,
                v.VisitDate, v.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
