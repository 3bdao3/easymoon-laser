using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Finance.Application.Subledgers;
using ErpClink.Modules.Finance.Application.Subledgers.Models;
using ErpClink.Modules.Finance.Domain;
using ErpClink.Modules.Finance.Domain.Subledgers;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Finance.Infrastructure.Subledgers;

public sealed class ArSubledgerQueryService : SubledgerQueryServiceBase, IArSubledgerQueryService
{
    public ArSubledgerQueryService(
        FinanceDbContext db,
        IOrganizationContext org,
        IArCustomerLookup customers,
        IBusinessClock clock)
        : base(db, org, SubledgerType.Ar, clock)
    {
        _customers = customers;
    }

    private readonly IArCustomerLookup _customers;

    public Task<PartyBalanceDto> GetCustomerBalanceAsync(Guid partyId, CancellationToken cancellationToken = default) =>
        GetBalanceAsync(partyId, cancellationToken);

    public Task<StatementResult> GetCustomerStatementAsync(StatementRequest request, CancellationToken cancellationToken = default) =>
        GetStatementAsync(request, cancellationToken);

    protected override async Task<string?> ResolveDisplayNameAsync(Guid partyId, CancellationToken cancellationToken)
    {
        var c = await _customers.GetAsync(partyId, cancellationToken);
        return c?.DisplayName;
    }
}

public sealed class ApSubledgerQueryService : SubledgerQueryServiceBase, IApSubledgerQueryService
{
    public ApSubledgerQueryService(
        FinanceDbContext db,
        IOrganizationContext org,
        IApSupplierLookup suppliers,
        IBusinessClock clock)
        : base(db, org, SubledgerType.Ap, clock)
    {
        _suppliers = suppliers;
    }

    private readonly IApSupplierLookup _suppliers;

    public Task<PartyBalanceDto> GetSupplierBalanceAsync(Guid partyId, CancellationToken cancellationToken = default) =>
        GetBalanceAsync(partyId, cancellationToken);

    public Task<StatementResult> GetSupplierStatementAsync(StatementRequest request, CancellationToken cancellationToken = default) =>
        GetStatementAsync(request, cancellationToken);

    protected override async Task<string?> ResolveDisplayNameAsync(Guid partyId, CancellationToken cancellationToken)
    {
        var s = await _suppliers.GetAsync(partyId, cancellationToken);
        return s is null ? null : $"{s.SupplierCode} — {s.DisplayName}";
    }
}

public abstract class SubledgerQueryServiceBase
{
    private readonly FinanceDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly SubledgerType _type;
    private readonly IBusinessClock _clock;

    protected SubledgerQueryServiceBase(
        FinanceDbContext db,
        IOrganizationContext org,
        SubledgerType type,
        IBusinessClock clock)
    {
        _db = db;
        _org = org;
        _type = type;
        _clock = clock;
    }

    protected abstract Task<string?> ResolveDisplayNameAsync(Guid partyId, CancellationToken cancellationToken);

    public async Task<PagedSubledgerTransactionsResult> SearchAsync(
        SearchSubledgerTransactionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = BaseQuery();
        if (request.PartyId is { } partyId)
            query = query.Where(x => x.PartyId == partyId);
        if (request.BranchId is { } branchId)
            query = query.Where(x => x.BranchId == branchId);
        if (request.FromDate is { } from)
            query = query.Where(x => x.TransactionDate >= from);
        if (request.ToDate is { } to)
            query = query.Where(x => x.TransactionDate <= to);
        if (!string.IsNullOrWhiteSpace(request.SourceModule))
            query = query.Where(x => x.SourceModule == request.SourceModule);
        if (!string.IsNullOrWhiteSpace(request.SourceType))
            query = query.Where(x => x.SourceType == request.SourceType);
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<SubledgerTransactionStatus>(request.Status, true, out var status))
            query = query.Where(x => x.Status == status);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.PostedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedSubledgerTransactionsResult(items.Select(Map).ToList(), total, page, pageSize);
    }

    protected async Task<PartyBalanceDto> GetBalanceAsync(Guid partyId, CancellationToken cancellationToken)
    {
        if (partyId == Guid.Empty)
            throw new AppException("finance.subledger.party_required", "PartyId is required.", 400);

        var row = await _db.SubledgerPartyBalances.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == _org.OrganizationId
                     && x.SubledgerType == _type
                     && x.PartyId == partyId,
                cancellationToken);

        var computed = await BaseQuery()
            .Where(x => x.PartyId == partyId)
            .Select(x => x.Direction == SubledgerDirection.Debit
                ? (_type == SubledgerType.Ar ? x.Amount : -x.Amount)
                : (_type == SubledgerType.Ar ? -x.Amount : x.Amount))
            .SumAsync(cancellationToken);

        var balance = Money.Round(row?.Balance ?? computed);
        var currency = row?.CurrencyCode ?? "EGP";
        var name = await ResolveDisplayNameAsync(partyId, cancellationToken);
        return new PartyBalanceDto(partyId, _type.ToString(), balance, currency, name);
    }

    protected async Task<StatementResult> GetStatementAsync(StatementRequest request, CancellationToken cancellationToken)
    {
        if (request.PartyId == Guid.Empty)
            throw new AppException("finance.subledger.party_required", "PartyId is required.", 400);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 500);

        var query = BaseQuery().Where(x => x.PartyId == request.PartyId);
        if (request.BranchId is { } branchId)
            query = query.Where(x => x.BranchId == branchId);

        // Opening is activity before FromDate; when FromDate is omitted, opening is zero.
        decimal opening = 0m;
        if (request.FromDate is { } from)
        {
            var openingQuery = query.Where(x => x.TransactionDate < from);
            opening = Money.Round(await SumSignedAsync(openingQuery, cancellationToken));
        }

        var periodQuery = query;
        if (request.FromDate is { } fromDate)
            periodQuery = periodQuery.Where(x => x.TransactionDate >= fromDate);
        if (request.ToDate is { } toDate)
            periodQuery = periodQuery.Where(x => x.TransactionDate <= toDate);

        var total = await periodQuery.CountAsync(cancellationToken);
        var rows = await periodQuery
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.PostedAtUtc)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Running balance for the page starts after opening + prior pages in the filtered period.
        var priorInPeriod = await periodQuery
            .OrderBy(x => x.TransactionDate).ThenBy(x => x.PostedAtUtc).ThenBy(x => x.Id)
            .Take((page - 1) * pageSize)
            .ToListAsync(cancellationToken);
        var running = Money.Round(opening + priorInPeriod.Sum(SignedImpact));

        var lines = new List<StatementLineDto>(rows.Count);
        foreach (var row in rows)
        {
            running = Money.Round(running + SignedImpact(row));
            lines.Add(new StatementLineDto(
                row.TransactionDate,
                $"{row.SourceModule}/{row.SourceType}",
                row.Description,
                row.DebitAmount,
                row.CreditAmount,
                running,
                row.SourceId,
                row.Id));
        }

        var closing = Money.Round(opening + await SumSignedAsync(periodQuery, cancellationToken));
        var name = await ResolveDisplayNameAsync(request.PartyId, cancellationToken);
        var currency = rows.FirstOrDefault()?.CurrencyCode
                       ?? (await _db.SubledgerPartyBalances.AsNoTracking()
                           .Where(x => x.OrganizationId == _org.OrganizationId && x.SubledgerType == _type && x.PartyId == request.PartyId)
                           .Select(x => x.CurrencyCode)
                           .FirstOrDefaultAsync(cancellationToken))
                       ?? "EGP";

        return new StatementResult(
            request.PartyId,
            _type.ToString(),
            name,
            opening,
            closing,
            currency,
            lines,
            total,
            page,
            pageSize);
    }

    public async Task<AgingResult> GetAgingAsync(AgingRequest request, CancellationToken cancellationToken = default)
    {
        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(_clock.UtcNow);

        var query = BaseQuery();
        if (request.PartyId is { } partyId)
            query = query.Where(x => x.PartyId == partyId);
        if (request.BranchId is { } branchId)
            query = query.Where(x => x.BranchId == branchId);
        query = query.Where(x => x.TransactionDate <= asOf);

        var rows = await query
            .Select(x => new
            {
                x.PartyId,
                x.PartyDisplayNameSnapshot,
                x.SourceModule,
                x.SourceType,
                x.SourceId,
                x.TransactionDate,
                x.DueDate,
                Impact = x.Direction == SubledgerDirection.Debit
                    ? (_type == SubledgerType.Ar ? x.Amount : -x.Amount)
                    : (_type == SubledgerType.Ar ? -x.Amount : x.Amount)
            })
            .ToListAsync(cancellationToken);

        var openItems = rows
            .GroupBy(x => new { x.PartyId, x.SourceModule, x.SourceType, x.SourceId })
            .Select(g =>
            {
                var outstanding = Money.Round(g.Sum(i => i.Impact));
                var anchor = g.MinBy(i => i.TransactionDate)!;
                var due = anchor.DueDate; // null when source has no due date — do not invent
                var ageDate = due ?? anchor.TransactionDate;
                var days = asOf.DayNumber - ageDate.DayNumber;
                if (days < 0) days = 0;
                return new AgingOpenItemDto(
                    g.Key.PartyId,
                    g.Max(i => i.PartyDisplayNameSnapshot),
                    g.Key.SourceModule,
                    g.Key.SourceType,
                    g.Key.SourceId,
                    anchor.TransactionDate,
                    due,
                    outstanding,
                    Bucket(days),
                    days);
            })
            .Where(i => i.OutstandingAmount > 0)
            .OrderByDescending(i => i.DaysPastDue)
            .ToList();

        var buckets = new[] { "Current", "1-30", "31-60", "61-90", "91+" }
            .Select(b => new AgingBucketDto(
                b,
                Money.Round(openItems.Where(i => i.AgingBucket == b).Sum(i => i.OutstandingAmount)),
                openItems.Count(i => i.AgingBucket == b)))
            .ToList();

        return new AgingResult(
            _type.ToString(),
            asOf,
            buckets,
            openItems,
            Money.Round(openItems.Sum(i => i.OutstandingAmount)));
    }

    private IQueryable<SubledgerTransaction> BaseQuery() =>
        _db.SubledgerTransactions.AsNoTracking()
            .Where(x => x.OrganizationId == _org.OrganizationId && x.SubledgerType == _type);

    private async Task<decimal> SumSignedAsync(IQueryable<SubledgerTransaction> query, CancellationToken cancellationToken)
    {
        var list = await query.AsNoTracking().ToListAsync(cancellationToken);
        return Money.Round(list.Sum(SignedImpact));
    }

    private decimal SignedImpact(SubledgerTransaction x) =>
        x.Direction == SubledgerDirection.Debit
            ? (_type == SubledgerType.Ar ? x.Amount : -x.Amount)
            : (_type == SubledgerType.Ar ? -x.Amount : x.Amount);

    private static string Bucket(int daysPastDue) => daysPastDue switch
    {
        <= 0 => "Current",
        <= 30 => "1-30",
        <= 60 => "31-60",
        <= 90 => "61-90",
        _ => "91+"
    };

    private static SubledgerTransactionDto Map(SubledgerTransaction x) =>
        new(
            x.Id,
            x.SubledgerType.ToString(),
            x.PartyId,
            x.PartyDisplayNameSnapshot,
            x.SourceModule,
            x.SourceType,
            x.SourceId,
            x.EventType,
            x.TransactionDate,
            x.DueDate,
            x.Amount,
            x.DebitAmount,
            x.CreditAmount,
            x.CurrencyCode,
            x.Direction.ToString(),
            x.Description,
            x.Status.ToString(),
            x.CorrelationId,
            x.ReversalOfTransactionId,
            x.ReversedByTransactionId,
            x.PostedAtUtc);
}
