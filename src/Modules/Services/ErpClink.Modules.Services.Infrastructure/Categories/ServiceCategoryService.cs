using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Services.Application.Categories;
using ErpClink.Modules.Services.Application.Categories.Models;
using ErpClink.Modules.Services.Application.Common;
using ErpClink.Modules.Services.Domain.Categories;
using ErpClink.Modules.Services.Infrastructure.Common;
using ErpClink.Modules.Services.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Services.Infrastructure.Categories;

public sealed class ServiceCategoryService : IServiceCategoryService
{
    private readonly ServicesDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IServicesDomainEventDispatcher _events;
    private readonly IValidator<CreateServiceCategoryRequest> _createValidator;
    private readonly IValidator<UpdateServiceCategoryRequest> _updateValidator;

    public ServiceCategoryService(
        ServicesDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IServicesDomainEventDispatcher events,
        IValidator<CreateServiceCategoryRequest> createValidator,
        IValidator<UpdateServiceCategoryRequest> updateValidator)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ServiceCategoryDto> CreateAsync(CreateServiceCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("service_categories.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var category = ServiceCategory.Create(
            _org.OrganizationId,
            request.Code,
            request.Name,
            request.Description,
            _user.UserId,
            _clock.UtcNow);

        try
        {
            _db.ServiceCategories.Add(category);
            await ServicesPersistenceHelper.SaveChangesAsync(_db, "service_categories.concurrency_conflict", cancellationToken);
            await DispatchAsync(category, cancellationToken);
            return Map(category);
        }
        catch (DbUpdateException ex) when (ServicesPersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("service_categories.duplicate_code", "Category code already exists.", 409);
        }
    }

    public async Task<ServiceCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await OrgCategories().AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        return category is null ? null : Map(category);
    }

    public async Task<PagedServiceCategoriesResult> SearchAsync(SearchServiceCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgCategories().AsNoTracking();

        if (request.IsActive == true)
            query = query.Where(c => c.IsActive);
        else if (request.IsActive == false)
            query = query.Where(c => !c.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(c => c.Code.Contains(q) || c.Name.Contains(q));
        }

        query = query.OrderBy(c => c.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedServiceCategoriesResult(
            items.Select(Map).ToList(),
            paging.NormalizedPage,
            paging.NormalizedPageSize,
            total);
    }

    public async Task<ServiceCategoryDto> UpdateAsync(Guid id, UpdateServiceCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("service_categories.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var category = await GetRequiredAsync(id, cancellationToken);
        ServicesPersistenceHelper.ApplyRowVersion(_db, category, request.RowVersion, c => c.RowVersion);
        category.Update(request.Name, request.Description, _user.UserId, _clock.UtcNow);
        await ServicesPersistenceHelper.SaveChangesAsync(_db, "service_categories.concurrency_conflict", cancellationToken);
        await DispatchAsync(category, cancellationToken);
        return Map(category);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetRequiredAsync(id, cancellationToken);
        category.Activate(_user.UserId, _clock.UtcNow);
        await ServicesPersistenceHelper.SaveChangesAsync(_db, "service_categories.concurrency_conflict", cancellationToken);
        await DispatchAsync(category, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetRequiredAsync(id, cancellationToken);
        category.Deactivate(_user.UserId, _clock.UtcNow);
        await ServicesPersistenceHelper.SaveChangesAsync(_db, "service_categories.concurrency_conflict", cancellationToken);
        await DispatchAsync(category, cancellationToken);
    }

    private IQueryable<ServiceCategory> OrgCategories() =>
        _db.ServiceCategories.Where(c => c.OrganizationId == _org.OrganizationId);

    private async Task<ServiceCategory> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await OrgCategories().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
            throw new AppException("service_categories.not_found", "Service category was not found.", 404);
        return category;
    }

    private async Task DispatchAsync(ServiceCategory category, CancellationToken cancellationToken)
    {
        var events = category.DomainEvents.ToList();
        category.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static ServiceCategoryDto Map(ServiceCategory c) =>
        new(c.Id, c.OrganizationId, c.Code, c.Name, c.Description, c.IsActive,
            c.CreatedAtUtc, c.CreatedBy, c.UpdatedAtUtc, c.UpdatedBy, c.RowVersion);
}
