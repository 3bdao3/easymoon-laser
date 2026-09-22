using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Patients.Domain.Patients;

public sealed class PatientRegisteredDomainEvent : IDomainEvent
{
    public PatientRegisteredDomainEvent(
        Guid patientId,
        Guid organizationId,
        Guid branchId,
        string patientNumber,
        DateTime occurredOnUtc)
    {
        PatientId = patientId;
        OrganizationId = organizationId;
        BranchId = branchId;
        PatientNumber = patientNumber;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PatientId { get; }
    public Guid OrganizationId { get; }
    public Guid BranchId { get; }
    public string PatientNumber { get; }
    public DateTime OccurredOnUtc { get; }
}
