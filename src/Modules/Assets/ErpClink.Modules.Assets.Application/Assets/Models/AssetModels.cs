namespace ErpClink.Modules.Assets.Application.Assets.Models;

public sealed record CreateAssetRequest(
    string Name,
    string? Description,
    Guid AssetCategoryId,
    Guid AssetLocationId,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    decimal? AcquisitionCost,
    string? AcquisitionReference,
    DateOnly? WarrantyStartDate,
    DateOnly? WarrantyEndDate,
    string? WarrantyNotes);

public sealed record UpdateAssetRequest(
    string Name,
    string? Description,
    Guid AssetCategoryId,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    decimal? AcquisitionCost,
    string? AcquisitionReference,
    DateOnly? WarrantyStartDate,
    DateOnly? WarrantyEndDate,
    string? WarrantyNotes,
    byte[]? RowVersion);

public sealed record ChangeAssetLocationRequest(Guid AssetLocationId, byte[]? RowVersion);
public sealed record StartAssetMaintenanceRequest(string? Notes, byte[]? RowVersion);
public sealed record CompleteAssetMaintenanceRequest(byte[]? RowVersion);
public sealed record RetireAssetRequest(string Reason, byte[]? RowVersion);

public sealed record SearchAssetsRequest(
    string? Query,
    Guid? AssetCategoryId,
    Guid? AssetLocationId,
    string? Status,
    int Page = 1,
    int PageSize = 20);

public sealed record AssetDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string AssetNumber,
    string Name,
    string? Description,
    Guid AssetCategoryId,
    Guid AssetLocationId,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    decimal? AcquisitionCost,
    string? AcquisitionReference,
    DateOnly? WarrantyStartDate,
    DateOnly? WarrantyEndDate,
    string? WarrantyNotes,
    string Status,
    string? MaintenanceNotes,
    DateTime? RetiredAtUtc,
    string? RetirementReason,
    DateTime CreatedAtUtc,
    byte[] RowVersion);

public sealed record PagedAssetsResult(
    IReadOnlyList<AssetDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AssetHistoryEntryDto(
    Guid Id,
    string EventType,
    string Summary,
    string? OldValue,
    string? NewValue,
    string? Notes,
    DateTime OccurredAtUtc,
    string? OccurredBy);
