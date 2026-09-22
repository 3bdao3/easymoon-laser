using ErpClink.Modules.Assets.Application.Accounting.Models;
using ErpClink.Modules.Billing.Application.Invoices;
using ErpClink.Modules.Billing.Application.Invoices.Models;
using ErpClink.Modules.Billing.Application.Payments;
using ErpClink.Modules.Billing.Application.Payments.Models;
using ErpClink.Modules.Finance.Application.GeneralLedger;
using ErpClink.Modules.Finance.Application.GeneralLedger.Models;
using ErpClink.Modules.Finance.Application.Journals;
using ErpClink.Modules.Finance.Application.Journals.Models;
using ErpClink.Modules.Finance.Application.Subledgers;
using ErpClink.Modules.Finance.Application.Subledgers.Models;
using ErpClink.Modules.Finance.Application.TrialBalance;
using ErpClink.Modules.Finance.Application.TrialBalance.Models;
using ErpClink.Modules.Inventory.Application.Costing.Models;
using ErpClink.Modules.Inventory.Application.GoodsReceipts;
using ErpClink.Modules.Inventory.Application.GoodsReceipts.Models;
using ErpClink.Modules.Inventory.Application.Stock;
using ErpClink.Modules.Inventory.Application.Stock.Models;
using ErpClink.Modules.Procurement.Application.PurchaseOrders;
using ErpClink.Modules.Procurement.Application.PurchaseOrders.Models;
using ErpClink.Modules.Reports.Application;
using ErpClink.Modules.Reports.Application.Models;

namespace ErpClink.Modules.Reports.Infrastructure;

/// <summary>
/// Composes existing Application query ports. No cross-module Infrastructure references.
/// </summary>
public sealed class ReportingQueryService : IReportingQueryService
{
    private const string DueDateNote =
        "Aging uses DueDate when present; otherwise TransactionDate (Billing invoices currently have no due date).";

    private readonly IGeneralLedgerService _gl;
    private readonly ITrialBalanceService _tb;
    private readonly IJournalService _journals;
    private readonly IArSubledgerQueryService _ar;
    private readonly IApSubledgerQueryService _ap;
    private readonly IInvoiceService _invoices;
    private readonly IPaymentService _payments;
    private readonly IStockService _stock;
    private readonly IInventoryValuationQueryService _inventoryValuation;
    private readonly IGoodsReceiptService _goodsReceipts;
    private readonly IAssetAccountingService _assetsAccounting;
    private readonly IPurchaseOrderService _purchaseOrders;

    public ReportingQueryService(
        IGeneralLedgerService gl,
        ITrialBalanceService tb,
        IJournalService journals,
        IArSubledgerQueryService ar,
        IApSubledgerQueryService ap,
        IInvoiceService invoices,
        IPaymentService payments,
        IStockService stock,
        IInventoryValuationQueryService inventoryValuation,
        IGoodsReceiptService goodsReceipts,
        IAssetAccountingService assetsAccounting,
        IPurchaseOrderService purchaseOrders)
    {
        _gl = gl;
        _tb = tb;
        _journals = journals;
        _ar = ar;
        _ap = ap;
        _invoices = invoices;
        _payments = payments;
        _stock = stock;
        _inventoryValuation = inventoryValuation;
        _goodsReceipts = goodsReceipts;
        _assetsAccounting = assetsAccounting;
        _purchaseOrders = purchaseOrders;
    }

    public async Task<ReportPage<GlReportLine>> GetGeneralLedgerAsync(
        DateRangeFilter filter, Guid? accountId, CancellationToken cancellationToken = default)
    {
        var result = await _gl.SearchAsync(new SearchGeneralLedgerRequest(
            accountId, filter.BranchId, filter.FromDate, filter.ToDate, filter.Page, filter.PageSize), cancellationToken);
        var items = result.Items.Select(x => new GlReportLine(
            x.JournalDate, x.JournalNumber, x.LineDescription, x.Debit, x.Credit,
            x.AccountId, x.JournalStatus, x.JournalEntryId, x.LineId, x.PostedAtUtc)).ToList();
        return Page(items, result.TotalCount, result.Page, result.PageSize, "Finance");
    }

    public async Task<TrialBalanceReportResult> GetTrialBalanceAsync(
        DateOnly fromDate, DateOnly toDate, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var result = await _tb.GetAsync(new GetTrialBalanceRequest(fromDate, toDate, branchId), cancellationToken);
        return new TrialBalanceReportResult(
            result.FromDate, result.ToDate,
            result.Lines.Select(l => new TrialBalanceReportLine(
                l.AccountId, l.AccountCode, l.AccountName, l.AccountType,
                l.TotalDebit, l.TotalCredit, l.NetDebit, l.NetCredit)).ToList(),
            result.GrandTotalDebit, result.GrandTotalCredit, "Finance");
    }

    public async Task<ReportPage<JournalRegisterLine>> GetJournalRegisterAsync(
        DateRangeFilter filter, string? status, CancellationToken cancellationToken = default)
    {
        var result = await _journals.SearchAsync(new SearchJournalsRequest(
            null, status, filter.FromDate, filter.ToDate, filter.Page, filter.PageSize), cancellationToken);
        var items = result.Items.Select(j => new JournalRegisterLine(
            j.Id, j.JournalNumber, j.JournalDate, j.Description, j.Status,
            j.TotalDebit, j.TotalCredit, null, j.PostedBy, j.PostedAtUtc)).ToList();
        return Page(items, result.TotalCount, result.Page, result.PageSize, "Finance");
    }

    public async Task<PartyStatementReport> GetArStatementAsync(
        Guid partyId, DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var s = await _ar.GetCustomerStatementAsync(new StatementRequest(
            partyId, filter.BranchId, filter.FromDate, filter.ToDate, filter.Page, filter.PageSize), cancellationToken);
        return MapStatement(s);
    }

    public async Task<AgingReportResult> GetArAgingAsync(
        Guid? partyId, Guid? branchId, DateOnly? asOfDate, CancellationToken cancellationToken = default) =>
        MapAging(await _ar.GetAgingAsync(new AgingRequest(partyId, branchId, asOfDate), cancellationToken));

    public async Task<ReportPage<OutstandingPartyReportLine>> GetOutstandingReceivablesAsync(
        Guid? branchId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var aging = await _ar.GetAgingAsync(new AgingRequest(null, branchId, null), cancellationToken);
        return OutstandingFromAging(aging, page, pageSize);
    }

    public async Task<PartyStatementReport> GetApStatementAsync(
        Guid partyId, DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var s = await _ap.GetSupplierStatementAsync(new StatementRequest(
            partyId, filter.BranchId, filter.FromDate, filter.ToDate, filter.Page, filter.PageSize), cancellationToken);
        return MapStatement(s);
    }

    public async Task<AgingReportResult> GetApAgingAsync(
        Guid? partyId, Guid? branchId, DateOnly? asOfDate, CancellationToken cancellationToken = default) =>
        MapAging(await _ap.GetAgingAsync(new AgingRequest(partyId, branchId, asOfDate), cancellationToken));

    public async Task<ReportPage<OutstandingPartyReportLine>> GetOutstandingPayablesAsync(
        Guid? branchId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var aging = await _ap.GetAgingAsync(new AgingRequest(null, branchId, null), cancellationToken);
        return OutstandingFromAging(aging, page, pageSize);
    }

    public async Task<ReportPage<InvoiceRegisterLine>> GetInvoiceRegisterAsync(
        DateRangeFilter filter, string? status, Guid? patientId, CancellationToken cancellationToken = default)
    {
        var result = await _invoices.SearchAsync(new SearchInvoicesRequest(
            patientId, null, null, filter.FromDate, filter.ToDate, status, filter.Page, filter.PageSize), cancellationToken);
        return Page(result.Items.Select(MapInvoice).ToList(), result.TotalCount, result.Page, result.PageSize, "Billing");
    }

    public async Task<ReportPage<PaymentRegisterLine>> GetPaymentRegisterAsync(
        DateRangeFilter filter, string? status, Guid? patientId, CancellationToken cancellationToken = default)
    {
        var result = await _payments.SearchAsync(new SearchPaymentsRequest(
            null, patientId, null, filter.FromDate, filter.ToDate, status, filter.Page, filter.PageSize), cancellationToken);
        var items = result.Items.Select(p => new PaymentRegisterLine(
            p.Id, p.PaymentNumber, p.PaymentDate, p.InvoiceId, Guid.Empty, p.Amount, p.Status, p.Method)).ToList();
        return Page(items, result.TotalCount, result.Page, result.PageSize, "Billing");
    }

    public async Task<ReportPage<InvoiceRegisterLine>> GetOutstandingInvoicesAsync(
        DateRangeFilter filter, Guid? patientId, CancellationToken cancellationToken = default)
    {
        // Issued + PartiallyPaid — two searches merged is expensive; filter Issued then PartiallyPaid via status.
        // Prefer Issued first page then note; better: search without status and filter outstanding > 0 server-side via two calls.
        var issued = await _invoices.SearchAsync(new SearchInvoicesRequest(
            patientId, null, null, filter.FromDate, filter.ToDate, "Issued", filter.Page, filter.PageSize), cancellationToken);
        var partial = await _invoices.SearchAsync(new SearchInvoicesRequest(
            patientId, null, null, filter.FromDate, filter.ToDate, "PartiallyPaid", filter.Page, filter.PageSize), cancellationToken);
        var merged = issued.Items.Concat(partial.Items)
            .Where(i => i.OutstandingAmount > 0)
            .GroupBy(i => i.Id)
            .Select(g => g.First())
            .OrderByDescending(i => i.InvoiceDate)
            .Take(filter.PageSize)
            .Select(MapInvoice)
            .ToList();
        return Page(merged, issued.TotalCount + partial.TotalCount, filter.Page, filter.PageSize, "Billing");
    }

    public async Task<ReportPage<StockBalanceReportLine>> GetStockBalancesAsync(
        Guid? warehouseId, Guid? itemId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await _stock.SearchBalancesAsync(new SearchStockBalancesRequest(warehouseId, itemId, page, pageSize), cancellationToken);
        var items = result.Items.Select(b => new StockBalanceReportLine(
            b.WarehouseId, b.InventoryItemId, b.Quantity, b.InventoryValue, b.AverageUnitCost)).ToList();
        return Page(items, result.TotalCount, result.Page, result.PageSize, "Inventory");
    }

    public async Task<ReportPage<StockMovementReportLine>> GetStockMovementsAsync(
        DateRangeFilter filter, Guid? warehouseId, Guid? itemId, CancellationToken cancellationToken = default)
    {
        var result = await _stock.SearchMovementsAsync(new SearchStockMovementsRequest(
            warehouseId, itemId, filter.FromDate, filter.ToDate, null, filter.Page, filter.PageSize), cancellationToken);
        var items = result.Items.Select(m => new StockMovementReportLine(
            m.Id, m.CreatedAtUtc, m.WarehouseId, m.InventoryItemId, m.MovementType, m.Quantity,
            m.ReferenceType, m.ReferenceId, m.Reason, m.BalanceAfter)).ToList();
        return Page(items, result.TotalCount, result.Page, result.PageSize, "Inventory");
    }

    public async Task<ReportPage<InventoryValuationReportLine>> GetInventoryValuationAsync(
        Guid? warehouseId, Guid? itemId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryValuation.GetValuationAsync(
            new ValuationQueryRequest(warehouseId, itemId, null, page, pageSize), cancellationToken);
        var items = result.Items.Select(i => new InventoryValuationReportLine(
            i.WarehouseId, i.InventoryItemId, i.ItemCode, i.ItemName, i.Quantity, i.InventoryValue, i.AverageUnitCost)).ToList();
        return Page(items, result.TotalCount, result.Page, result.PageSize, "Inventory");
    }

    public async Task<ReportPage<InventoryCogsReportLine>> GetInventoryCogsAsync(
        DateRangeFilter filter, Guid? warehouseId, Guid? itemId, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryValuation.GetIssueCostsAsync(new IssueCostQueryRequest(
            filter.FromDate, filter.ToDate, warehouseId, itemId, filter.Page, filter.PageSize), cancellationToken);
        var items = result.Items.Select(i => new InventoryCogsReportLine(
            i.Id, i.WarehouseId, i.InventoryItemId, i.TransactionDate, i.Quantity, i.UnitCost, i.TotalCost,
            i.SourceType, i.SourceId, i.EventType)).ToList();
        return new ReportPage<InventoryCogsReportLine>(
            items, result.TotalCount, result.Page, result.PageSize, "Inventory",
            "Inventory issue cost foundation — not GL COGS. Inclusive FromDate/ToDate.");
    }

    public async Task<ReportPage<AssetRegisterReportLine>> GetAssetRegisterAsync(
        string? financialStatus, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await _assetsAccounting.SearchAsync(new SearchAssetAccountingRequest(
            null, null, null, financialStatus, null, page, pageSize), cancellationToken);
        var items = result.Items.Select(a => new AssetRegisterReportLine(
            a.AssetId, a.AssetNumber, a.AssetName, a.OperationalStatus, a.FinancialStatus,
            a.CapitalizedCost, a.AccumulatedDepreciation, a.NetBookValue)).ToList();
        return Page(items, result.TotalCount, result.Page, result.PageSize, "Assets");
    }

    public async Task<ReportPage<AssetDepreciationReportLine>> GetAssetDepreciationAsync(
        Guid? assetId, DateOnly? fromPeriod, DateOnly? toPeriod, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _assetsAccounting.SearchDepreciationAsync(new SearchDepreciationTransactionsRequest(
            assetId, fromPeriod, toPeriod, page, pageSize), cancellationToken);
        var items = result.Items.Select(d => new AssetDepreciationReportLine(
            d.Id, d.AssetId, d.PeriodKey, d.PeriodStartDate, d.DepreciationAmount,
            d.ClosingAccumulatedDepreciation, d.ClosingNetBookValue, d.Method)).ToList();
        return Page(items, result.TotalCount, result.Page, result.PageSize, "Assets");
    }

    public async Task<AssetValuationReportResult> GetAssetValuationAsync(
        int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await _assetsAccounting.GetValuationAsync(page, pageSize, cancellationToken);
        return new AssetValuationReportResult(
            result.TotalCapitalizedCost, result.TotalAccumulatedDepreciation, result.TotalNetBookValue,
            result.Items.Select(i => new AssetValuationReportLine(
                i.AssetId, i.AssetNumber, i.AssetName, i.CapitalizedCost, i.AccumulatedDepreciation,
                i.NetBookValue, i.FinancialStatus)).ToList(),
            result.TotalCount, result.Page, result.PageSize, "Assets");
    }

    public async Task<ReportPage<PurchaseOrderRegisterLine>> GetPurchaseOrderRegisterAsync(
        DateRangeFilter filter, string? status, Guid? supplierId, CancellationToken cancellationToken = default)
    {
        var result = await _purchaseOrders.SearchAsync(new SearchPurchaseOrdersRequest(
            supplierId, null, filter.FromDate, filter.ToDate, status, filter.Page, filter.PageSize), cancellationToken);
        var items = result.Items.Select(p => new PurchaseOrderRegisterLine(
            p.Id, p.PurchaseOrderNumber, p.SupplierId, p.OrderDate, p.Status, p.TotalAmount, p.CurrencyCode,
            "Operational procurement order value — not an AP financial obligation.")).ToList();
        return Page(items, result.TotalCount, result.Page, result.PageSize, "Procurement");
    }

    public async Task<ReportPage<ReceivingSummaryLine>> GetReceivingSummaryAsync(
        DateRangeFilter filter, Guid? warehouseId, Guid? purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var result = await _goodsReceipts.SearchAsync(new SearchGoodsReceiptsRequest(
            purchaseOrderId, warehouseId, null, filter.Page, filter.PageSize), cancellationToken);
        var items = result.Items
            .Where(r => (!filter.FromDate.HasValue || r.ReceiptDate >= filter.FromDate) &&
                        (!filter.ToDate.HasValue || r.ReceiptDate <= filter.ToDate))
            .Select(r => new ReceivingSummaryLine(
                r.Id, r.ReceiptNumber, r.PurchaseOrderId, r.WarehouseId, r.ReceiptDate, r.Status)).ToList();
        return Page(items, result.TotalCount, result.Page, result.PageSize, "Inventory");
    }

    public async Task<ManagementSummaryResult> GetManagementSummaryAsync(
        DateOnly? fromDate, DateOnly? toDate, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var metrics = new List<MetricValue>();
        var from = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);
        var to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var invoices = await _invoices.SearchAsync(new SearchInvoicesRequest(
            null, null, null, from, to, null, 1, 100), cancellationToken);
        var billingTotal = invoices.Items.Where(i => i.Status is not "Draft" and not "Voided").Sum(i => i.TotalAmount);
        metrics.Add(new MetricValue("BillingInvoiceTotal", true, billingTotal, "EGP",
            invoices.TotalCount > 100 ? "Partial page aggregate (first 100 matching invoices)." : null, "Billing"));

        var payments = await _payments.SearchAsync(new SearchPaymentsRequest(
            null, null, null, from, to, "Captured", 1, 100), cancellationToken);
        metrics.Add(new MetricValue("BillingCollections", true, payments.Items.Sum(p => p.Amount), "EGP",
            payments.TotalCount > 100 ? "Partial page aggregate (first 100 payments)." : null, "Billing"));

        var arAging = await _ar.GetAgingAsync(new AgingRequest(null, branchId, to), cancellationToken);
        metrics.Add(new MetricValue("ArOutstanding", true, arAging.TotalOutstanding, "EGP", null, "Finance.AR"));

        var apAging = await _ap.GetAgingAsync(new AgingRequest(null, branchId, to), cancellationToken);
        metrics.Add(new MetricValue("ApOutstanding", true, apAging.TotalOutstanding, "EGP", null, "Finance.AP"));

        var invVal = await _inventoryValuation.GetValuationAsync(new ValuationQueryRequest(null, null, null, 1, 1), cancellationToken);
        metrics.Add(new MetricValue("InventoryValue", true, invVal.TotalValue, "EGP", null, "Inventory"));

        var cogs = await _inventoryValuation.GetIssueCostsAsync(new IssueCostQueryRequest(from, to, null, null, 1, 1), cancellationToken);
        metrics.Add(new MetricValue("InventoryIssueCost", true, cogs.TotalIssueCost, "EGP",
            "Inventory issue-cost foundation — not GL COGS.", "Inventory"));

        var assetVal = await _assetsAccounting.GetValuationAsync(1, 1, cancellationToken);
        metrics.Add(new MetricValue("AssetGrossValue", true, assetVal.TotalCapitalizedCost, "EGP", null, "Assets"));
        metrics.Add(new MetricValue("AssetAccumulatedDepreciation", true, assetVal.TotalAccumulatedDepreciation, "EGP", null, "Assets"));
        metrics.Add(new MetricValue("AssetNetBookValue", true, assetVal.TotalNetBookValue, "EGP", null, "Assets"));

        var tb = await _tb.GetAsync(new GetTrialBalanceRequest(from, to, branchId), cancellationToken);
        metrics.Add(new MetricValue("PostedGlDebit", true, tb.GrandTotalDebit, "EGP", null, "Finance.GL"));
        metrics.Add(new MetricValue("PostedGlCredit", true, tb.GrandTotalCredit, "EGP", null, "Finance.GL"));

        metrics.Add(new MetricValue("NetProfit", false, null, null,
            "Revenue and expense GL integration / P&L mapping is not configured.", "Unavailable"));
        metrics.Add(new MetricValue("Ebitda", false, null, null, "Not supported by current accounting foundation.", "Unavailable"));
        metrics.Add(new MetricValue("CashFlow", false, null, null, "Not supported by current accounting foundation.", "Unavailable"));
        metrics.Add(new MetricValue("GrossMargin", false, null, null,
            "Do not derive margin by mixing Billing totals with Inventory issue cost.", "Unavailable"));

        return new ManagementSummaryResult(from, to, metrics,
            "Billing totals are not GL revenue. Inventory issue cost is not GL COGS. PO totals are not AP.");
    }

    private static InvoiceRegisterLine MapInvoice(InvoiceDto i) =>
        new(i.Id, i.InvoiceNumber ?? string.Empty, i.InvoiceDate, i.PatientId, i.Status,
            i.SubTotal, i.DiscountAmount, i.TaxAmount, i.TotalAmount, i.PaidAmount, i.OutstandingAmount, i.CurrencyCode);

    private static PartyStatementReport MapStatement(StatementResult s) =>
        new(s.PartyId, s.SubledgerType, s.PartyDisplayName, s.OpeningBalance, s.ClosingBalance, s.CurrencyCode,
            s.Items.Select(l => new StatementReportLine(
                l.Date, l.Source, l.Description, l.Debit, l.Credit, l.RunningBalance, l.Reference, l.TransactionId)).ToList(),
            s.TotalCount, s.Page, s.PageSize, "Finance", DueDateNote);

    private static AgingReportResult MapAging(AgingResult a) =>
        new(a.SubledgerType, a.AsOfDate,
            a.Buckets.Select(b => new AgingBucketReportLine(b.Bucket, b.Amount, b.ItemCount)).ToList(),
            a.Items.Select(i => new AgingOpenItemReportLine(
                i.PartyId, i.PartyDisplayName, i.SourceType, i.SourceId, i.TransactionDate, i.DueDate,
                i.OutstandingAmount, i.AgingBucket, i.DaysPastDue)).ToList(),
            a.TotalOutstanding, "Finance", DueDateNote);

    private static ReportPage<OutstandingPartyReportLine> OutstandingFromAging(AgingResult aging, int page, int pageSize)
    {
        var grouped = aging.Items
            .GroupBy(i => i.PartyId)
            .Select(g => new OutstandingPartyReportLine(
                g.Key,
                g.First().PartyDisplayName,
                g.Sum(x => x.OutstandingAmount),
                g.OrderByDescending(x => x.DaysPastDue).First().AgingBucket,
                g.Max(x => x.TransactionDate)))
            .OrderByDescending(x => x.Balance)
            .ToList();
        var pagingPage = page < 1 ? 1 : page;
        var size = pageSize is < 1 or > 100 ? 50 : pageSize;
        var slice = grouped.Skip((pagingPage - 1) * size).Take(size).ToList();
        return Page(slice, grouped.Count, pagingPage, size, "Finance");
    }

    private static ReportPage<T> Page<T>(IReadOnlyList<T> items, int total, int page, int pageSize, string source) =>
        new(items, total, page, pageSize, source);
}
