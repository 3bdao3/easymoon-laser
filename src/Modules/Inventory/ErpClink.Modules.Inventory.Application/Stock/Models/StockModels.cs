namespace ErpClink.Modules.Inventory.Application.Stock.Models;

public sealed record SearchStockBalancesRequest(
    Guid? WarehouseId,
    Guid? InventoryItemId,
    int Page = 1,
    int PageSize = 20);

public sealed record StockBalanceDto(
    Guid Id,
    Guid WarehouseId,
    Guid InventoryItemId,
    decimal Quantity,
    decimal InventoryValue,
    decimal? AverageUnitCost,
    byte[] RowVersion);

public sealed record PagedStockBalancesResult(
    IReadOnlyList<StockBalanceDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdjustStockRequest(
    Guid WarehouseId,
    Guid InventoryItemId,
    string Direction,
    decimal Quantity,
    string Reason,
    Guid? BatchId,
    decimal? UnitCost,
    byte[]? BalanceRowVersion);

public sealed record SearchStockMovementsRequest(
    Guid? WarehouseId,
    Guid? InventoryItemId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? MovementType,
    int Page = 1,
    int PageSize = 50);

public sealed record StockMovementDto(
    Guid Id,
    Guid WarehouseId,
    Guid InventoryItemId,
    Guid? BatchId,
    decimal Quantity,
    string MovementType,
    string ReferenceType,
    Guid ReferenceId,
    string? Reason,
    decimal BalanceAfter,
    DateTime CreatedAtUtc,
    string? CreatedBy);

public sealed record PagedStockMovementsResult(
    IReadOnlyList<StockMovementDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
