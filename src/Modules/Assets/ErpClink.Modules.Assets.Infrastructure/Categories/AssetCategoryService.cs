using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Assets.Application.Categories;
using ErpClink.Modules.Assets.Application.Categories.Models;
using ErpClink.Modules.Assets.Domain.Categories;
using ErpClink.Modules.Assets.Infrastructure.Common;
using ErpClink.Modules.Assets.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Assets.Infrastructure.Categories;

public sealed class AssetCategoryService : IAssetCategoryService
{
    private readonly AssetsDbContext _db;
    private readonly IAssetCategoryCodeGenerator _codes;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IValidator<CreateAssetCategoryRequest> _createValidator;
    private readonly IValidator<UpdateAssetCategoryRequest> _updateValidator;

    public AssetCategoryService(
        AssetsDbContext db,
        IAssetCategoryCodeGenerator codes,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IValidator<CreateAssetCategoryRequest> createValidator,
        IValidator<UpdateAssetCategoryRequest> updateValidator)
    {
        _db = db;
        _codes = codes;
        _org = org;
        _user = user;
        _clock = clock;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<AssetCategoryDto> CreateAsync(CreateAssetCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var code = await _codes.GenerateAsync(_org.OrganizationId, cancellationToken);
        var category = AssetCategory.Create(_org.OrganizationId, code, request.Name, request.Description, _user.UserId, _clock.UtcNow);
        try
        {
            _db.AssetCategories.Add(category);
            await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
            return Map(category);
        }
        catch (DbUpdateException ex) when (AssetsPersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("assets.duplicate_category_code", "Asset category code already exists.", 409);
        }
    }

    public async Task<AssetCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await OrgCategories().AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        return category is null ? null : Map(category);
    }

    public async Task<PagedAssetCategoriesResult> SearchAsync(SearchAssetCategoriesRequest request, CancellationToken cancellationToken = default)
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
        return new PagedAssetCategoriesResult(items.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<AssetCategoryDto> UpdateAsync(Guid id, UpdateAssetCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var category = await GetRequiredAsync(id, cancellationToken);
        AssetsPersistenceHelper.ApplyRowVersion(_db, category, request.RowVersion, c => c.RowVersion);
        category.Update(request.Name, request.Description, _user.UserId, _clock.UtcNow);
        await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
        return Map(category);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetRequiredAsync(id, cancellationToken);
        category.Activate(_user.UserId, _clock.UtcNow);
        await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetRequiredAsync(id, cancellationToken);
        category.Deactivate(_user.UserId, _clock.UtcNow);
        await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
    }

    private IQueryable<AssetCategory> OrgCategories() =>
        _db.AssetCategories.Where(c => c.OrganizationId == _org.OrganizationId);

    private async Task<AssetCategory> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await OrgCategories().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
            throw new AppException("assets.category_not_found", "Asset category was not found.", 404);
        return category;
    }

    private static AssetCategoryDto Map(AssetCategory c) =>
        new(c.Id, c.Code, c.Name, c.Description, c.IsActive, c.RowVersion);
}
