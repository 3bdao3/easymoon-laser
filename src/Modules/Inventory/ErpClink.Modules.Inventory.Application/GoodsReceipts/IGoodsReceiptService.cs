using ErpClink.Modules.Inventory.Application.GoodsReceipts.Models;

namespace ErpClink.Modules.Inventory.Application.GoodsReceipts;

public interface IGoodsReceiptService
{
    Task<GoodsReceiptDto> CreateFromPurchaseOrderAsync(CreateGoodsReceiptFromPoRequest request, CancellationToken cancellationToken = default);
    Task<GoodsReceiptDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedGoodsReceiptsResult> SearchAsync(SearchGoodsReceiptsRequest request, CancellationToken cancellationToken = default);
    Task<GoodsReceiptDto> CancelAsync(Guid id, CancelGoodsReceiptRequest request, CancellationToken cancellationToken = default);
}

public interface IGoodsReceiptNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
