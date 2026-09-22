using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Prescriptions.Domain.Prescriptions;

public enum PrescriptionStatus
{
    Draft = 0,
    Issued = 1,
    Cancelled = 2
}

public sealed class PrescriptionCreatedDomainEvent : IDomainEvent
{
    public PrescriptionCreatedDomainEvent(Guid prescriptionId, Guid medicalVisitId, string prescriptionNumber, DateTime occurredOnUtc)
    {
        PrescriptionId = prescriptionId;
        MedicalVisitId = medicalVisitId;
        PrescriptionNumber = prescriptionNumber;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PrescriptionId { get; }
    public Guid MedicalVisitId { get; }
    public string PrescriptionNumber { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PrescriptionUpdatedDomainEvent : IDomainEvent
{
    public PrescriptionUpdatedDomainEvent(Guid prescriptionId, DateTime occurredOnUtc)
    {
        PrescriptionId = prescriptionId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PrescriptionId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PrescriptionIssuedDomainEvent : IDomainEvent
{
    public PrescriptionIssuedDomainEvent(Guid prescriptionId, DateTime occurredOnUtc)
    {
        PrescriptionId = prescriptionId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PrescriptionId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PrescriptionCancelledDomainEvent : IDomainEvent
{
    public PrescriptionCancelledDomainEvent(Guid prescriptionId, DateTime occurredOnUtc)
    {
        PrescriptionId = prescriptionId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PrescriptionId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PrescriptionItemAddedDomainEvent : IDomainEvent
{
    public PrescriptionItemAddedDomainEvent(Guid prescriptionId, Guid itemId, DateTime occurredOnUtc)
    {
        PrescriptionId = prescriptionId;
        ItemId = itemId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PrescriptionId { get; }
    public Guid ItemId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PrescriptionItemUpdatedDomainEvent : IDomainEvent
{
    public PrescriptionItemUpdatedDomainEvent(Guid prescriptionId, Guid itemId, DateTime occurredOnUtc)
    {
        PrescriptionId = prescriptionId;
        ItemId = itemId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PrescriptionId { get; }
    public Guid ItemId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PrescriptionItemRemovedDomainEvent : IDomainEvent
{
    public PrescriptionItemRemovedDomainEvent(Guid prescriptionId, Guid itemId, DateTime occurredOnUtc)
    {
        PrescriptionId = prescriptionId;
        ItemId = itemId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PrescriptionId { get; }
    public Guid ItemId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PrescriptionItem
{
    private PrescriptionItem()
    {
    }

    public Guid Id { get; private set; }
    public Guid PrescriptionId { get; private set; }
    public Guid MedicationId { get; private set; }
    public string MedicationNameSnapshot { get; private set; } = string.Empty;
    public string Dosage { get; private set; } = string.Empty;
    public string Frequency { get; private set; } = string.Empty;
    public string? Duration { get; private set; }
    public string? Route { get; private set; }
    public string? Instructions { get; private set; }
    public decimal? Quantity { get; private set; }
    public string? Notes { get; private set; }
    public int SortOrder { get; private set; }

    internal static PrescriptionItem Create(
        Guid prescriptionId,
        Guid medicationId,
        string medicationNameSnapshot,
        string dosage,
        string frequency,
        string? duration,
        string? route,
        string? instructions,
        decimal? quantity,
        string? notes,
        int sortOrder)
    {
        return new PrescriptionItem
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescriptionId,
            MedicationId = medicationId,
            MedicationNameSnapshot = Require(medicationNameSnapshot, 200),
            Dosage = Require(dosage, 128),
            Frequency = Require(frequency, 128),
            Duration = Normalize(duration, 128),
            Route = Normalize(route, 64),
            Instructions = Normalize(instructions, 1000),
            Quantity = quantity,
            Notes = Normalize(notes, 500),
            SortOrder = sortOrder
        };
    }

    internal void Update(
        string dosage,
        string frequency,
        string? duration,
        string? route,
        string? instructions,
        decimal? quantity,
        string? notes,
        int sortOrder)
    {
        Dosage = Require(dosage, 128);
        Frequency = Require(frequency, 128);
        Duration = Normalize(duration, 128);
        Route = Normalize(route, 64);
        Instructions = Normalize(instructions, 1000);
        Quantity = quantity;
        Notes = Normalize(notes, 500);
        SortOrder = sortOrder;
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

/// <summary>Prescription for a Medical Visit. Issued prescriptions are immutable.</summary>
public sealed class Prescription : AggregateRoot
{
    private readonly List<PrescriptionItem> _items = [];

    private Prescription()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string PrescriptionNumber { get; private set; } = string.Empty;
    public Guid MedicalVisitId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public DateOnly PrescriptionDate { get; private set; }
    public PrescriptionStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? IssuedAtUtc { get; private set; }
    public string? IssuedBy { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    // Return the backing list (not AsReadOnly wrapper) so EF field-access tracking stays consistent.
    public IReadOnlyCollection<PrescriptionItem> Items => _items;
    public bool IsActive => Status is PrescriptionStatus.Draft or PrescriptionStatus.Issued;

    public static Prescription CreateDraft(
        Guid organizationId,
        Guid branchId,
        string prescriptionNumber,
        Guid medicalVisitId,
        Guid patientId,
        Guid doctorId,
        DateOnly prescriptionDate,
        string? notes,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prescriptionNumber);
        var prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            PrescriptionNumber = prescriptionNumber.Trim(),
            MedicalVisitId = medicalVisitId,
            PatientId = patientId,
            DoctorId = doctorId,
            PrescriptionDate = prescriptionDate,
            Status = PrescriptionStatus.Draft,
            Notes = NormalizeNotes(notes)
        };
        prescription.SetCreated(createdBy, utcNow);
        prescription.RaiseDomainEvent(new PrescriptionCreatedDomainEvent(
            prescription.Id, medicalVisitId, prescription.PrescriptionNumber, utcNow));
        return prescription;
    }

    public void UpdateNotes(string? notes, string? updatedBy, DateTime utcNow)
    {
        EnsureDraft();
        Notes = NormalizeNotes(notes);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new PrescriptionUpdatedDomainEvent(Id, utcNow));
    }

    public PrescriptionItem AddItem(
        Guid medicationId,
        string medicationNameSnapshot,
        string dosage,
        string frequency,
        string? duration,
        string? route,
        string? instructions,
        decimal? quantity,
        string? notes,
        int? sortOrder,
        string? updatedBy,
        DateTime utcNow)
    {
        EnsureDraft();
        if (_items.Count >= 50)
            throw new InvalidOperationException("A prescription cannot contain more than 50 items.");

        var order = sortOrder ?? (_items.Count == 0 ? 1 : _items.Max(i => i.SortOrder) + 1);
        var item = PrescriptionItem.Create(
            Id, medicationId, medicationNameSnapshot, dosage, frequency, duration, route, instructions, quantity, notes, order);
        _items.Add(item);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new PrescriptionItemAddedDomainEvent(Id, item.Id, utcNow));
        return item;
    }

    public void UpdateItem(
        Guid itemId,
        string dosage,
        string frequency,
        string? duration,
        string? route,
        string? instructions,
        decimal? quantity,
        string? notes,
        int sortOrder,
        string? updatedBy,
        DateTime utcNow)
    {
        EnsureDraft();
        var item = _items.SingleOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Prescription item was not found.");
        item.Update(dosage, frequency, duration, route, instructions, quantity, notes, sortOrder);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new PrescriptionItemUpdatedDomainEvent(Id, itemId, utcNow));
    }

    public void RemoveItem(Guid itemId, string? updatedBy, DateTime utcNow)
    {
        EnsureDraft();
        var item = _items.SingleOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Prescription item was not found.");
        _items.Remove(item);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new PrescriptionItemRemovedDomainEvent(Id, itemId, utcNow));
    }

    public void Issue(string? issuedBy, DateTime utcNow)
    {
        if (Status != PrescriptionStatus.Draft)
            throw new InvalidOperationException($"Cannot issue prescription in status {Status}.");
        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot issue a prescription without items.");

        Status = PrescriptionStatus.Issued;
        IssuedAtUtc = utcNow;
        IssuedBy = issuedBy;
        SetUpdated(issuedBy, utcNow);
        RaiseDomainEvent(new PrescriptionIssuedDomainEvent(Id, utcNow));
    }

    public void Cancel(string? reason, string? cancelledBy, DateTime utcNow)
    {
        if (Status == PrescriptionStatus.Cancelled)
            throw new InvalidOperationException("Prescription is already cancelled.");
        // Draft and Issued may be cancelled (wrong-issue correction). No reinstatement.
        Status = PrescriptionStatus.Cancelled;
        CancellationReason = NormalizeNotes(reason);
        CancelledAtUtc = utcNow;
        CancelledBy = cancelledBy;
        SetUpdated(cancelledBy, utcNow);
        RaiseDomainEvent(new PrescriptionCancelledDomainEvent(Id, utcNow));
    }

    private void EnsureDraft()
    {
        if (Status != PrescriptionStatus.Draft)
            throw new InvalidOperationException($"Cannot modify prescription in status {Status}.");
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        var trimmed = notes.Trim();
        return trimmed.Length <= 1000 ? trimmed : trimmed[..1000];
    }
}
