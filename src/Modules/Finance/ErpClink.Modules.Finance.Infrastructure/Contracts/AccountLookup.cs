using ErpClink.Modules.Finance.Application.Contracts;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Finance.Infrastructure.Contracts;

public sealed class AccountLookup : IAccountLookup
{
    private readonly FinanceDbContext _db;

    public AccountLookup(FinanceDbContext db) => _db = db;

    public Task<bool> ExistsActivePostableInOrgAsync(Guid accountId, Guid organizationId, CancellationToken cancellationToken = default) =>
        _db.Accounts.AsNoTracking()
            .AnyAsync(a => a.Id == accountId && a.OrganizationId == organizationId && a.IsActive && a.IsPostable, cancellationToken);
}
