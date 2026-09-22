namespace ErpClink.Modules.Inventory.Application.Costing.Models;

public sealed record ValuationQueryRequest(
    Guid? WarehouseId,
    Guid? InventoryItemId,
    Guid? CategoryId,
    int Page = 1,
    int PageSize = 50);

public sealed record ValuationLineDto(
    Guid WarehouseId,
    Guid InventoryItemId,
    string? ItemCode,
    string? ItemName,
    decimal Quantity,
    decimal InventoryValue,
    decimal? AverageUnitCost);

public sealed record ValuationResult(
    decimal TotalQuantity,
    decimal TotalValue,
    string ValuationMethod,
    IReadOnlyList<ValuationLineDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CostHistoryQueryRequest(
    Guid InventoryItemId,
    Guid? WarehouseId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 50);

public sealed record CostHistoryLineDto(
    Guid Id,
    Guid WarehouseId,
    string TransactionType,
    DateOnly TransactionDate,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    string SourceType,
    Guid SourceId,
    string EventType,
    string ValuationMethod,
    DateTime CreatedAtUtc);

public sealed record PagedCostHistoryResult(
    IReadOnlyList<CostHistoryLineDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CostLayerQueryRequest(
    Guid? WarehouseId,
    Guid? InventoryItemId,
    Guid? StockBatchId,
    string? Status,
    int Page = 1,
    int PageSize = 50);

public sealed record CostLayerDto(
    Guid Id,
    Guid WarehouseId,
    Guid InventoryItemId,
    Guid? StockBatchId,
    string SourceType,
    Guid SourceId,
    DateOnly ReceiptDate,
    decimal OriginalQuantity,
    decimal RemainingQuantity,
    decimal UnitCost,
    decimal RemainingValue,
    string Status);

public sealed record PagedCostLayersResult(
    IReadOnlyList<CostLayerDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record IssueCostQueryRequest(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? WarehouseId,
    Guid? InventoryItemId,
    int Page = 1,
    int PageSize = 50);

public sealed record IssueCostLineDto(
    Guid Id,
    Guid WarehouseId,
    Guid InventoryItemId,
    DateOnly TransactionDate,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    string SourceType,
    Guid SourceId,
    string EventType);

public sealed record PagedIssueCostResult(
    IReadOnlyList<IssueCostLineDto> Items,
    decimal TotalIssueCost,
    int TotalCount,
    int Page,
    int PageSize);

public interface IInventoryValuationQueryService
{
    Task<ValuationResult> GetValuationAsync(ValuationQueryRequest request, CancellationToken cancellationToken = default);
    Task<PagedCostHistoryResult> GetCostHistoryAsync(CostHistoryQueryRequest request, CancellationToken cancellationToken = default);
    Task<PagedCostLayersResult> GetCostLayersAsync(CostLayerQueryRequest request, CancellationToken cancellationToken = default);
    Task<PagedIssueCostResult> GetIssueCostsAsync(IssueCostQueryRequest request, CancellationToken cancellationToken = default);
}
