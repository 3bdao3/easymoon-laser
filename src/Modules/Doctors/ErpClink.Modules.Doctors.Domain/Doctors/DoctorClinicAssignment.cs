using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Doctors.Domain.Doctors;

public sealed class DoctorClinicAssignment : AuditableEntity
{
    private DoctorClinicAssignment()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid ClinicId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static DoctorClinicAssignment Create(
        Guid organizationId,
        Guid branchId,
        Guid doctorId,
        Guid clinicId,
        DateOnly startDate,
        DateOnly? endDate,
        string? createdBy,
        DateTime utcNow)
    {
        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new InvalidOperationException("EndDate cannot be earlier than StartDate.");
        }

        var assignment = new DoctorClinicAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true
        };
        assignment.SetCreated(createdBy, utcNow);
        return assignment;
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        EndDate ??= DateOnly.FromDateTime(utcNow);
        SetUpdated(updatedBy, utcNow);
    }
}
