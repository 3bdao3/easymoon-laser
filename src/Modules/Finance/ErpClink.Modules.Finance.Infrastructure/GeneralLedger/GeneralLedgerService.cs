using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Finance.Application.GeneralLedger;
using ErpClink.Modules.Finance.Application.GeneralLedger.Models;
using ErpClink.Modules.Finance.Domain.Journals;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Finance.Infrastructure.GeneralLedger;

public sealed class GeneralLedgerService : IGeneralLedgerService
{
    private readonly FinanceDbContext _db;
    private readonly IOrganizationContext _org;

    public GeneralLedgerService(FinanceDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<PagedGeneralLedgerResult> SearchAsync(SearchGeneralLedgerRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);

        var query =
            from line in _db.JournalEntryLines.AsNoTracking()
            join journal in _db.JournalEntries.AsNoTracking() on line.JournalEntryId equals journal.Id
            where journal.OrganizationId == _org.OrganizationId
                  && (journal.Status == JournalStatus.Posted || journal.Status == JournalStatus.Reversed)
            select new { line, journal };

        if (request.AccountId is not null)
            query = query.Where(x => x.line.AccountId == request.AccountId);

        if (request.BranchId is not null)
            query = query.Where(x => x.journal.BranchId == request.BranchId);

        if (request.FromDate is not null)
            query = query.Where(x => x.journal.JournalDate >= request.FromDate);

        if (request.ToDate is not null)
            query = query.Where(x => x.journal.JournalDate <= request.ToDate);

        query = query.OrderBy(x => x.journal.JournalDate)
            .ThenBy(x => x.journal.JournalNumber)
            .ThenBy(x => x.line.SortOrder);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .Select(x => new GeneralLedgerLineDto(
                x.journal.Id,
                x.journal.JournalNumber,
                x.journal.JournalDate,
                x.journal.BranchId,
                x.journal.Status.ToString(),
                x.line.Id,
                x.line.AccountId,
                x.line.Description,
                x.line.Debit,
                x.line.Credit,
                x.journal.PostedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedGeneralLedgerResult(items, total, paging.NormalizedPage, paging.NormalizedPageSize);
    }
}
