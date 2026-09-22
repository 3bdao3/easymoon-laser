using ErpClink.Modules.Finance.Application.FiscalPeriods.Models;

namespace ErpClink.Modules.Finance.Application.FiscalPeriods;

public interface IFiscalPeriodService
{
    Task<FiscalPeriodDto> CreateAsync(CreateFiscalPeriodRequest request, CancellationToken cancellationToken = default);
    Task<FiscalPeriodDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedFiscalPeriodsResult> SearchAsync(SearchFiscalPeriodsRequest request, CancellationToken cancellationToken = default);
    Task<FiscalPeriodDto> UpdateAsync(Guid id, UpdateFiscalPeriodRequest request, CancellationToken cancellationToken = default);
    Task CloseAsync(Guid id, CancellationToken cancellationToken = default);
}
