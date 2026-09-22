using ErpClink.Modules.Finance.Application.GeneralLedger.Models;

namespace ErpClink.Modules.Finance.Application.GeneralLedger;

public interface IGeneralLedgerService
{
    Task<PagedGeneralLedgerResult> SearchAsync(SearchGeneralLedgerRequest request, CancellationToken cancellationToken = default);
}
