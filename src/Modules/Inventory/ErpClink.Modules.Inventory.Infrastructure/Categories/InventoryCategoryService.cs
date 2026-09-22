using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Inventory.Application.Categories;
using ErpClink.Modules.Inventory.Application.Categories.Models;
using ErpClink.Modules.Inventory.Domain.Categories;
using ErpClink.Modules.Inventory.Infrastructure.Common;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Inventory.Infrastructure.Categories;

public sealed class InventoryCategoryService : IInventoryCategoryService
{
    private readonly InventoryDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IValidator<CreateInventoryCategoryRequest> _createValidator;
    private readonly IValidator<UpdateInventoryCategoryRequest> _updateValidator;

    public InventoryCategoryService(
        InventoryDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IValidator<CreateInventoryCategoryRequest> createValidator,
        IValidator<UpdateInventoryCategoryRequest> updateValidator)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<InventoryCategoryDto> CreateAsync(CreateInventoryCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("inventory.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var category = InventoryCategory.Create(_org.OrganizationId, request.Code, request.Name, _user.UserId, _clock.UtcNow);
        try
        {
            _db.InventoryCategories.Add(category);
            await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
            return Map(category);
        }
        catch (DbUpdateException ex) when (InventoryPersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("inventory.duplicate_category_code", "Category code already exists.", 409);
        }
    }

    public async Task<InventoryCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await OrgCategories().AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        return category is null ? null : Map(category);
    }

    public async Task<PagedInventoryCategoriesResult> SearchAsync(SearchInventoryCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgCategories().AsNoTracking();
        if (request.IsActive == true) query = query.Where(c => c.IsActive);
        else if (request.IsActive == false) query = query.Where(c => !c.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(c => c.Code.Contains(q) || c.Name.Contains(q));
        }

        query = query.OrderBy(c => c.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedInventoryCategoriesResult(items.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<InventoryCategoryDto> UpdateAsync(Guid id, UpdateInventoryCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("inventory.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var category = await GetRequiredAsync(id, cancellationToken);
        InventoryPersistenceHelper.ApplyRowVersion(_db, category, request.RowVersion, c => c.RowVersion);
        category.Update(request.Name, _user.UserId, _clock.UtcNow);
        await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
        return Map(category);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetRequiredAsync(id, cancellationToken);
        category.Activate(_user.UserId, _clock.UtcNow);
        await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetRequiredAsync(id, cancellationToken);
        category.Deactivate(_user.UserId, _clock.UtcNow);
        await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
    }

    private IQueryable<InventoryCategory> OrgCategories() =>
        _db.InventoryCategories.Where(c => c.OrganizationId == _org.OrganizationId);

    private async Task<InventoryCategory> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await OrgCategories().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
            throw new AppException("inventory.category_not_found", "Inventory category was not found.", 404);
        return category;
    }

    private static InventoryCategoryDto Map(InventoryCategory c) =>
        new(c.Id, c.Code, c.Name, c.IsActive, c.RowVersion);
}
