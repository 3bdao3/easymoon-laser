using ErpClink.Modules.Finance.Application.TrialBalance.Models;

namespace ErpClink.Modules.Finance.Application.TrialBalance;

public interface ITrialBalanceService
{
    Task<TrialBalanceResult> GetAsync(GetTrialBalanceRequest request, CancellationToken cancellationToken = default);
}
