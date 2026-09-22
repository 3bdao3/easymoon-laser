using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Patients.Domain.Patients;

/// <summary>
/// Lightweight patient-level medical history item (not a clinical visit record).
/// </summary>
public sealed class PatientMedicalHistoryItem : AuditableEntity
{
    private PatientMedicalHistoryItem()
    {
    }

    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public string Category { get; private set; } = "General";
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    internal static PatientMedicalHistoryItem Create(
        Guid patientId,
        string category,
        string description,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var item = new PatientMedicalHistoryItem
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
            Description = description.Trim(),
            IsActive = true
        };

        item.SetCreated(createdBy, utcNow);
        return item;
    }

    internal void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        SetUpdated(updatedBy, utcNow);
    }
}
