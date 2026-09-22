namespace ErpClink.Modules.Finance.Application.Subledgers.Models;

public sealed record PartyBalanceDto(
    Guid PartyId,
    string SubledgerType,
    decimal Balance,
    string CurrencyCode,
    string? PartyDisplayName);

public sealed record SubledgerTransactionDto(
    Guid Id,
    string SubledgerType,
    Guid PartyId,
    string? PartyDisplayName,
    string SourceModule,
    string SourceType,
    string SourceId,
    string EventType,
    DateOnly TransactionDate,
    DateOnly? DueDate,
    decimal Amount,
    decimal Debit,
    decimal Credit,
    string CurrencyCode,
    string Direction,
    string? Description,
    string Status,
    Guid CorrelationId,
    Guid? ReversalOfTransactionId,
    Guid? ReversedByTransactionId,
    DateTime PostedAtUtc);

public sealed record SearchSubledgerTransactionsRequest(
    Guid? PartyId,
    Guid? BranchId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? SourceModule,
    string? SourceType,
    string? Status,
    int Page = 1,
    int PageSize = 20);

public sealed record PagedSubledgerTransactionsResult(
    IReadOnlyList<SubledgerTransactionDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record StatementRequest(
    Guid PartyId,
    Guid? BranchId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 100);

public sealed record StatementLineDto(
    DateOnly Date,
    string Source,
    string? Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string Reference,
    Guid TransactionId);

public sealed record StatementResult(
    Guid PartyId,
    string SubledgerType,
    string? PartyDisplayName,
    decimal OpeningBalance,
    decimal ClosingBalance,
    string CurrencyCode,
    IReadOnlyList<StatementLineDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AgingRequest(Guid? PartyId, Guid? BranchId, DateOnly? AsOfDate);

public sealed record AgingBucketDto(string Bucket, decimal Amount, int ItemCount);

public sealed record AgingOpenItemDto(
    Guid PartyId,
    string? PartyDisplayName,
    string SourceModule,
    string SourceType,
    string SourceId,
    DateOnly TransactionDate,
    DateOnly? DueDate,
    decimal OutstandingAmount,
    string AgingBucket,
    int DaysPastDue);

public sealed record AgingResult(
    string SubledgerType,
    DateOnly AsOfDate,
    IReadOnlyList<AgingBucketDto> Buckets,
    IReadOnlyList<AgingOpenItemDto> Items,
    decimal TotalOutstanding);
