namespace ErpClink.Modules.Finance.Application.Accounts.Models;

public sealed record AccountDto(
    Guid Id,
    Guid? ParentAccountId,
    string Code,
    string Name,
    string? Description,
    string AccountType,
    bool IsPostable,
    bool IsActive,
    byte[] RowVersion);

public sealed record CreateAccountRequest(
    string Code,
    string Name,
    string? Description,
    string AccountType,
    bool IsPostable,
    Guid? ParentAccountId);

public sealed record UpdateAccountRequest(
    string Name,
    string? Description,
    string AccountType,
    bool IsPostable,
    Guid? ParentAccountId,
    byte[] RowVersion);

public sealed record SearchAccountsRequest(string? Query, string? AccountType, bool? IsActive, int Page = 1, int PageSize = 20);

public sealed record PagedAccountsResult(IReadOnlyList<AccountDto> Items, int TotalCount, int Page, int PageSize);
