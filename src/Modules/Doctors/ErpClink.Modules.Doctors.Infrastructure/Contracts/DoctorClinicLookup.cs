using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.Doctors.Application.Contracts;
using ErpClink.Modules.Doctors.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Doctors.Infrastructure.Contracts;

public sealed class DoctorClinicLookup : IDoctorClinicLookup
{
    private readonly DoctorsDbContext _db;
    private readonly IOrganizationContext _org;

    public DoctorClinicLookup(DoctorsDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<DoctorClinicLookupResult?> GetDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        return await _db.Doctors.AsNoTracking()
            .Where(d => d.Id == doctorId && d.OrganizationId == _org.OrganizationId)
            .Select(d => new DoctorClinicLookupResult(d.Id, d.OrganizationId, d.BranchId, d.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<DoctorClinicLookupResult?> GetClinicAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        return await _db.Clinics.AsNoTracking()
            .Where(c => c.Id == clinicId && c.OrganizationId == _org.OrganizationId)
            .Select(c => new DoctorClinicLookupResult(c.Id, c.OrganizationId, c.BranchId, c.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasActiveAssignmentAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly onDate,
        CancellationToken cancellationToken = default)
    {
        return await _db.DoctorClinicAssignments.AsNoTracking().AnyAsync(a =>
            a.OrganizationId == _org.OrganizationId &&
            a.DoctorId == doctorId &&
            a.ClinicId == clinicId &&
            a.IsActive &&
            a.StartDate <= onDate &&
            (a.EndDate == null || a.EndDate >= onDate), cancellationToken);
    }
}
