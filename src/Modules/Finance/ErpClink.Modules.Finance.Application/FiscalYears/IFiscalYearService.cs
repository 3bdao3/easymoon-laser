using ErpClink.Modules.Finance.Application.FiscalYears.Models;

namespace ErpClink.Modules.Finance.Application.FiscalYears;

public interface IFiscalYearService
{
    Task<FiscalYearDto> CreateAsync(CreateFiscalYearRequest request, CancellationToken cancellationToken = default);
    Task<FiscalYearDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedFiscalYearsResult> SearchAsync(SearchFiscalYearsRequest request, CancellationToken cancellationToken = default);
    Task<FiscalYearDto> UpdateAsync(Guid id, UpdateFiscalYearRequest request, CancellationToken cancellationToken = default);
    Task CloseAsync(Guid id, CancellationToken cancellationToken = default);
}
