using ErpClink.Modules.Procurement.Application.PurchaseOrders.Models;

namespace ErpClink.Modules.Procurement.Application.PurchaseOrders;

public interface IPurchaseOrderService
{
    Task<PurchaseOrderDto> CreateDraftAsync(CreateDraftPurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> GetByNumberAsync(string purchaseOrderNumber, CancellationToken cancellationToken = default);
    Task<PagedPurchaseOrdersResult> SearchAsync(SearchPurchaseOrdersRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> UpdateDraftAsync(Guid id, UpdateDraftPurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> AddLineAsync(Guid id, AddPurchaseOrderLineRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> UpdateLineAsync(Guid id, Guid lineId, UpdatePurchaseOrderLineRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> RemoveLineAsync(Guid id, Guid lineId, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> SetDiscountAsync(Guid id, SetPurchaseOrderDiscountRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> SubmitAsync(Guid id, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> ApproveAsync(Guid id, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> CancelAsync(Guid id, CancelPurchaseOrderRequest request, CancellationToken cancellationToken = default);
}

public interface IPurchaseOrderNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
