using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Doctors.Domain.Clinics;

public sealed class ClinicCreatedDomainEvent : IDomainEvent
{
    public ClinicCreatedDomainEvent(Guid clinicId, Guid organizationId, Guid branchId, string code, DateTime occurredOnUtc)
    {
        ClinicId = clinicId;
        OrganizationId = organizationId;
        BranchId = branchId;
        Code = code;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid ClinicId { get; }
    public Guid OrganizationId { get; }
    public Guid BranchId { get; }
    public string Code { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class Clinic : AggregateRoot
{
    private Clinic()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Location { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Clinic Create(
        Guid organizationId,
        Guid branchId,
        string code,
        string name,
        string? description,
        string? location,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var clinic = new Clinic
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = Normalize(description),
            Location = Normalize(location),
            IsActive = true
        };
        clinic.SetCreated(createdBy, utcNow);
        clinic.RaiseDomainEvent(new ClinicCreatedDomainEvent(clinic.Id, organizationId, branchId, clinic.Code, utcNow));
        return clinic;
    }

    public void Update(string name, string? description, string? location, string? updatedBy, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = Normalize(description);
        Location = Normalize(location);
        SetUpdated(updatedBy, utcNow);
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
