using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Finance.Application.TrialBalance;
using ErpClink.Modules.Finance.Application.TrialBalance.Models;
using ErpClink.Modules.Finance.Domain;
using ErpClink.Modules.Finance.Domain.Journals;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Finance.Infrastructure.TrialBalance;

public sealed class TrialBalanceService : ITrialBalanceService
{
    private readonly FinanceDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly IValidator<GetTrialBalanceRequest> _validator;

    public TrialBalanceService(
        FinanceDbContext db,
        IOrganizationContext org,
        IValidator<GetTrialBalanceRequest> validator)
    {
        _db = db;
        _org = org;
        _validator = validator;
    }

    public async Task<TrialBalanceResult> GetAsync(GetTrialBalanceRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var aggregates =
            from line in _db.JournalEntryLines.AsNoTracking()
            join journal in _db.JournalEntries.AsNoTracking() on line.JournalEntryId equals journal.Id
            join account in _db.Accounts.AsNoTracking() on line.AccountId equals account.Id
            where journal.OrganizationId == _org.OrganizationId
                  && (journal.Status == JournalStatus.Posted || journal.Status == JournalStatus.Reversed)
                  && account.IsPostable
                  && journal.JournalDate >= request.FromDate
                  && journal.JournalDate <= request.ToDate
                  && (request.BranchId == null || journal.BranchId == request.BranchId)
            group new { line, account } by new
            {
                account.Id,
                account.Code,
                account.Name,
                account.AccountType
            }
            into g
            select new
            {
                g.Key.Id,
                g.Key.Code,
                g.Key.Name,
                AccountType = g.Key.AccountType.ToString(),
                TotalDebit = g.Sum(x => x.line.Debit),
                TotalCredit = g.Sum(x => x.line.Credit)
            };

        var rows = await aggregates.OrderBy(a => a.Code).ToListAsync(cancellationToken);

        var lines = rows.Select(r =>
        {
            var debit = Money.Round(r.TotalDebit);
            var credit = Money.Round(r.TotalCredit);
            var net = debit - credit;
            return new TrialBalanceLineDto(
                r.Id,
                r.Code,
                r.Name,
                r.AccountType,
                debit,
                credit,
                net > 0 ? net : 0,
                net < 0 ? Money.Round(-net) : 0);
        }).ToList();

        var grandDebit = Money.Round(lines.Sum(l => l.TotalDebit));
        var grandCredit = Money.Round(lines.Sum(l => l.TotalCredit));

        return new TrialBalanceResult(request.FromDate, request.ToDate, lines, grandDebit, grandCredit);
    }
}
