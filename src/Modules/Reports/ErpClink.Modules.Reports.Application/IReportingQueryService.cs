using ErpClink.Modules.Reports.Application.Models;

namespace ErpClink.Modules.Reports.Application;

/// <summary>
/// Read-only reporting façade. Delegates to owning modules' Application query contracts.
/// Does not create a second source of financial truth.
/// </summary>
public interface IReportingQueryService
{
    Task<ReportPage<GlReportLine>> GetGeneralLedgerAsync(DateRangeFilter filter, Guid? accountId, CancellationToken cancellationToken = default);
    Task<TrialBalanceReportResult> GetTrialBalanceAsync(DateOnly fromDate, DateOnly toDate, Guid? branchId, CancellationToken cancellationToken = default);
    Task<ReportPage<JournalRegisterLine>> GetJournalRegisterAsync(DateRangeFilter filter, string? status, CancellationToken cancellationToken = default);

    Task<PartyStatementReport> GetArStatementAsync(Guid partyId, DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<AgingReportResult> GetArAgingAsync(Guid? partyId, Guid? branchId, DateOnly? asOfDate, CancellationToken cancellationToken = default);
    Task<ReportPage<OutstandingPartyReportLine>> GetOutstandingReceivablesAsync(Guid? branchId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<PartyStatementReport> GetApStatementAsync(Guid partyId, DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<AgingReportResult> GetApAgingAsync(Guid? partyId, Guid? branchId, DateOnly? asOfDate, CancellationToken cancellationToken = default);
    Task<ReportPage<OutstandingPartyReportLine>> GetOutstandingPayablesAsync(Guid? branchId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<ReportPage<InvoiceRegisterLine>> GetInvoiceRegisterAsync(DateRangeFilter filter, string? status, Guid? patientId, CancellationToken cancellationToken = default);
    Task<ReportPage<PaymentRegisterLine>> GetPaymentRegisterAsync(DateRangeFilter filter, string? status, Guid? patientId, CancellationToken cancellationToken = default);
    Task<ReportPage<InvoiceRegisterLine>> GetOutstandingInvoicesAsync(DateRangeFilter filter, Guid? patientId, CancellationToken cancellationToken = default);

    Task<ReportPage<StockBalanceReportLine>> GetStockBalancesAsync(Guid? warehouseId, Guid? itemId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<ReportPage<StockMovementReportLine>> GetStockMovementsAsync(DateRangeFilter filter, Guid? warehouseId, Guid? itemId, CancellationToken cancellationToken = default);
    Task<ReportPage<InventoryValuationReportLine>> GetInventoryValuationAsync(Guid? warehouseId, Guid? itemId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<ReportPage<InventoryCogsReportLine>> GetInventoryCogsAsync(DateRangeFilter filter, Guid? warehouseId, Guid? itemId, CancellationToken cancellationToken = default);

    Task<ReportPage<AssetRegisterReportLine>> GetAssetRegisterAsync(string? financialStatus, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<ReportPage<AssetDepreciationReportLine>> GetAssetDepreciationAsync(Guid? assetId, DateOnly? fromPeriod, DateOnly? toPeriod, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<AssetValuationReportResult> GetAssetValuationAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<ReportPage<PurchaseOrderRegisterLine>> GetPurchaseOrderRegisterAsync(DateRangeFilter filter, string? status, Guid? supplierId, CancellationToken cancellationToken = default);
    Task<ReportPage<ReceivingSummaryLine>> GetReceivingSummaryAsync(DateRangeFilter filter, Guid? warehouseId, Guid? purchaseOrderId, CancellationToken cancellationToken = default);

    Task<ManagementSummaryResult> GetManagementSummaryAsync(DateOnly? fromDate, DateOnly? toDate, Guid? branchId, CancellationToken cancellationToken = default);
}
