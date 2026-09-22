namespace ErpClink.Modules.Reports.Application.Models;

public sealed record ReportPage<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    string SourceModule,
    string DateFilterSemantics = "Inclusive FromDate and ToDate (DateOnly) where provided.");

public sealed record MetricValue(
    string Metric,
    bool Available,
    decimal? Value,
    string? CurrencyCode,
    string? Reason,
    string Source);

public sealed record DateRangeFilter(DateOnly? FromDate, DateOnly? ToDate, Guid? BranchId, int Page = 1, int PageSize = 50);

// —— Finance ——
public sealed record GlReportLine(
    DateOnly JournalDate,
    string JournalNumber,
    string? Description,
    decimal Debit,
    decimal Credit,
    Guid AccountId,
    string JournalStatus,
    Guid JournalEntryId,
    Guid LineId,
    DateTime? PostedAtUtc);

public sealed record TrialBalanceReportLine(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal NetDebit,
    decimal NetCredit);

public sealed record TrialBalanceReportResult(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<TrialBalanceReportLine> Lines,
    decimal GrandTotalDebit,
    decimal GrandTotalCredit,
    string SourceModule);

public sealed record JournalRegisterLine(
    Guid Id,
    string JournalNumber,
    DateOnly JournalDate,
    string? Description,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    string? CreatedBy,
    string? PostedBy,
    DateTime? PostedAtUtc);

// —— AR / AP ——
public sealed record PartyStatementReport(
    Guid PartyId,
    string SubledgerType,
    string? PartyDisplayName,
    decimal OpeningBalance,
    decimal ClosingBalance,
    string CurrencyCode,
    IReadOnlyList<StatementReportLine> Items,
    int TotalCount,
    int Page,
    int PageSize,
    string SourceModule,
    string AgingDueDateNote);

public sealed record StatementReportLine(
    DateOnly Date,
    string Source,
    string? Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string Reference,
    Guid TransactionId);

public sealed record AgingReportResult(
    string SubledgerType,
    DateOnly AsOfDate,
    IReadOnlyList<AgingBucketReportLine> Buckets,
    IReadOnlyList<AgingOpenItemReportLine> Items,
    decimal TotalOutstanding,
    string SourceModule,
    string DueDateSemantics);

public sealed record AgingBucketReportLine(string Bucket, decimal Amount, int ItemCount);

public sealed record AgingOpenItemReportLine(
    Guid PartyId,
    string? PartyDisplayName,
    string SourceType,
    string SourceId,
    DateOnly TransactionDate,
    DateOnly? DueDate,
    decimal OutstandingAmount,
    string AgingBucket,
    int DaysPastDue);

public sealed record OutstandingPartyReportLine(
    Guid PartyId,
    string? PartyDisplayName,
    decimal Balance,
    string? AgingCategory,
    DateOnly? LastTransactionDate);

// —— Billing ——
public sealed record InvoiceRegisterLine(
    Guid Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    Guid PatientId,
    string Status,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    string CurrencyCode);

public sealed record PaymentRegisterLine(
    Guid Id,
    string PaymentNumber,
    DateOnly PaymentDate,
    Guid InvoiceId,
    Guid PatientId,
    decimal Amount,
    string Status,
    string? MethodNote);

public sealed record BillingTotalsNote(
    string Label,
    string Semantics);

// —— Inventory ——
public sealed record StockBalanceReportLine(
    Guid WarehouseId,
    Guid InventoryItemId,
    decimal Quantity,
    decimal InventoryValue,
    decimal? AverageUnitCost);

public sealed record StockMovementReportLine(
    Guid Id,
    DateTime CreatedAtUtc,
    Guid WarehouseId,
    Guid InventoryItemId,
    string MovementType,
    decimal Quantity,
    string ReferenceType,
    Guid ReferenceId,
    string? Reason,
    decimal BalanceAfter);

public sealed record InventoryValuationReportLine(
    Guid WarehouseId,
    Guid InventoryItemId,
    string? ItemCode,
    string? ItemName,
    decimal Quantity,
    decimal InventoryValue,
    decimal? AverageUnitCost);

public sealed record InventoryCogsReportLine(
    Guid Id,
    Guid WarehouseId,
    Guid InventoryItemId,
    DateOnly TransactionDate,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    string SourceType,
    Guid SourceId,
    string EventType);

// —— Assets ——
public sealed record AssetRegisterReportLine(
    Guid AssetId,
    string AssetNumber,
    string AssetName,
    string OperationalStatus,
    string FinancialStatus,
    decimal? CapitalizedCost,
    decimal? AccumulatedDepreciation,
    decimal? NetBookValue);

public sealed record AssetDepreciationReportLine(
    Guid Id,
    Guid AssetId,
    string PeriodKey,
    DateOnly PeriodStartDate,
    decimal DepreciationAmount,
    decimal ClosingAccumulatedDepreciation,
    decimal ClosingNetBookValue,
    string Method);

public sealed record AssetValuationReportResult(
    decimal TotalCapitalizedCost,
    decimal TotalAccumulatedDepreciation,
    decimal TotalNetBookValue,
    IReadOnlyList<AssetValuationReportLine> Items,
    int TotalCount,
    int Page,
    int PageSize,
    string SourceModule);

public sealed record AssetValuationReportLine(
    Guid AssetId,
    string AssetNumber,
    string AssetName,
    decimal CapitalizedCost,
    decimal AccumulatedDepreciation,
    decimal NetBookValue,
    string FinancialStatus);

// —— Procurement ——
public sealed record PurchaseOrderRegisterLine(
    Guid Id,
    string PurchaseOrderNumber,
    Guid SupplierId,
    DateOnly OrderDate,
    string Status,
    decimal TotalAmount,
    string CurrencyCode,
    string SemanticsNote);

public sealed record ReceivingSummaryLine(
    Guid Id,
    string ReceiptNumber,
    Guid PurchaseOrderId,
    Guid WarehouseId,
    DateOnly ReceiptDate,
    string Status);

// —— Management ——
public sealed record ManagementSummaryResult(
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyList<MetricValue> Metrics,
    string Notes);
