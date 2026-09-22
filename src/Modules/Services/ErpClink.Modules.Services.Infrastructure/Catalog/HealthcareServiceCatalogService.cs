using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Services.Application.Catalog;
using ErpClink.Modules.Services.Application.Catalog.Models;
using ErpClink.Modules.Services.Application.Common;
using ErpClink.Modules.Services.Domain.Catalog;
using ErpClink.Modules.Services.Infrastructure.Common;
using ErpClink.Modules.Services.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Services.Infrastructure.Catalog;

public sealed class HealthcareServiceCatalogService : IHealthcareServiceCatalogService
{
    private readonly ServicesDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IServicesDomainEventDispatcher _events;
    private readonly IValidator<CreateHealthcareServiceRequest> _createValidator;
    private readonly IValidator<UpdateHealthcareServiceRequest> _updateValidator;

    public HealthcareServiceCatalogService(
        ServicesDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IServicesDomainEventDispatcher events,
        IValidator<CreateHealthcareServiceRequest> createValidator,
        IValidator<UpdateHealthcareServiceRequest> updateValidator)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<HealthcareServiceDto> CreateAsync(CreateHealthcareServiceRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("services.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        if (request.CategoryId.HasValue)
            await EnsureCategoryExistsAsync(request.CategoryId.Value, cancellationToken);

        var service = HealthcareService.Create(
            _org.OrganizationId,
            request.ServiceCode,
            request.Name,
            request.Description,
            request.CategoryId,
            request.DefaultPrice,
            request.CurrencyCode,
            request.DurationMinutes,
            _user.UserId,
            _clock.UtcNow);

        try
        {
            _db.HealthcareServices.Add(service);
            await ServicesPersistenceHelper.SaveChangesAsync(_db, "services.concurrency_conflict", cancellationToken);
            await DispatchAsync(service, cancellationToken);
            return Map(service);
        }
        catch (DbUpdateException ex) when (ServicesPersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("services.duplicate_code", "Service code already exists.", 409);
        }
    }

    public async Task<HealthcareServiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await OrgServices()
            .AsNoTracking()
            .Include(s => s.Prices)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        return service is null ? null : Map(service);
    }

    public async Task<PagedHealthcareServicesResult> SearchAsync(SearchHealthcareServicesRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgServices().AsNoTracking();

        if (request.IsActive == true)
            query = query.Where(s => s.IsActive);
        else if (request.IsActive == false)
            query = query.Where(s => !s.IsActive);

        if (request.CategoryId.HasValue)
            query = query.Where(s => s.CategoryId == request.CategoryId);

        if (!string.IsNullOrWhiteSpace(request.ServiceCode))
            query = query.Where(s => s.ServiceCode.Contains(request.ServiceCode.Trim()));

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(s => s.Name.Contains(request.Name.Trim()));

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(s =>
                s.ServiceCode.Contains(q) ||
                s.Name.Contains(q) ||
                (s.Description != null && s.Description.Contains(q)));
        }

        query = query.OrderBy(s => s.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);

        return new PagedHealthcareServicesResult(
            items.Select(s => new HealthcareServiceListItemDto(
                s.Id, s.ServiceCode, s.Name, s.CategoryId, s.DefaultPrice, s.CurrencyCode, s.IsActive)).ToList(),
            paging.NormalizedPage,
            paging.NormalizedPageSize,
            total);
    }

    public async Task<HealthcareServiceDto> UpdateAsync(Guid id, UpdateHealthcareServiceRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("services.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        if (request.CategoryId.HasValue)
            await EnsureCategoryExistsAsync(request.CategoryId.Value, cancellationToken);

        var service = await GetRequiredForUpdateAsync(id, cancellationToken);
        ServicesPersistenceHelper.ApplyRowVersion(_db, service, request.RowVersion, s => s.RowVersion);

        try
        {
            if (request.DefaultPrice.HasValue && request.DefaultPrice.Value != service.DefaultPrice)
            {
                await _db.ServicePrices
                    .Where(p => p.ServiceId == id && p.EffectiveToUtc == null)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(p => p.EffectiveToUtc, _clock.UtcNow),
                        cancellationToken);
            }

            service.Update(
                request.Name,
                request.Description,
                request.CategoryId,
                request.DefaultPrice,
                request.CurrencyCode,
                request.DurationMinutes,
                _user.UserId,
                _clock.UtcNow);
            AttachNewPriceRows(service);
            await ServicesPersistenceHelper.SaveChangesAsync(_db, "services.concurrency_conflict", cancellationToken);
            await DispatchAsync(service, cancellationToken);
            return Map(service);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("services.invalid_operation", ex.Message, 400);
        }
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await GetRequiredAsync(id, cancellationToken);
        service.Activate(_user.UserId, _clock.UtcNow);
        await ServicesPersistenceHelper.SaveChangesAsync(_db, "services.concurrency_conflict", cancellationToken);
        await DispatchAsync(service, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await GetRequiredAsync(id, cancellationToken);
        service.Deactivate(_user.UserId, _clock.UtcNow);
        await ServicesPersistenceHelper.SaveChangesAsync(_db, "services.concurrency_conflict", cancellationToken);
        await DispatchAsync(service, cancellationToken);
    }

    private IQueryable<HealthcareService> OrgServices() =>
        _db.HealthcareServices.Where(s => s.OrganizationId == _org.OrganizationId);

    private async Task<HealthcareService> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await OrgServices()
            .Include(s => s.Prices)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
            throw new AppException("services.not_found", "Service was not found.", 404);
        return service;
    }

    private async Task<HealthcareService> GetRequiredForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await OrgServices().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
            throw new AppException("services.not_found", "Service was not found.", 404);
        return service;
    }

    private async Task EnsureCategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var exists = await _db.ServiceCategories.AsNoTracking()
            .AnyAsync(c => c.OrganizationId == _org.OrganizationId && c.Id == categoryId, cancellationToken);
        if (!exists)
            throw new AppException("services.category_not_found", "Service category was not found.", 404);
    }

    private async Task DispatchAsync(HealthcareService service, CancellationToken cancellationToken)
    {
        var events = service.DomainEvents.ToList();
        service.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private void AttachNewPriceRows(HealthcareService service)
    {
        foreach (var price in service.Prices)
        {
            if (_db.Entry(price).State == EntityState.Detached)
                _db.ServicePrices.Add(price);
        }
    }

    private static HealthcareServiceDto Map(HealthcareService s)
    {
        var current = s.Prices.FirstOrDefault(p => p.EffectiveToUtc is null);
        return new HealthcareServiceDto(
            s.Id,
            s.OrganizationId,
            s.ServiceCode,
            s.Name,
            s.Description,
            s.CategoryId,
            s.DefaultPrice,
            s.CurrencyCode,
            s.DurationMinutes,
            s.IsActive,
            current?.Id,
            s.CreatedAtUtc,
            s.CreatedBy,
            s.UpdatedAtUtc,
            s.UpdatedBy,
            s.RowVersion);
    }
}
