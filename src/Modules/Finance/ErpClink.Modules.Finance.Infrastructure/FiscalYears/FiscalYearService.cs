using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Finance.Application.Common;
using ErpClink.Modules.Finance.Application.FiscalYears;
using ErpClink.Modules.Finance.Application.FiscalYears.Models;
using ErpClink.Modules.Finance.Domain.FiscalYears;
using ErpClink.Modules.Finance.Infrastructure.Common;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Finance.Infrastructure.FiscalYears;

public sealed class FiscalYearService : IFiscalYearService
{
    private readonly FinanceDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IFinanceDomainEventDispatcher _events;
    private readonly IValidator<CreateFiscalYearRequest> _createValidator;
    private readonly IValidator<UpdateFiscalYearRequest> _updateValidator;

    public FiscalYearService(
        FinanceDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IFinanceDomainEventDispatcher events,
        IValidator<CreateFiscalYearRequest> createValidator,
        IValidator<UpdateFiscalYearRequest> updateValidator)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<FiscalYearDto> CreateAsync(CreateFiscalYearRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        await EnsureNoOverlapAsync(null, request.StartDate, request.EndDate, cancellationToken);

        var year = FiscalYear.Create(_org.OrganizationId, request.Name, request.StartDate, request.EndDate, _user.UserId, _clock.UtcNow);
        _db.FiscalYears.Add(year);
        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await _events.DispatchAsync(year.DomainEvents, cancellationToken);
        return Map(year);
    }

    public async Task<FiscalYearDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var year = await OrgYears().AsNoTracking().SingleOrDefaultAsync(y => y.Id == id, cancellationToken);
        return year is null ? null : Map(year);
    }

    public async Task<PagedFiscalYearsResult> SearchAsync(SearchFiscalYearsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgYears().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(y => y.Name.Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<FiscalYearStatus>(request.Status, true, out var status))
            query = query.Where(y => y.Status == status);

        query = query.OrderByDescending(y => y.StartDate);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedFiscalYearsResult(items.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<FiscalYearDto> UpdateAsync(Guid id, UpdateFiscalYearRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("finance.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var year = await GetRequiredAsync(id, cancellationToken);
        FinancePersistenceHelper.ApplyRowVersion(_db, year, request.RowVersion, y => y.RowVersion);
        await EnsureNoOverlapAsync(id, request.StartDate, request.EndDate, cancellationToken);
        year.Update(request.Name, request.StartDate, request.EndDate, _user.UserId, _clock.UtcNow);
        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await _events.DispatchAsync(year.DomainEvents, cancellationToken);
        return Map(year);
    }

    public async Task CloseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var year = await GetRequiredAsync(id, cancellationToken);
        year.Close(_user.UserId, _clock.UtcNow);
        await FinancePersistenceHelper.SaveChangesAsync(_db, "finance.concurrency_conflict", cancellationToken);
        await _events.DispatchAsync(year.DomainEvents, cancellationToken);
    }

    private IQueryable<FiscalYear> OrgYears() =>
        _db.FiscalYears.Where(y => y.OrganizationId == _org.OrganizationId);

    private async Task<FiscalYear> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var year = await OrgYears().SingleOrDefaultAsync(y => y.Id == id, cancellationToken);
        if (year is null)
            throw new AppException("finance.fiscal_year_not_found", "Fiscal year was not found.", 404);
        return year;
    }

    private async Task EnsureNoOverlapAsync(Guid? excludeId, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        var overlap = await OrgYears().AsNoTracking()
            .AnyAsync(y => y.Id != excludeId && y.StartDate <= end && y.EndDate >= start, cancellationToken);
        if (overlap)
            throw new AppException("finance.fiscal_year_overlap", "Fiscal year dates overlap an existing year.", 400);
    }

    private static FiscalYearDto Map(FiscalYear year) => new(
        year.Id,
        year.Name,
        year.StartDate,
        year.EndDate,
        year.Status.ToString(),
        year.RowVersion);
}
