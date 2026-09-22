using ErpClink.Modules.Inventory.Application.Stock.Models;

namespace ErpClink.Modules.Inventory.Application.Stock;

public interface IStockService
{
    Task<PagedStockBalancesResult> SearchBalancesAsync(SearchStockBalancesRequest request, CancellationToken cancellationToken = default);
    Task<PagedStockMovementsResult> SearchMovementsAsync(SearchStockMovementsRequest request, CancellationToken cancellationToken = default);
    Task AdjustAsync(AdjustStockRequest request, CancellationToken cancellationToken = default);
}
