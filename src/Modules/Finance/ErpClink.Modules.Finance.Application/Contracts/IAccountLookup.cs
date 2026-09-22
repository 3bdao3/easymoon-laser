namespace ErpClink.Modules.Finance.Application.Contracts;

public interface IAccountLookup
{
    Task<bool> ExistsActivePostableInOrgAsync(Guid accountId, Guid organizationId, CancellationToken cancellationToken = default);
}
