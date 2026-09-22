namespace ErpClink.Modules.Finance.Application.FiscalYears.Models;

public sealed record FiscalYearDto(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    byte[] RowVersion);

public sealed record CreateFiscalYearRequest(string Name, DateOnly StartDate, DateOnly EndDate);

public sealed record UpdateFiscalYearRequest(string Name, DateOnly StartDate, DateOnly EndDate, byte[] RowVersion);

public sealed record SearchFiscalYearsRequest(string? Query, string? Status, int Page = 1, int PageSize = 20);

public sealed record PagedFiscalYearsResult(IReadOnlyList<FiscalYearDto> Items, int TotalCount, int Page, int PageSize);
