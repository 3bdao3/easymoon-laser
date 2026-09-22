namespace ErpClink.Modules.Finance.Application.GeneralLedger.Models;

public sealed record GeneralLedgerLineDto(
    Guid JournalEntryId,
    string JournalNumber,
    DateOnly JournalDate,
    Guid BranchId,
    string JournalStatus,
    Guid LineId,
    Guid AccountId,
    string? LineDescription,
    decimal Debit,
    decimal Credit,
    DateTime? PostedAtUtc);

public sealed record SearchGeneralLedgerRequest(
    Guid? AccountId,
    Guid? BranchId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 50);

public sealed record PagedGeneralLedgerResult(
    IReadOnlyList<GeneralLedgerLineDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
