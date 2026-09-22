using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Inventory.Application.Warehouses;
using ErpClink.Modules.Inventory.Application.Warehouses.Models;
using ErpClink.Modules.Inventory.Domain.Warehouses;
using ErpClink.Modules.Inventory.Infrastructure.Common;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Inventory.Infrastructure.Warehouses;

public sealed class WarehouseService : IWarehouseService
{
    private readonly InventoryDbContext _db;
    private readonly IWarehouseNumberGenerator _numbers;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IValidator<CreateWarehouseRequest> _createValidator;
    private readonly IValidator<UpdateWarehouseRequest> _updateValidator;

    public WarehouseService(
        InventoryDbContext db,
        IWarehouseNumberGenerator numbers,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IValidator<CreateWarehouseRequest> createValidator,
        IValidator<UpdateWarehouseRequest> updateValidator)
    {
        _db = db;
        _numbers = numbers;
        _org = org;
        _user = user;
        _clock = clock;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("inventory.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var code = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
        var warehouse = Warehouse.Create(
            _org.OrganizationId, _org.BranchId, code, request.Name, request.Description, _user.UserId, _clock.UtcNow);

        try
        {
            _db.Warehouses.Add(warehouse);
            await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
            return Map(warehouse);
        }
        catch (DbUpdateException ex) when (InventoryPersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("inventory.duplicate_warehouse_code", "Warehouse code already exists.", 409);
        }
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await OrgWarehouses().AsNoTracking().SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
        return warehouse is null ? null : Map(warehouse);
    }

    public async Task<PagedWarehousesResult> SearchAsync(SearchWarehousesRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgWarehouses().AsNoTracking();
        if (request.IsActive == true) query = query.Where(w => w.IsActive);
        else if (request.IsActive == false) query = query.Where(w => !w.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(w => w.Name.Contains(q) || w.WarehouseCode.Contains(q));
        }

        query = query.OrderBy(w => w.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedWarehousesResult(items.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("inventory.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var warehouse = await GetRequiredAsync(id, cancellationToken);
        InventoryPersistenceHelper.ApplyRowVersion(_db, warehouse, request.RowVersion, w => w.RowVersion);
        warehouse.Update(request.Name, request.Description, _user.UserId, _clock.UtcNow);
        await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
        return Map(warehouse);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await GetRequiredAsync(id, cancellationToken);
        warehouse.Activate(_user.UserId, _clock.UtcNow);
        await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await GetRequiredAsync(id, cancellationToken);
        warehouse.Deactivate(_user.UserId, _clock.UtcNow);
        await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
    }

    private IQueryable<Warehouse> OrgWarehouses() => _db.Warehouses.Where(w => w.OrganizationId == _org.OrganizationId);

    private async Task<Warehouse> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await OrgWarehouses().SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warehouse is null)
            throw new AppException("inventory.warehouse_not_found", "Warehouse was not found.", 404);
        return warehouse;
    }

    private static WarehouseDto Map(Warehouse w) =>
        new(w.Id, w.OrganizationId, w.BranchId, w.WarehouseCode, w.Name, w.Description, w.IsActive, w.CreatedAtUtc, w.RowVersion);
}
