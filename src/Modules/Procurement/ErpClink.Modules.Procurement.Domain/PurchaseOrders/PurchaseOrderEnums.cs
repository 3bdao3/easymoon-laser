namespace ErpClink.Modules.Procurement.Domain.PurchaseOrders;



/// <summary>

/// Step 14: Draft → Submitted → Approved and Cancelled.

/// Step 15: Inventory receiving transitions Approved|Ordered|PartiallyReceived → PartiallyReceived|Received.

/// </summary>

public enum PurchaseOrderStatus

{

    Draft = 0,

    Submitted = 1,

    Approved = 2,

    Cancelled = 3,

    Ordered = 4,

    PartiallyReceived = 5,

    Received = 6

}

