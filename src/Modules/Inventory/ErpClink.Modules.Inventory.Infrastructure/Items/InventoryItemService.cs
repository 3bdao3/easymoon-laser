using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Inventory.Application.Items;
using ErpClink.Modules.Inventory.Application.Items.Models;
using ErpClink.Modules.Inventory.Domain.Items;
using ErpClink.Modules.Inventory.Infrastructure.Common;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Inventory.Infrastructure.Items;

public sealed class InventoryItemService : IInventoryItemService
{
    private readonly InventoryDbContext _db;
    private readonly IInventoryItemNumberGenerator _numbers;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IValidator<CreateInventoryItemRequest> _createValidator;
    private readonly IValidator<UpdateInventoryItemRequest> _updateValidator;

    public InventoryItemService(
        InventoryDbContext db,
        IInventoryItemNumberGenerator numbers,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IValidator<CreateInventoryItemRequest> createValidator,
        IValidator<UpdateInventoryItemRequest> updateValidator)
    {
        _db = db;
        _numbers = numbers;
        _org = org;
        _user = user;
        _clock = clock;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("inventory.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        if (request.CategoryId.HasValue)
            await EnsureCategoryAsync(request.CategoryId.Value, cancellationToken);

        var code = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
        var item = InventoryItem.Create(
            _org.OrganizationId,
            code,
            request.Name,
            request.Description,
            request.CategoryId,
            request.UnitOfMeasure,
            request.Barcode,
            request.TrackExpiry,
            request.MinStockQuantity,
            _user.UserId,
            _clock.UtcNow);

        try
        {
            _db.InventoryItems.Add(item);
            await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
            return Map(item);
        }
        catch (DbUpdateException ex) when (InventoryPersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("inventory.duplicate_item_code", "Item code already exists.", 409);
        }
    }

    public async Task<InventoryItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await OrgItems().AsNoTracking().SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<PagedInventoryItemsResult> SearchAsync(SearchInventoryItemsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgItems().AsNoTracking();
        if (request.CategoryId.HasValue) query = query.Where(i => i.CategoryId == request.CategoryId);
        if (request.IsActive == true) query = query.Where(i => i.IsActive);
        else if (request.IsActive == false) query = query.Where(i => !i.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(i => i.Name.Contains(q) || i.ItemCode.Contains(q) || (i.Barcode != null && i.Barcode.Contains(q)));
        }

        query = query.OrderBy(i => i.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedInventoryItemsResult(items.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<InventoryItemDto> UpdateAsync(Guid id, UpdateInventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("inventory.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        if (request.CategoryId.HasValue)
            await EnsureCategoryAsync(request.CategoryId.Value, cancellationToken);

        var item = await GetRequiredAsync(id, cancellationToken);
        InventoryPersistenceHelper.ApplyRowVersion(_db, item, request.RowVersion, i => i.RowVersion);
        item.Update(
            request.Name,
            request.Description,
            request.CategoryId,
            request.UnitOfMeasure,
            request.Barcode,
            request.TrackExpiry,
            request.MinStockQuantity,
            _user.UserId,
            _clock.UtcNow);
        await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
        return Map(item);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetRequiredAsync(id, cancellationToken);
        item.Activate(_user.UserId, _clock.UtcNow);
        await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetRequiredAsync(id, cancellationToken);
        item.Deactivate(_user.UserId, _clock.UtcNow);
        await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
    }

    private async Task EnsureCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var exists = await _db.InventoryCategories.AsNoTracking()
            .AnyAsync(c => c.Id == categoryId && c.OrganizationId == _org.OrganizationId, cancellationToken);
        if (!exists)
            throw new AppException("inventory.category_not_found", "Inventory category was not found.", 404);
    }

    private IQueryable<InventoryItem> OrgItems() => _db.InventoryItems.Where(i => i.OrganizationId == _org.OrganizationId);

    private async Task<InventoryItem> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await OrgItems().SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
            throw new AppException("inventory.item_not_found", "Inventory item was not found.", 404);
        return item;
    }

    private static InventoryItemDto Map(InventoryItem i) =>
        new(i.Id, i.ItemCode, i.Name, i.Description, i.CategoryId, i.UnitOfMeasure, i.Barcode, i.TrackExpiry, i.MinStockQuantity, i.IsActive, i.RowVersion);
}
