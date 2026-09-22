namespace ErpClink.Modules.Finance.Application.Journals.Models;

public sealed record JournalEntryLineDto(
    Guid Id,
    Guid AccountId,
    string? Description,
    decimal Debit,
    decimal Credit,
    int SortOrder);

public sealed record JournalEntryDto(
    Guid Id,
    string JournalNumber,
    DateOnly JournalDate,
    string? Description,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    Guid? FiscalPeriodId,
    Guid? ReversalOfJournalId,
    Guid? ReversedByJournalId,
    DateTime? PostedAtUtc,
    string? PostedBy,
    byte[] RowVersion,
    IReadOnlyList<JournalEntryLineDto> Lines);

public sealed record JournalLineInput(Guid AccountId, string? Description, decimal Debit, decimal Credit, int SortOrder);

public sealed record CreateJournalDraftRequest(DateOnly JournalDate, string? Description, IReadOnlyList<JournalLineInput>? Lines);

public sealed record UpdateJournalDraftRequest(DateOnly JournalDate, string? Description, byte[] RowVersion);

public sealed record ReplaceJournalLinesRequest(IReadOnlyList<JournalLineInput> Lines, byte[] RowVersion);

public sealed record PostJournalRequest(byte[] RowVersion);

public sealed record ReverseJournalRequest(byte[] RowVersion);

public sealed record SearchJournalsRequest(
    string? Query,
    string? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 20);

public sealed record PagedJournalsResult(IReadOnlyList<JournalEntryDto> Items, int TotalCount, int Page, int PageSize);

public sealed record JournalHistoryEntryDto(
    Guid Id,
    string EventType,
    DateTime OccurredAtUtc,
    string? OccurredBy,
    string? Notes);
