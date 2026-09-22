using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Prescriptions.Domain.Medications;

public sealed class MedicationCreatedDomainEvent : IDomainEvent
{
    public MedicationCreatedDomainEvent(Guid medicationId, string code, DateTime occurredOnUtc)
    {
        MedicationId = medicationId;
        Code = code;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid MedicationId { get; }
    public string Code { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class MedicationUpdatedDomainEvent : IDomainEvent
{
    public MedicationUpdatedDomainEvent(Guid medicationId, DateTime occurredOnUtc)
    {
        MedicationId = medicationId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid MedicationId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class MedicationActivatedDomainEvent : IDomainEvent
{
    public MedicationActivatedDomainEvent(Guid medicationId, DateTime occurredOnUtc)
    {
        MedicationId = medicationId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid MedicationId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class MedicationDeactivatedDomainEvent : IDomainEvent
{
    public MedicationDeactivatedDomainEvent(Guid medicationId, DateTime occurredOnUtc)
    {
        MedicationId = medicationId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid MedicationId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class Medication : AggregateRoot
{
    private Medication()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? GenericName { get; private set; }
    public string? Strength { get; private set; }
    public string? DosageForm { get; private set; }
    public string? Route { get; private set; }
    public bool IsActive { get; private set; }

    public static Medication Create(
        Guid organizationId,
        string code,
        string name,
        string? genericName,
        string? strength,
        string? dosageForm,
        string? route,
        string? createdBy,
        DateTime utcNow)
    {
        var medication = new Medication
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = Require(code, 64),
            Name = Require(name, 200),
            GenericName = Normalize(genericName, 200),
            Strength = Normalize(strength, 64),
            DosageForm = Normalize(dosageForm, 64),
            Route = Normalize(route, 64),
            IsActive = true
        };
        medication.SetCreated(createdBy, utcNow);
        medication.RaiseDomainEvent(new MedicationCreatedDomainEvent(medication.Id, medication.Code, utcNow));
        return medication;
    }

    public void Update(
        string name,
        string? genericName,
        string? strength,
        string? dosageForm,
        string? route,
        string? updatedBy,
        DateTime utcNow)
    {
        Name = Require(name, 200);
        GenericName = Normalize(genericName, 200);
        Strength = Normalize(strength, 64);
        DosageForm = Normalize(dosageForm, 64);
        Route = Normalize(route, 64);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new MedicationUpdatedDomainEvent(Id, utcNow));
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new MedicationActivatedDomainEvent(Id, utcNow));
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new MedicationDeactivatedDomainEvent(Id, utcNow));
    }

    private static string Require(string value, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
