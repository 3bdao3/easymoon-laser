using ErpClink.Modules.Inventory.Domain.Costing;

namespace ErpClink.Modules.Inventory.Application.Costing;

public sealed record CostReceiptLineRequest(
    Guid InventoryItemId,
    Guid? StockBatchId,
    decimal Quantity,
    decimal UnitCost,
    Guid? StockMovementId);

public sealed record CostIssueRequest(
    Guid WarehouseId,
    Guid InventoryItemId,
    Guid? StockBatchId,
    decimal Quantity,
    Guid? StockMovementId,
    DateOnly TransactionDate,
    string SourceType,
    Guid SourceId,
    string EventType,
    string IdempotencyKey,
    Guid? CorrelationId,
    string? Description,
    InventoryCostTransactionType TransactionType = InventoryCostTransactionType.Issue);

public sealed record CostReceiptRequest(
    Guid WarehouseId,
    DateOnly TransactionDate,
    string SourceType,
    Guid SourceId,
    string EventType,
    string IdempotencyKeyPrefix,
    Guid? CorrelationId,
    string? Description,
    IReadOnlyList<CostReceiptLineRequest> Lines,
    InventoryCostTransactionType TransactionType = InventoryCostTransactionType.Receipt);

public sealed record CostReversalRequest(
    Guid WarehouseId,
    Guid SourceId,
    string SourceType,
    string EventType,
    string IdempotencyKey,
    Guid? CorrelationId,
    string? Description);

public sealed record CostingLineResult(
    Guid InventoryItemId,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    Guid CostTransactionId,
    IReadOnlyList<CostAllocationResult> Allocations);

public sealed record CostAllocationResult(
    Guid CostLayerId,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost);

public sealed record CostingResult(
    bool AlreadyProcessed,
    IReadOnlyList<CostingLineResult> Lines);

/// <summary>
/// Valuation strategy. Default implementation is provisional FIFO (ADR-015) until product approves a method.
/// </summary>
public interface IInventoryValuationMethod
{
    InventoryValuationMethodCode Method { get; }

    Task<CostingResult> ApplyReceiptAsync(CostReceiptRequest request, CancellationToken cancellationToken = default);
    Task<CostingResult> ApplyIssueAsync(CostIssueRequest request, CancellationToken cancellationToken = default);
    Task<CostingResult> ReverseReceiptAsync(CostReversalRequest request, CancellationToken cancellationToken = default);
}
