using ErpClink.BuildingBlocks.Domain.Abstractions;
using AssetStatus = ErpClink.Modules.Assets.Domain.AssetStatus;

namespace ErpClink.Modules.Assets.Domain.Assets;

public sealed class Asset : AggregateRoot
{
    private Asset()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string AssetNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid AssetCategoryId { get; private set; }
    public Guid AssetLocationId { get; private set; }
    public string? SerialNumber { get; private set; }
    public DateOnly? PurchaseDate { get; private set; }
    public decimal? AcquisitionCost { get; private set; }
    public string? AcquisitionReference { get; private set; }
    public DateOnly? WarrantyStartDate { get; private set; }
    public DateOnly? WarrantyEndDate { get; private set; }
    public string? WarrantyNotes { get; private set; }
    public AssetStatus Status { get; private set; }
    public string? MaintenanceNotes { get; private set; }
    public DateTime? RetiredAtUtc { get; private set; }
    public string? RetiredBy { get; private set; }
    public string? RetirementReason { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static Asset Create(
        Guid organizationId,
        Guid branchId,
        string assetNumber,
        string name,
        string? description,
        Guid assetCategoryId,
        Guid assetLocationId,
        string? serialNumber,
        DateOnly? purchaseDate,
        decimal? acquisitionCost,
        string? acquisitionReference,
        DateOnly? warrantyStartDate,
        DateOnly? warrantyEndDate,
        string? warrantyNotes,
        string? createdBy,
        DateTime utcNow)
    {
        EnsureWarrantyValid(warrantyStartDate, warrantyEndDate);
        if (acquisitionCost is < 0)
            throw new ArgumentOutOfRangeException(nameof(acquisitionCost));

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            AssetNumber = Require(assetNumber, 32),
            Name = Require(name, 200),
            Description = Normalize(description, 500),
            AssetCategoryId = assetCategoryId,
            AssetLocationId = assetLocationId,
            SerialNumber = Normalize(serialNumber, 128),
            PurchaseDate = purchaseDate,
            AcquisitionCost = acquisitionCost,
            AcquisitionReference = Normalize(acquisitionReference, 500),
            WarrantyStartDate = warrantyStartDate,
            WarrantyEndDate = warrantyEndDate,
            WarrantyNotes = Normalize(warrantyNotes, 500),
            Status = AssetStatus.Active
        };
        asset.SetCreated(createdBy, utcNow);
        return asset;
    }

    public void UpdateDetails(
        string name,
        string? description,
        Guid assetCategoryId,
        string? serialNumber,
        DateOnly? purchaseDate,
        decimal? acquisitionCost,
        string? acquisitionReference,
        DateOnly? warrantyStartDate,
        DateOnly? warrantyEndDate,
        string? warrantyNotes,
        string? updatedBy,
        DateTime utcNow)
    {
        EnsureNotRetired();
        EnsureWarrantyValid(warrantyStartDate, warrantyEndDate);
        if (acquisitionCost is < 0)
            throw new ArgumentOutOfRangeException(nameof(acquisitionCost));

        Name = Require(name, 200);
        Description = Normalize(description, 500);
        AssetCategoryId = assetCategoryId;
        SerialNumber = Normalize(serialNumber, 128);
        PurchaseDate = purchaseDate;
        AcquisitionCost = acquisitionCost;
        AcquisitionReference = Normalize(acquisitionReference, 500);
        WarrantyStartDate = warrantyStartDate;
        WarrantyEndDate = warrantyEndDate;
        WarrantyNotes = Normalize(warrantyNotes, 500);
        SetUpdated(updatedBy, utcNow);
    }

    public void ChangeLocation(Guid assetLocationId, string? updatedBy, DateTime utcNow)
    {
        EnsureNotRetired();
        AssetLocationId = assetLocationId;
        SetUpdated(updatedBy, utcNow);
    }

    public void StartMaintenance(string? notes, string? updatedBy, DateTime utcNow)
    {
        if (Status != AssetStatus.Active)
            throw new InvalidOperationException("Maintenance can only be started for active assets.");

        Status = AssetStatus.UnderMaintenance;
        MaintenanceNotes = Normalize(notes, 500);
        SetUpdated(updatedBy, utcNow);
    }

    public void CompleteMaintenance(string? updatedBy, DateTime utcNow)
    {
        if (Status != AssetStatus.UnderMaintenance)
            throw new InvalidOperationException("Maintenance can only be completed for assets under maintenance.");

        Status = AssetStatus.Active;
        MaintenanceNotes = null;
        SetUpdated(updatedBy, utcNow);
    }

    public void Retire(string reason, string? retiredBy, DateTime utcNow)
    {
        if (Status == AssetStatus.Retired)
            throw new InvalidOperationException("Asset is already retired.");

        Status = AssetStatus.Retired;
        RetirementReason = Require(reason, 500);
        RetiredAtUtc = utcNow;
        RetiredBy = string.IsNullOrWhiteSpace(retiredBy) ? null : retiredBy.Trim();
        MaintenanceNotes = null;
        SetUpdated(retiredBy, utcNow);
    }

    private static void EnsureWarrantyValid(DateOnly? start, DateOnly? end)
    {
        if (start.HasValue && end.HasValue && end.Value < start.Value)
            throw new ArgumentException("Warranty end date must be on or after warranty start date.");
        if (end.HasValue && !start.HasValue)
            throw new ArgumentException("Warranty start date is required when warranty end date is set.");
    }

    private void EnsureNotRetired()
    {
        if (Status == AssetStatus.Retired)
            throw new InvalidOperationException("Retired assets cannot be modified.");
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
