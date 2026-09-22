using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Finance.Application.Accounts;
using ErpClink.Modules.Finance.Application.Accounts.Models;
using ErpClink.Modules.Finance.Application.Common;
using ErpClink.Modules.Finance.Domain.Accounts;
using ErpClink.Modules.Finance.Infrastructure.Common;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Finance.Infrastructure.Accounts;

public sealed class AccountService : IAccountService
{
    private readonly FinanceDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IFinanceDomainEventDispatcher _events;
    private readonly IValidator<CreateAccountRequest> _createValidator;
    private readonly IValidator<UpdateAccountRequest> _updateValidator;

    public AccountService(
        FinanceDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IFinanceDomainEventDispatcher events,
        IValidator<CreateAccountRequest> createValidator,
        IValidator<UpdateAccountRequest> updateValidator)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<AccountDto> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        if (!Enum.TryParse<AccountType>(request.AccountType, true, out var accountType))
            throw new AppException("finance.invalid_account_type", "Invalid account type.", 400);

        await EnsureParentValidAsync(null, request.ParentAccountId, cancellationToken);

        var account = Account.Create(
            _org.OrganizationId,
            request.Code,
            request.Name,
            request.Description,
            accountType,
            request.IsPostable,
            request.ParentAccountId,
            _user.UserId,
            _clock.UtcNow);

        try
        {
            _db.Accounts.Add(account);
            await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
            await DispatchAsync(account, cancellationToken);
            return Map(account);
        }
        catch (DbUpdateException ex) when (FinancePersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("finance.duplicate_account_code", "Account code already exists in this organization.", 409);
        }
    }

    public async Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await OrgAccounts().AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        return account is null ? null : Map(account);
    }

    public async Task<PagedAccountsResult> SearchAsync(SearchAccountsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgAccounts().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(a => a.Code.Contains(q) || a.Name.Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(request.AccountType) &&
            Enum.TryParse<AccountType>(request.AccountType, true, out var type))
            query = query.Where(a => a.AccountType == type);

        if (request.IsActive == true) query = query.Where(a => a.IsActive);
        else if (request.IsActive == false) query = query.Where(a => !a.IsActive);

        query = query.OrderBy(a => a.Code);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedAccountsResult(items.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<AccountDto> UpdateAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        if (!Enum.TryParse<AccountType>(request.AccountType, true, out var accountType))
            throw new AppException("finance.invalid_account_type", "Invalid account type.", 400);

        var account = await GetRequiredAsync(id, cancellationToken);
        FinancePersistenceHelper.ApplyRowVersion(_db, account, request.RowVersion, a => a.RowVersion);
        await EnsureParentValidAsync(id, request.ParentAccountId, cancellationToken);

        account.Update(request.Name, request.Description, accountType, request.IsPostable, request.ParentAccountId, _user.UserId, _clock.UtcNow);
        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await DispatchAsync(account, cancellationToken);
        return Map(account);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAsync(id, cancellationToken);
        account.Activate(_user.UserId, _clock.UtcNow);
        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await DispatchAsync(account, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAsync(id, cancellationToken);
        account.Deactivate(_user.UserId, _clock.UtcNow);
        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await DispatchAsync(account, cancellationToken);
    }

    private IQueryable<Account> OrgAccounts() =>
        _db.Accounts.Where(a => a.OrganizationId == _org.OrganizationId);

    private async Task<Account> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await OrgAccounts().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (account is null)
            throw new AppException("finance.account_not_found", "Account was not found.", 404);
        return account;
    }

    private async Task EnsureParentValidAsync(Guid? accountId, Guid? parentAccountId, CancellationToken cancellationToken)
    {
        if (parentAccountId is null) return;
        if (accountId == parentAccountId)
            throw new AppException("finance.invalid_parent", "An account cannot be its own parent.", 400);

        var parent = await OrgAccounts().AsNoTracking().SingleOrDefaultAsync(a => a.Id == parentAccountId, cancellationToken);
        if (parent is null)
            throw new AppException("finance.parent_not_found", "Parent account was not found.", 404);

        if (accountId is null) return;

        var visited = new HashSet<Guid> { accountId.Value };
        Guid? current = parentAccountId;
        while (current is not null)
        {
            if (!visited.Add(current.Value))
                throw new AppException("finance.account_cycle", "Account hierarchy would create a cycle.", 400);

            current = await OrgAccounts().AsNoTracking()
                .Where(a => a.Id == current.Value)
                .Select(a => a.ParentAccountId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    private async Task DispatchAsync(Account account, CancellationToken cancellationToken) =>
        await _events.DispatchAsync(account.DomainEvents, cancellationToken);

    private static AccountDto Map(Account account) => new(
        account.Id,
        account.ParentAccountId,
        account.Code,
        account.Name,
        account.Description,
        account.AccountType.ToString(),
        account.IsPostable,
        account.IsActive,
        account.RowVersion);
}
