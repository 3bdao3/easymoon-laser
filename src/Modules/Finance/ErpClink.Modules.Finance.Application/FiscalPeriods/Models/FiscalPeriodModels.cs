namespace ErpClink.Modules.Finance.Application.FiscalPeriods.Models;

public sealed record FiscalPeriodDto(
    Guid Id,
    Guid FiscalYearId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    byte[] RowVersion);

public sealed record CreateFiscalPeriodRequest(Guid FiscalYearId, string Name, DateOnly StartDate, DateOnly EndDate);

public sealed record UpdateFiscalPeriodRequest(string Name, DateOnly StartDate, DateOnly EndDate, byte[] RowVersion);

public sealed record SearchFiscalPeriodsRequest(Guid? FiscalYearId, string? Status, int Page = 1, int PageSize = 20);

public sealed record PagedFiscalPeriodsResult(IReadOnlyList<FiscalPeriodDto> Items, int TotalCount, int Page, int PageSize);
