using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Assets.Application.Locations;
using ErpClink.Modules.Assets.Application.Locations.Models;
using ErpClink.Modules.Assets.Domain.Locations;
using ErpClink.Modules.Assets.Infrastructure.Common;
using ErpClink.Modules.Assets.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Assets.Infrastructure.Locations;

public sealed class AssetLocationService : IAssetLocationService
{
    private readonly AssetsDbContext _db;
    private readonly IAssetLocationCodeGenerator _codes;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IValidator<CreateAssetLocationRequest> _createValidator;
    private readonly IValidator<UpdateAssetLocationRequest> _updateValidator;

    public AssetLocationService(
        AssetsDbContext db,
        IAssetLocationCodeGenerator codes,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IValidator<CreateAssetLocationRequest> createValidator,
        IValidator<UpdateAssetLocationRequest> updateValidator)
    {
        _db = db;
        _codes = codes;
        _org = org;
        _user = user;
        _clock = clock;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<AssetLocationDto> CreateAsync(CreateAssetLocationRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var code = await _codes.GenerateAsync(_org.OrganizationId, cancellationToken);
        var location = AssetLocation.Create(
            _org.OrganizationId, _org.BranchId, code, request.Name, request.Description, _user.UserId, _clock.UtcNow);
        try
        {
            _db.AssetLocations.Add(location);
            await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
            return Map(location);
        }
        catch (DbUpdateException ex) when (AssetsPersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("assets.duplicate_location_code", "Asset location code already exists.", 409);
        }
    }

    public async Task<AssetLocationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await OrgLocations().AsNoTracking().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        return location is null ? null : Map(location);
    }

    public async Task<PagedAssetLocationsResult> SearchAsync(SearchAssetLocationsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgLocations().AsNoTracking().Where(l => l.BranchId == _org.BranchId);
        if (request.IsActive == true) query = query.Where(l => l.IsActive);
        else if (request.IsActive == false) query = query.Where(l => !l.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(l => l.Code.Contains(q) || l.Name.Contains(q));
        }

        query = query.OrderBy(l => l.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedAssetLocationsResult(items.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<AssetLocationDto> UpdateAsync(Guid id, UpdateAssetLocationRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var location = await GetRequiredAsync(id, cancellationToken);
        AssetsPersistenceHelper.ApplyRowVersion(_db, location, request.RowVersion, l => l.RowVersion);
        location.Update(request.Name, request.Description, _user.UserId, _clock.UtcNow);
        await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
        return Map(location);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await GetRequiredAsync(id, cancellationToken);
        location.Activate(_user.UserId, _clock.UtcNow);
        await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await GetRequiredAsync(id, cancellationToken);
        location.Deactivate(_user.UserId, _clock.UtcNow);
        await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
    }

    private IQueryable<AssetLocation> OrgLocations() =>
        _db.AssetLocations.Where(l => l.OrganizationId == _org.OrganizationId);

    private async Task<AssetLocation> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var location = await OrgLocations().SingleOrDefaultAsync(l => l.Id == id && l.BranchId == _org.BranchId, cancellationToken);
        if (location is null)
            throw new AppException("assets.location_not_found", "Asset location was not found.", 404);
        return location;
    }

    private static AssetLocationDto Map(AssetLocation l) =>
        new(l.Id, l.OrganizationId, l.BranchId, l.Code, l.Name, l.Description, l.IsActive, l.RowVersion);
}
