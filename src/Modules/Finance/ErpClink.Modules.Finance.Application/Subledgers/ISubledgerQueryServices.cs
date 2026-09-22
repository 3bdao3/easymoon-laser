using ErpClink.Modules.Finance.Application.Subledgers.Models;

namespace ErpClink.Modules.Finance.Application.Subledgers;

public interface IArSubledgerQueryService
{
    Task<PartyBalanceDto> GetCustomerBalanceAsync(Guid partyId, CancellationToken cancellationToken = default);
    Task<StatementResult> GetCustomerStatementAsync(StatementRequest request, CancellationToken cancellationToken = default);
    Task<PagedSubledgerTransactionsResult> SearchAsync(SearchSubledgerTransactionsRequest request, CancellationToken cancellationToken = default);
    Task<AgingResult> GetAgingAsync(AgingRequest request, CancellationToken cancellationToken = default);
}

public interface IApSubledgerQueryService
{
    Task<PartyBalanceDto> GetSupplierBalanceAsync(Guid partyId, CancellationToken cancellationToken = default);
    Task<StatementResult> GetSupplierStatementAsync(StatementRequest request, CancellationToken cancellationToken = default);
    Task<PagedSubledgerTransactionsResult> SearchAsync(SearchSubledgerTransactionsRequest request, CancellationToken cancellationToken = default);
    Task<AgingResult> GetAgingAsync(AgingRequest request, CancellationToken cancellationToken = default);
}
