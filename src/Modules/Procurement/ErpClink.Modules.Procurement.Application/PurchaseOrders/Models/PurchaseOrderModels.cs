namespace ErpClink.Modules.Procurement.Application.PurchaseOrders.Models;



public sealed record PurchaseOrderLineInputDto(

    Guid? CatalogItemId,

    string DescriptionSnapshot,

    decimal Quantity,

    decimal UnitCost,

    decimal? LineDiscountAmount,

    int? SortOrder);



public sealed record CreateDraftPurchaseOrderRequest(

    Guid SupplierId,

    DateOnly OrderDate,

    string CurrencyCode,

    string? Notes,

    decimal? DiscountAmount,

    IReadOnlyList<PurchaseOrderLineInputDto>? Lines);



public sealed record UpdateDraftPurchaseOrderRequest(

    Guid SupplierId,

    DateOnly OrderDate,

    string? Notes,

    decimal? DiscountAmount,

    byte[]? RowVersion);



public sealed record AddPurchaseOrderLineRequest(

    PurchaseOrderLineInputDto Line,

    byte[]? RowVersion);



public sealed record UpdatePurchaseOrderLineRequest(

    decimal Quantity,

    decimal LineDiscountAmount,

    int SortOrder,

    byte[]? RowVersion);



public sealed record SetPurchaseOrderDiscountRequest(decimal DiscountAmount, byte[]? RowVersion);



public sealed record CancelPurchaseOrderRequest(string? Reason, byte[]? RowVersion);



public sealed record SearchPurchaseOrdersRequest(

    Guid? SupplierId,

    string? PurchaseOrderNumber,

    DateOnly? DateFrom,

    DateOnly? DateTo,

    string? Status,

    int Page = 1,

    int PageSize = 20);



public sealed record PurchaseOrderLineDto(

    Guid Id,

    Guid? CatalogItemId,

    string DescriptionSnapshot,

    decimal Quantity,

    decimal QuantityReceived,

    decimal UnitCost,

    decimal LineDiscountAmount,

    decimal LineSubtotal,

    decimal LineTotal,

    int SortOrder);



public sealed record PurchaseOrderDto(

    Guid Id,

    Guid OrganizationId,

    Guid BranchId,

    string PurchaseOrderNumber,

    Guid SupplierId,

    DateOnly OrderDate,

    string Status,

    string CurrencyCode,

    decimal SubTotal,

    decimal DiscountAmount,

    decimal TaxAmount,

    decimal TotalAmount,

    string? Notes,

    DateTime? SubmittedAtUtc,

    string? SubmittedBy,

    DateTime? ApprovedAtUtc,

    string? ApprovedBy,

    DateTime? CancelledAtUtc,

    string? CancelledBy,

    string? CancellationReason,

    DateTime CreatedAtUtc,

    string? CreatedBy,

    DateTime? UpdatedAtUtc,

    string? UpdatedBy,

    byte[] RowVersion,

    IReadOnlyList<PurchaseOrderLineDto> Lines);



public sealed record PagedPurchaseOrdersResult(

    IReadOnlyList<PurchaseOrderDto> Items,

    int TotalCount,

    int Page,

    int PageSize);

