namespace ErpClink.Modules.Finance.Application.TrialBalance.Models;

public sealed record TrialBalanceLineDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal NetDebit,
    decimal NetCredit);

public sealed record TrialBalanceResult(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<TrialBalanceLineDto> Lines,
    decimal GrandTotalDebit,
    decimal GrandTotalCredit);

public sealed record GetTrialBalanceRequest(DateOnly FromDate, DateOnly ToDate, Guid? BranchId);
