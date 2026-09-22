using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Patients.Domain.Patients;

public sealed class Patient : AggregateRoot
{
    private readonly List<PatientAllergy> _allergies = [];
    private readonly List<PatientMedicalHistoryItem> _medicalHistory = [];

    private Patient()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string PatientNumber { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string? MiddleName { get; private set; }
    public string LastName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string? NationalId { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<PatientAllergy> Allergies => _allergies.AsReadOnly();
    public IReadOnlyCollection<PatientMedicalHistoryItem> MedicalHistory => _medicalHistory.AsReadOnly();

    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}".Trim()
        : $"{FirstName} {MiddleName} {LastName}".Trim();

    public static Patient Register(
        Guid organizationId,
        Guid branchId,
        string patientNumber,
        string firstName,
        string? middleName,
        string lastName,
        DateOnly dateOfBirth,
        Gender gender,
        string? nationalId,
        string phoneNumber,
        string? email,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? emergencyContactName,
        string? emergencyContactPhone,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        if (dateOfBirth > DateOnly.FromDateTime(utcNow))
        {
            throw new InvalidOperationException("Date of birth cannot be in the future.");
        }

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            PatientNumber = patientNumber.Trim(),
            FirstName = firstName.Trim(),
            MiddleName = NormalizeOptional(middleName),
            LastName = lastName.Trim(),
            DateOfBirth = dateOfBirth,
            Gender = gender,
            NationalId = NormalizeOptional(nationalId),
            PhoneNumber = phoneNumber.Trim(),
            Email = NormalizeOptional(email),
            AddressLine1 = NormalizeOptional(addressLine1),
            AddressLine2 = NormalizeOptional(addressLine2),
            City = NormalizeOptional(city),
            EmergencyContactName = NormalizeOptional(emergencyContactName),
            EmergencyContactPhone = NormalizeOptional(emergencyContactPhone),
            IsActive = true
        };

        patient.SetCreated(createdBy, utcNow);
        patient.RaiseDomainEvent(new PatientRegisteredDomainEvent(
            patient.Id,
            organizationId,
            branchId,
            patient.PatientNumber,
            utcNow));

        return patient;
    }

    public void UpdateDemographics(
        string firstName,
        string? middleName,
        string lastName,
        DateOnly dateOfBirth,
        Gender gender,
        string? nationalId,
        string phoneNumber,
        string? email,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? emergencyContactName,
        string? emergencyContactPhone,
        string? updatedBy,
        DateTime utcNow)
    {
        EnsureActive();
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        if (dateOfBirth > DateOnly.FromDateTime(utcNow))
        {
            throw new InvalidOperationException("Date of birth cannot be in the future.");
        }

        FirstName = firstName.Trim();
        MiddleName = NormalizeOptional(middleName);
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender;
        NationalId = NormalizeOptional(nationalId);
        PhoneNumber = phoneNumber.Trim();
        Email = NormalizeOptional(email);
        AddressLine1 = NormalizeOptional(addressLine1);
        AddressLine2 = NormalizeOptional(addressLine2);
        City = NormalizeOptional(city);
        EmergencyContactName = NormalizeOptional(emergencyContactName);
        EmergencyContactPhone = NormalizeOptional(emergencyContactPhone);
        SetUpdated(updatedBy, utcNow);
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        SetUpdated(updatedBy, utcNow);
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        SetUpdated(updatedBy, utcNow);
    }

    public void Touch(string? updatedBy, DateTime utcNow) => SetUpdated(updatedBy, utcNow);

    public PatientAllergy AddAllergy(
        string name,
        string? reaction,
        AllergySeverity severity,
        string? notes,
        string? createdBy,
        DateTime utcNow)
    {
        EnsureActive();
        var allergy = PatientAllergy.Create(Id, name, reaction, severity, notes, createdBy, utcNow);
        _allergies.Add(allergy);
        SetUpdated(createdBy, utcNow);
        return allergy;
    }

    public PatientAllergy UpdateAllergy(
        Guid allergyId,
        string name,
        string? reaction,
        AllergySeverity severity,
        string? notes,
        string? updatedBy,
        DateTime utcNow)
    {
        EnsureActive();
        var allergy = GetAllergy(allergyId);
        allergy.Update(name, reaction, severity, notes, updatedBy, utcNow);
        SetUpdated(updatedBy, utcNow);
        return allergy;
    }

    public void DeactivateAllergy(Guid allergyId, string? updatedBy, DateTime utcNow)
    {
        var allergy = GetAllergy(allergyId);
        allergy.Deactivate(updatedBy, utcNow);
        SetUpdated(updatedBy, utcNow);
    }

    public PatientMedicalHistoryItem AddMedicalHistory(
        string category,
        string description,
        string? createdBy,
        DateTime utcNow)
    {
        EnsureActive();
        var item = PatientMedicalHistoryItem.Create(Id, category, description, createdBy, utcNow);
        _medicalHistory.Add(item);
        SetUpdated(createdBy, utcNow);
        return item;
    }

    private PatientAllergy GetAllergy(Guid allergyId)
    {
        var allergy = _allergies.SingleOrDefault(a => a.Id == allergyId);
        if (allergy is null)
        {
            throw new InvalidOperationException("Allergy was not found for this patient.");
        }

        return allergy;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Patient is inactive.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
