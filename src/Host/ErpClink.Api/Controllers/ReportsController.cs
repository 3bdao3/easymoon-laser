using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Reports.Application;
using ErpClink.Modules.Reports.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportingQueryService _reports;

    public ReportsController(IReportingQueryService reports) => _reports = reports;

    [HttpGet("finance/general-ledger")]
    [HasPermission(PermissionCodes.ReportsFinanceGeneralLedgerView)]
    public Task<ReportPage<GlReportLine>> Gl(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] Guid? branchId,
        [FromQuery] Guid? accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        _reports.GetGeneralLedgerAsync(new DateRangeFilter(fromDate, toDate, branchId, page, pageSize), accountId, cancellationToken);

    [HttpGet("finance/trial-balance")]
    [HasPermission(PermissionCodes.ReportsFinanceTrialBalanceView)]
    public Task<TrialBalanceReportResult> TrialBalance(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default) =>
        _reports.GetTrialBalanceAsync(fromDate, toDate, branchId, cancellationToken);

    [HttpGet("finance/journals")]
    [HasPermission(PermissionCodes.ReportsFinanceJournalsView)]
    public Task<ReportPage<JournalRegisterLine>> Journals(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        _reports.GetJournalRegisterAsync(new DateRangeFilter(fromDate, toDate, null, page, pageSize), status, cancellationToken);

    [HttpGet("ar/statement")]
    [HasPermission(PermissionCodes.ReportsArStatementView)]
    public Task<PartyStatementReport> ArStatement(
        [FromQuery] Guid partyId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? branchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        _reports.GetArStatementAsync(partyId, new DateRangeFilter(fromDate, toDate, branchId, page, pageSize), cancellationToken);

    [HttpGet("ar/aging")]
    [HasPermission(PermissionCodes.ReportsArAgingView)]
    public Task<AgingReportResult> ArAging(
        [FromQuery] Guid? partyId, [FromQuery] Guid? branchId, [FromQuery] DateOnly? asOfDate,
        CancellationToken cancellationToken = default) =>
        _reports.GetArAgingAsync(partyId, branchId, asOfDate, cancellationToken);

    [HttpGet("ar/outstanding")]
    [HasPermission(PermissionCodes.ReportsArAgingView)]
    public Task<ReportPage<OutstandingPartyReportLine>> ArOutstanding(
        [FromQuery] Guid? branchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        _reports.GetOutstandingReceivablesAsync(branchId, page, pageSize, cancellationToken);

    [HttpGet("ap/statement")]
    [HasPermission(PermissionCodes.ReportsApStatementView)]
    public Task<PartyStatementReport> ApStatement(
        [FromQuery] Guid partyId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? branchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        _reports.GetApStatementAsync(partyId, new DateRangeFilter(fromDate, toDate, branchId, page, pageSize), cancellationToken);

    [HttpGet("ap/aging")]
    [HasPermission(PermissionCodes.ReportsApAgingView)]
    public Task<AgingReportResult> ApAging(
        [FromQuery] Guid? partyId, [FromQuery] Guid? branchId, [FromQuery] DateOnly? asOfDate,
        CancellationToken cancellationToken = default) =>
        _reports.GetApAgingAsync(partyId, branchId, asOfDate, cancellationToken);

    [HttpGet("ap/outstanding")]
    [HasPermission(PermissionCodes.ReportsApAgingView)]
    public Task<ReportPage<OutstandingPartyReportLine>> ApOutstanding(
        [FromQuery] Guid? branchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        _reports.GetOutstandingPayablesAsync(branchId, page, pageSize, cancellationToken);

    [HttpGet("billing/invoices")]
    [HasPermission(PermissionCodes.ReportsBillingInvoicesView)]
    public Task<ReportPage<InvoiceRegisterLine>> BillingInvoices(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string? status,
        [FromQuery] Guid? patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        _reports.GetInvoiceRegisterAsync(new DateRangeFilter(fromDate, toDate, null, page, pageSize), status, patientId, cancellationToken);

    [HttpGet("billing/payments")]
    [HasPermission(PermissionCodes.ReportsBillingPaymentsView)]
    public Task<ReportPage<PaymentRegisterLine>> BillingPayments(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string? status,
        [FromQuery] Guid? patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        _reports.GetPaymentRegisterAsync(new DateRangeFilter(fromDate, toDate, null, page, pageSize), status, patientId, cancellationToken);

    [HttpGet("billing/outstanding")]
    [HasPermission(PermissionCodes.ReportsBillingOutstandingView)]
    public Task<ReportPage<InvoiceRegisterLine>> BillingOutstanding(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] Guid? patientId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        _reports.GetOutstandingInvoicesAsync(new DateRangeFilter(fromDate, toDate, null, page, pageSize), patientId, cancellationToken);

    [HttpGet("inventory/stock")]
    [HasPermission(PermissionCodes.ReportsInventoryStockView)]
    public Task<ReportPage<StockBalanceReportLine>> Stock(
        [FromQuery] Guid? warehouseId, [FromQuery] Guid? inventoryItemId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        _reports.GetStockBalancesAsync(warehouseId, inventoryItemId, page, pageSize, cancellationToken);

    [HttpGet("inventory/movements")]
    [HasPermission(PermissionCodes.ReportsInventoryMovementsView)]
    public Task<ReportPage<StockMovementReportLine>> Movements(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? warehouseId, [FromQuery] Guid? inventoryItemId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        _reports.GetStockMovementsAsync(new DateRangeFilter(fromDate, toDate, null, page, pageSize), warehouseId, inventoryItemId, cancellationToken);

    [HttpGet("inventory/valuation")]
    [HasPermission(PermissionCodes.ReportsInventoryValuationView)]
    public Task<ReportPage<InventoryValuationReportLine>> InventoryValuation(
        [FromQuery] Guid? warehouseId, [FromQuery] Guid? inventoryItemId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        _reports.GetInventoryValuationAsync(warehouseId, inventoryItemId, page, pageSize, cancellationToken);

    [HttpGet("inventory/cogs")]
    [HasPermission(PermissionCodes.ReportsInventoryCogsView)]
    public Task<ReportPage<InventoryCogsReportLine>> InventoryCogs(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? warehouseId, [FromQuery] Guid? inventoryItemId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        _reports.GetInventoryCogsAsync(new DateRangeFilter(fromDate, toDate, null, page, pageSize), warehouseId, inventoryItemId, cancellationToken);

    [HttpGet("assets/register")]
    [HasPermission(PermissionCodes.ReportsAssetsRegisterView)]
    public Task<ReportPage<AssetRegisterReportLine>> AssetRegister(
        [FromQuery] string? financialStatus, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        _reports.GetAssetRegisterAsync(financialStatus, page, pageSize, cancellationToken);

    [HttpGet("assets/depreciation")]
    [HasPermission(PermissionCodes.ReportsAssetsDepreciationView)]
    public Task<ReportPage<AssetDepreciationReportLine>> AssetDepreciation(
        [FromQuery] Guid? assetId, [FromQuery] DateOnly? fromPeriod, [FromQuery] DateOnly? toPeriod,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        _reports.GetAssetDepreciationAsync(assetId, fromPeriod, toPeriod, page, pageSize, cancellationToken);

    [HttpGet("assets/valuation")]
    [HasPermission(PermissionCodes.ReportsAssetsValuationView)]
    public Task<AssetValuationReportResult> AssetValuation(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        _reports.GetAssetValuationAsync(page, pageSize, cancellationToken);

    [HttpGet("procurement/purchase-orders")]
    [HasPermission(PermissionCodes.ReportsProcurementPurchaseOrdersView)]
    public Task<ReportPage<PurchaseOrderRegisterLine>> PurchaseOrders(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string? status,
        [FromQuery] Guid? supplierId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        _reports.GetPurchaseOrderRegisterAsync(new DateRangeFilter(fromDate, toDate, null, page, pageSize), status, supplierId, cancellationToken);

    [HttpGet("procurement/receiving")]
    [HasPermission(PermissionCodes.ReportsProcurementReceivingView)]
    public Task<ReportPage<ReceivingSummaryLine>> Receiving(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? warehouseId, [FromQuery] Guid? purchaseOrderId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        _reports.GetReceivingSummaryAsync(new DateRangeFilter(fromDate, toDate, null, page, pageSize), warehouseId, purchaseOrderId, cancellationToken);

    [HttpGet("management/summary")]
    [HasPermission(PermissionCodes.ReportsManagementSummaryView)]
    public Task<ManagementSummaryResult> ManagementSummary(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default) =>
        _reports.GetManagementSummaryAsync(fromDate, toDate, branchId, cancellationToken);
}
