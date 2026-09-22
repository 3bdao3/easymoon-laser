using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Patients.Domain.Patients;

public sealed class PatientAllergy : AuditableEntity
{
    private PatientAllergy()
    {
    }

    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Reaction { get; private set; }
    public AllergySeverity Severity { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static PatientAllergy Create(
        Guid patientId,
        string name,
        string? reaction,
        AllergySeverity severity,
        string? notes,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var allergy = new PatientAllergy
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Name = name.Trim(),
            Reaction = NormalizeOptional(reaction),
            Severity = severity,
            Notes = NormalizeOptional(notes),
            IsActive = true
        };

        allergy.SetCreated(createdBy, utcNow);
        return allergy;
    }

    public void Update(string name, string? reaction, AllergySeverity severity, string? notes, string? updatedBy, DateTime utcNow)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Cannot update an inactive allergy.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Reaction = NormalizeOptional(reaction);
        Severity = severity;
        Notes = NormalizeOptional(notes);
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

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
