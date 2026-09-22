using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Finance.Application.Common;
using ErpClink.Modules.Finance.Application.FiscalPeriods;
using ErpClink.Modules.Finance.Application.FiscalPeriods.Models;
using ErpClink.Modules.Finance.Domain.FiscalPeriods;
using ErpClink.Modules.Finance.Infrastructure.Common;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Finance.Infrastructure.FiscalPeriods;

public sealed class FiscalPeriodService : IFiscalPeriodService
{
    private readonly FinanceDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IFinanceDomainEventDispatcher _events;
    private readonly IValidator<CreateFiscalPeriodRequest> _createValidator;
    private readonly IValidator<UpdateFiscalPeriodRequest> _updateValidator;

    public FiscalPeriodService(
        FinanceDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IFinanceDomainEventDispatcher events,
        IValidator<CreateFiscalPeriodRequest> createValidator,
        IValidator<UpdateFiscalPeriodRequest> updateValidator)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<FiscalPeriodDto> CreateAsync(CreateFiscalPeriodRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var year = await _db.FiscalYears.SingleOrDefaultAsync(
            y => y.Id == request.FiscalYearId && y.OrganizationId == _org.OrganizationId, cancellationToken);
        if (year is null)
            throw new AppException("finance.fiscal_year_not_found", "Fiscal year was not found.", 404);

        if (request.StartDate < year.StartDate || request.EndDate > year.EndDate)
            throw new AppException("finance.period_outside_year", "Fiscal period must fall within its fiscal year.", 400);

        await EnsureNoOverlapAsync(null, request.StartDate, request.EndDate, cancellationToken);

        var period = FiscalPeriod.Create(
            _org.OrganizationId,
            request.FiscalYearId,
            request.Name,
            request.StartDate,
            request.EndDate,
            _user.UserId,
            _clock.UtcNow);

        _db.FiscalPeriods.Add(period);
        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await _events.DispatchAsync(period.DomainEvents, cancellationToken);
        return Map(period);
    }

    public async Task<FiscalPeriodDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var period = await OrgPeriods().AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        return period is null ? null : Map(period);
    }

    public async Task<PagedFiscalPeriodsResult> SearchAsync(SearchFiscalPeriodsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgPeriods().AsNoTracking();

        if (request.FiscalYearId is not null)
            query = query.Where(p => p.FiscalYearId == request.FiscalYearId);

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<FiscalPeriodStatus>(request.Status, true, out var status))
            query = query.Where(p => p.Status == status);

        query = query.OrderBy(p => p.StartDate);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedFiscalPeriodsResult(items.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<FiscalPeriodDto> UpdateAsync(Guid id, UpdateFiscalPeriodRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var period = await GetRequiredAsync(id, cancellationToken);
        FinancePersistenceHelper.ApplyRowVersion(_db, period, request.RowVersion, p => p.RowVersion);

        var year = await _db.FiscalYears.AsNoTracking()
            .SingleAsync(y => y.Id == period.FiscalYearId && y.OrganizationId == _org.OrganizationId, cancellationToken);
        if (request.StartDate < year.StartDate || request.EndDate > year.EndDate)
            throw new AppException("finance.period_outside_year", "Fiscal period must fall within its fiscal year.", 400);

        await EnsureNoOverlapAsync(id, request.StartDate, request.EndDate, cancellationToken);
        period.Update(request.Name, request.StartDate, request.EndDate, _user.UserId, _clock.UtcNow);
        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await _events.DispatchAsync(period.DomainEvents, cancellationToken);
        return Map(period);
    }

    public async Task CloseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var period = await GetRequiredAsync(id, cancellationToken);
        period.Close(_user.UserId, _clock.UtcNow);
        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await _events.DispatchAsync(period.DomainEvents, cancellationToken);
    }

    private IQueryable<FiscalPeriod> OrgPeriods() =>
        _db.FiscalPeriods.Where(p => p.OrganizationId == _org.OrganizationId);

    private async Task<FiscalPeriod> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var period = await OrgPeriods().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (period is null)
            throw new AppException("finance.fiscal_period_not_found", "Fiscal period was not found.", 404);
        return period;
    }

    private async Task EnsureNoOverlapAsync(Guid? excludeId, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        var overlap = await OrgPeriods().AsNoTracking()
            .AnyAsync(p => p.Id != excludeId && p.StartDate <= end && p.EndDate >= start, cancellationToken);
        if (overlap)
            throw new AppException("finance.fiscal_period_overlap", "Fiscal period dates overlap an existing period.", 400);
    }

    private static FiscalPeriodDto Map(FiscalPeriod period) => new(
        period.Id,
        period.FiscalYearId,
        period.Name,
        period.StartDate,
        period.EndDate,
        period.Status.ToString(),
        period.RowVersion);
}
