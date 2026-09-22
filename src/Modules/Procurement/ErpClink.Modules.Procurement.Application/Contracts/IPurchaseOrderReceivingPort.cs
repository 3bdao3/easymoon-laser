namespace ErpClink.Modules.Procurement.Application.Contracts;

public interface IPurchaseOrderReceivingPort
{
    Task<PurchaseOrderReceivingSnapshot?> GetReceivableAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task ApplyReceiptAsync(ApplyPurchaseOrderReceiptRequest request, CancellationToken cancellationToken = default);
    Task ReverseReceiptAsync(ApplyPurchaseOrderReceiptRequest request, CancellationToken cancellationToken = default);
}

public sealed record PurchaseOrderReceivingLineSnapshot(
    Guid LineId,
    Guid? CatalogItemId,
    string DescriptionSnapshot,
    decimal QuantityOrdered,
    decimal QuantityReceived,
    decimal UnitCost);

public sealed record PurchaseOrderReceivingSnapshot(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string PurchaseOrderNumber,
    Guid SupplierId,
    string Status,
    IReadOnlyList<PurchaseOrderReceivingLineSnapshot> Lines,
    byte[] RowVersion);

public sealed record ApplyPurchaseOrderReceiptLine(Guid PurchaseOrderLineId, decimal QuantityReceivedDelta);

public sealed record ApplyPurchaseOrderReceiptRequest(
    Guid PurchaseOrderId,
    IReadOnlyList<ApplyPurchaseOrderReceiptLine> Lines,
    byte[]? PurchaseOrderRowVersion);
