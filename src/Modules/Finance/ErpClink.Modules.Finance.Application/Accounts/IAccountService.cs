using ErpClink.Modules.Finance.Application.Accounts.Models;

namespace ErpClink.Modules.Finance.Application.Accounts;

public interface IAccountService
{
    Task<AccountDto> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);
    Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedAccountsResult> SearchAsync(SearchAccountsRequest request, CancellationToken cancellationToken = default);
    Task<AccountDto> UpdateAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
