namespace ErpClink.Modules.Inventory.Application.Warehouses.Models;

public sealed record CreateWarehouseRequest(string Name, string? Description);
public sealed record UpdateWarehouseRequest(string Name, string? Description, byte[]? RowVersion);
public sealed record SearchWarehousesRequest(string? Query, bool? IsActive, int Page = 1, int PageSize = 20);

public sealed record WarehouseDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string WarehouseCode,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    byte[] RowVersion);

public sealed record PagedWarehousesResult(
    IReadOnlyList<WarehouseDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
