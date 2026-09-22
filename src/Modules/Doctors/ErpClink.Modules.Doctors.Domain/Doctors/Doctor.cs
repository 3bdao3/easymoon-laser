using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Doctors.Domain.Doctors;

public sealed class DoctorRegisteredDomainEvent : IDomainEvent
{
    public DoctorRegisteredDomainEvent(Guid doctorId, Guid organizationId, Guid branchId, string doctorNumber, DateTime occurredOnUtc)
    {
        DoctorId = doctorId;
        OrganizationId = organizationId;
        BranchId = branchId;
        DoctorNumber = doctorNumber;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid DoctorId { get; }
    public Guid OrganizationId { get; }
    public Guid BranchId { get; }
    public string DoctorNumber { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class DoctorActivatedDomainEvent : IDomainEvent
{
    public DoctorActivatedDomainEvent(Guid doctorId, DateTime occurredOnUtc)
    {
        DoctorId = doctorId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid DoctorId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class DoctorDeactivatedDomainEvent : IDomainEvent
{
    public DoctorDeactivatedDomainEvent(Guid doctorId, DateTime occurredOnUtc)
    {
        DoctorId = doctorId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid DoctorId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class DoctorAssignedToClinicDomainEvent : IDomainEvent
{
    public DoctorAssignedToClinicDomainEvent(Guid doctorId, Guid clinicId, Guid assignmentId, DateTime occurredOnUtc)
    {
        DoctorId = doctorId;
        ClinicId = clinicId;
        AssignmentId = assignmentId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid DoctorId { get; }
    public Guid ClinicId { get; }
    public Guid AssignmentId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class Doctor : AggregateRoot
{
    private Doctor()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string DoctorNumber { get; private set; } = string.Empty;
    public string? UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public Guid? SpecialtyId { get; private set; }
    public string? LicenseNumber { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Doctor Register(
        Guid organizationId,
        Guid branchId,
        string doctorNumber,
        string? userId,
        string firstName,
        string lastName,
        string? displayName,
        Guid? specialtyId,
        string? licenseNumber,
        string? phoneNumber,
        string? email,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(doctorNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var doctor = new Doctor
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            DoctorNumber = doctorNumber.Trim(),
            UserId = Normalize(userId),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? $"{firstName.Trim()} {lastName.Trim()}"
                : displayName.Trim(),
            SpecialtyId = specialtyId,
            LicenseNumber = Normalize(licenseNumber),
            PhoneNumber = Normalize(phoneNumber),
            Email = Normalize(email),
            IsActive = true
        };

        doctor.SetCreated(createdBy, utcNow);
        doctor.RaiseDomainEvent(new DoctorRegisteredDomainEvent(
            doctor.Id, organizationId, branchId, doctor.DoctorNumber, utcNow));
        return doctor;
    }

    public void Update(
        string firstName,
        string lastName,
        string? displayName,
        Guid? specialtyId,
        string? licenseNumber,
        string? phoneNumber,
        string? email,
        string? userId,
        string? updatedBy,
        DateTime utcNow)
    {
        EnsureActive();
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? $"{FirstName} {LastName}"
            : displayName.Trim();
        SpecialtyId = specialtyId;
        LicenseNumber = Normalize(licenseNumber);
        PhoneNumber = Normalize(phoneNumber);
        Email = Normalize(email);
        UserId = Normalize(userId);
        SetUpdated(updatedBy, utcNow);
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new DoctorActivatedDomainEvent(Id, utcNow));
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new DoctorDeactivatedDomainEvent(Id, utcNow));
    }

    public void NotifyAssignedToClinic(Guid clinicId, Guid assignmentId, DateTime utcNow)
    {
        RaiseDomainEvent(new DoctorAssignedToClinicDomainEvent(Id, clinicId, assignmentId, utcNow));
    }

    public void EnsureActive()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Doctor is inactive.");
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
