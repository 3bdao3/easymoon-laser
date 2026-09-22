using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Finance.Application.Common;
using ErpClink.Modules.Finance.Application.Journals;
using ErpClink.Modules.Finance.Application.Journals.Models;
using ErpClink.Modules.Finance.Domain.Accounts;
using ErpClink.Modules.Finance.Domain.FiscalPeriods;
using ErpClink.Modules.Finance.Domain.FiscalYears;
using ErpClink.Modules.Finance.Domain.Journals;
using ErpClink.Modules.Finance.Infrastructure.Common;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Finance.Infrastructure.Journals;

public sealed class JournalService : IJournalService
{
    private readonly FinanceDbContext _db;
    private readonly IJournalNumberGenerator _numbers;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IFinanceDomainEventDispatcher _events;
    private readonly IValidator<CreateJournalDraftRequest> _createValidator;
    private readonly IValidator<UpdateJournalDraftRequest> _updateValidator;
    private readonly IValidator<ReplaceJournalLinesRequest> _replaceValidator;
    private readonly IValidator<PostJournalRequest> _postValidator;
    private readonly IValidator<ReverseJournalRequest> _reverseValidator;

    public JournalService(
        FinanceDbContext db,
        IJournalNumberGenerator numbers,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IFinanceDomainEventDispatcher events,
        IValidator<CreateJournalDraftRequest> createValidator,
        IValidator<UpdateJournalDraftRequest> updateValidator,
        IValidator<ReplaceJournalLinesRequest> replaceValidator,
        IValidator<PostJournalRequest> postValidator,
        IValidator<ReverseJournalRequest> reverseValidator)
    {
        _db = db;
        _numbers = numbers;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _replaceValidator = replaceValidator;
        _postValidator = postValidator;
        _reverseValidator = reverseValidator;
    }

    public async Task<JournalEntryDto> CreateDraftAsync(CreateJournalDraftRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var number = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
        var entry = JournalEntry.CreateDraft(
            _org.OrganizationId,
            _org.BranchId,
            number,
            request.JournalDate,
            request.Description,
            _user.UserId,
            _clock.UtcNow);

        if (request.Lines is { Count: > 0 })
        {
            await ValidateLineAccountsAsync(request.Lines.Select(l => l.AccountId), cancellationToken);
            entry.ReplaceLines(
                request.Lines.Select(l => (l.AccountId, l.Description, l.Debit, l.Credit, l.SortOrder)).ToList(),
                _user.UserId,
                _clock.UtcNow);
        }

        _db.JournalEntries.Add(entry);
        _db.JournalEntryHistory.Add(JournalEntryHistory.Record(
            entry.Id, JournalHistoryEventType.Created, _clock.UtcNow, _user.UserId, null));

        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await DispatchAsync(entry, cancellationToken);
        return Map(entry);
    }

    public async Task<JournalEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await OrgJournals().AsNoTracking().SingleOrDefaultAsync(j => j.Id == id, cancellationToken);
        return entry is null ? null : Map(entry);
    }

    public async Task<PagedJournalsResult> SearchAsync(SearchJournalsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgJournals().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(j => j.JournalNumber.Contains(q) || (j.Description != null && j.Description.Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<JournalStatus>(request.Status, true, out var status))
            query = query.Where(j => j.Status == status);

        if (request.FromDate is not null)
            query = query.Where(j => j.JournalDate >= request.FromDate);
        if (request.ToDate is not null)
            query = query.Where(j => j.JournalDate <= request.ToDate);

        query = query.OrderByDescending(j => j.JournalDate).ThenByDescending(j => j.JournalNumber);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedJournalsResult(items.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<JournalEntryDto> UpdateDraftAsync(Guid id, UpdateJournalDraftRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var entry = await GetRequiredWithLinesAsync(id, cancellationToken);
        FinancePersistenceHelper.ApplyRowVersion(_db, entry, request.RowVersion, j => j.RowVersion);
        entry.UpdateDraft(request.JournalDate, request.Description, _user.UserId, _clock.UtcNow);
        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await DispatchAsync(entry, cancellationToken);
        return Map(entry);
    }

    public async Task<JournalEntryDto> ReplaceLinesAsync(Guid id, ReplaceJournalLinesRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _replaceValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var entry = await GetRequiredWithLinesAsync(id, cancellationToken);
        FinancePersistenceHelper.ApplyRowVersion(_db, entry, request.RowVersion, j => j.RowVersion);
        await ValidateLineAccountsAsync(request.Lines.Select(l => l.AccountId), cancellationToken);
        entry.ReplaceLines(
            request.Lines.Select(l => (l.AccountId, l.Description, l.Debit, l.Credit, l.SortOrder)).ToList(),
            _user.UserId,
            _clock.UtcNow);
        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await DispatchAsync(entry, cancellationToken);
        return Map(entry);
    }

    public async Task<JournalEntryDto> PostAsync(Guid id, PostJournalRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _postValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var entry = await GetRequiredWithLinesAsync(id, cancellationToken);
        FinancePersistenceHelper.ApplyRowVersion(_db, entry, request.RowVersion, j => j.RowVersion);

        await ValidateJournalAccountsForPostingAsync(entry, cancellationToken);
        var (period, year) = await ResolveOpenPeriodAsync(entry.JournalDate, cancellationToken);
        year.EnsureOpenForPosting();
        period.EnsureOpenForPosting();

        try
        {
            entry.EnsureBalanced();
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("finance.journal_unbalanced", ex.Message, 400);
        }

        entry.Post(period.Id, _user.UserId, _clock.UtcNow);
        _db.JournalEntryHistory.Add(JournalEntryHistory.Record(
            entry.Id, JournalHistoryEventType.Posted, _clock.UtcNow, _user.UserId, null));

        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await DispatchAsync(entry, cancellationToken);
        return Map(entry);
    }

    public async Task<JournalEntryDto> ReverseAsync(Guid id, ReverseJournalRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _reverseValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var original = await OrgJournals().SingleOrDefaultAsync(j => j.Id == id, cancellationToken);
        if (original is null)
            throw new AppException("finance.journal_not_found", "Journal entry was not found.", 404);

        FinancePersistenceHelper.ApplyRowVersion(_db, original, request.RowVersion, j => j.RowVersion);

        await _db.Entry(original).Collection(j => j.Lines).LoadAsync(cancellationToken);

        var (period, year) = await ResolveOpenPeriodAsync(original.JournalDate, cancellationToken);
        year.EnsureOpenForPosting();
        period.EnsureOpenForPosting();

        var number = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
        JournalEntry reversal;
        try
        {
            reversal = JournalEntry.CreatePostedReversal(
                _org.OrganizationId,
                _org.BranchId,
                number,
                original.JournalDate,
                $"Reversal of {original.JournalNumber}",
                period.Id,
                original,
                _user.UserId,
                _clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("finance.journal_reverse_failed", ex.Message, 400);
        }

        original.MarkReversed(reversal.Id, _user.UserId, _clock.UtcNow);

        _db.JournalEntries.Add(reversal);
        _db.JournalEntryHistory.Add(JournalEntryHistory.Record(
            reversal.Id, JournalHistoryEventType.Created, _clock.UtcNow, _user.UserId, $"Reversal of {original.JournalNumber}"));
        _db.JournalEntryHistory.Add(JournalEntryHistory.Record(
            reversal.Id, JournalHistoryEventType.Posted, _clock.UtcNow, _user.UserId, null));
        _db.JournalEntryHistory.Add(JournalEntryHistory.Record(
            original.Id, JournalHistoryEventType.Reversed, _clock.UtcNow, _user.UserId, reversal.JournalNumber));

        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await DispatchAsync(original, cancellationToken);
        await DispatchAsync(reversal, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Map(reversal);
    }

    public async Task<IReadOnlyList<JournalHistoryEntryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await OrgJournals().AsNoTracking().AnyAsync(j => j.Id == id, cancellationToken);
        if (!exists)
            throw new AppException("finance.journal_not_found", "Journal entry was not found.", 404);

        return await _db.JournalEntryHistory.AsNoTracking()
            .Where(h => h.JournalEntryId == id)
            .OrderBy(h => h.OccurredAtUtc)
            .Select(h => new JournalHistoryEntryDto(
                h.Id,
                h.EventType.ToString(),
                h.OccurredAtUtc,
                h.OccurredBy,
                h.Notes))
            .ToListAsync(cancellationToken);
    }

    private async Task ValidateLineAccountsAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken)
    {
        var ids = accountIds.Distinct().ToList();
        var accounts = await _db.Accounts.AsNoTracking()
            .Where(a => a.OrganizationId == _org.OrganizationId && ids.Contains(a.Id))
            .ToListAsync(cancellationToken);

        if (accounts.Count != ids.Count)
            throw new AppException("finance.account_not_found", "One or more accounts were not found.", 404);

        foreach (var account in accounts)
        {
            try
            {
                account.EnsureUsableForJournalLine();
            }
            catch (InvalidOperationException ex)
            {
                throw new AppException("finance.account_not_postable", ex.Message, 400);
            }
        }
    }

    private async Task ValidateJournalAccountsForPostingAsync(JournalEntry entry, CancellationToken cancellationToken) =>
        await ValidateLineAccountsAsync(entry.Lines.Select(l => l.AccountId), cancellationToken);

    private async Task<(FiscalPeriod Period, FiscalYear Year)> ResolveOpenPeriodAsync(DateOnly journalDate, CancellationToken cancellationToken)
    {
        var hasAnyPeriod = await _db.FiscalPeriods.AsNoTracking()
            .AnyAsync(p => p.OrganizationId == _org.OrganizationId, cancellationToken);
        if (!hasAnyPeriod)
            throw new AppException(
                "finance.no_fiscal_periods",
                "Posting requires at least one open fiscal period that contains the journal date. Configure fiscal years and periods first.",
                400);

        var period = await _db.FiscalPeriods.AsNoTracking()
            .Where(p => p.OrganizationId == _org.OrganizationId
                        && p.Status == FiscalPeriodStatus.Open
                        && p.StartDate <= journalDate
                        && p.EndDate >= journalDate)
            .SingleOrDefaultAsync(cancellationToken);

        if (period is null)
            throw new AppException(
                "finance.no_open_period_for_date",
                "No open fiscal period contains the journal date. Open a period for that date before posting.",
                400);

        var year = await _db.FiscalYears.AsNoTracking()
            .SingleAsync(y => y.Id == period.FiscalYearId && y.OrganizationId == _org.OrganizationId, cancellationToken);

        return (period, year);
    }

    private IQueryable<JournalEntry> OrgJournals() =>
        _db.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.OrganizationId == _org.OrganizationId);

    private async Task<JournalEntry> GetRequiredWithLinesAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await OrgJournals().SingleOrDefaultAsync(j => j.Id == id, cancellationToken);
        if (entry is null)
            throw new AppException("finance.journal_not_found", "Journal entry was not found.", 404);
        return entry;
    }

    private async Task DispatchAsync(JournalEntry entry, CancellationToken cancellationToken) =>
        await _events.DispatchAsync(entry.DomainEvents, cancellationToken);

    private static JournalEntryDto Map(JournalEntry entry) => new(
        entry.Id,
        entry.JournalNumber,
        entry.JournalDate,
        entry.Description,
        entry.Status.ToString(),
        entry.TotalDebit,
        entry.TotalCredit,
        entry.FiscalPeriodId,
        entry.ReversalOfJournalId,
        entry.ReversedByJournalId,
        entry.PostedAtUtc,
        entry.PostedBy,
        entry.RowVersion,
        entry.Lines.OrderBy(l => l.SortOrder).Select(l => new JournalEntryLineDto(
            l.Id, l.AccountId, l.Description, l.Debit, l.Credit, l.SortOrder)).ToList());
}
