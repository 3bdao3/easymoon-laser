namespace ErpClink.Modules.Procurement.Application.Contracts;



public interface IPurchaseOrderLookup

{

    Task<PurchaseOrderDetailLookup?> GetApprovedByIdAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);

}



public sealed record PurchaseOrderLineLookup(

    Guid Id,

    Guid? CatalogItemId,

    string DescriptionSnapshot,

    decimal Quantity,

    decimal QuantityReceived,

    decimal UnitCost);



public sealed record PurchaseOrderDetailLookup(

    Guid Id,

    string PurchaseOrderNumber,

    Guid SupplierId,

    string Status,

    IReadOnlyList<PurchaseOrderLineLookup> Lines);

