using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Assets.Application.Assets;
using ErpClink.Modules.Assets.Application.Assets.Models;
using ErpClink.Modules.Assets.Domain;
using ErpClink.Modules.Assets.Domain.Assets;
using ErpClink.Modules.Assets.Domain.History;
using ErpClink.Modules.Assets.Infrastructure.Common;
using ErpClink.Modules.Assets.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Assets.Infrastructure.Assets;

public sealed class AssetService : IAssetService
{
    private const string EventCreated = "Created";
    private const string EventUpdated = "Updated";
    private const string EventLocationChanged = "LocationChanged";
    private const string EventMaintenanceStarted = "MaintenanceStarted";
    private const string EventMaintenanceCompleted = "MaintenanceCompleted";
    private const string EventRetired = "Retired";

    private readonly AssetsDbContext _db;
    private readonly IAssetNumberGenerator _numbers;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IValidator<CreateAssetRequest> _createValidator;
    private readonly IValidator<UpdateAssetRequest> _updateValidator;
    private readonly IValidator<ChangeAssetLocationRequest> _changeLocationValidator;
    private readonly IValidator<StartAssetMaintenanceRequest> _startMaintenanceValidator;
    private readonly IValidator<RetireAssetRequest> _retireValidator;

    public AssetService(
        AssetsDbContext db,
        IAssetNumberGenerator numbers,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IValidator<CreateAssetRequest> createValidator,
        IValidator<UpdateAssetRequest> updateValidator,
        IValidator<ChangeAssetLocationRequest> changeLocationValidator,
        IValidator<StartAssetMaintenanceRequest> startMaintenanceValidator,
        IValidator<RetireAssetRequest> retireValidator)
    {
        _db = db;
        _numbers = numbers;
        _org = org;
        _user = user;
        _clock = clock;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _changeLocationValidator = changeLocationValidator;
        _startMaintenanceValidator = startMaintenanceValidator;
        _retireValidator = retireValidator;
    }

    public async Task<AssetDto> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        await EnsureActiveCategoryAsync(request.AssetCategoryId, cancellationToken);
        await EnsureActiveLocationAsync(request.AssetLocationId, cancellationToken);

        try
        {
            var assetNumber = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
            var asset = Domain.Assets.Asset.Create(
                _org.OrganizationId,
                _org.BranchId,
                assetNumber,
                request.Name,
                request.Description,
                request.AssetCategoryId,
                request.AssetLocationId,
                request.SerialNumber,
                request.PurchaseDate,
                request.AcquisitionCost,
                request.AcquisitionReference,
                request.WarrantyStartDate,
                request.WarrantyEndDate,
                request.WarrantyNotes,
                _user.UserId,
                _clock.UtcNow);

            _db.Assets.Add(asset);
            AddHistory(asset, EventCreated, $"Asset {asset.AssetNumber} registered.", null, asset.Status.ToString(), null);
            await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
            return Map(asset);
        }
        catch (ArgumentException ex)
        {
            throw new AppException("assets.validation_failed", ex.Message, 400);
        }
        catch (DbUpdateException ex) when (AssetsPersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("assets.duplicate_asset_number", "Asset number or serial number already exists.", 409);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("assets.invalid_operation", ex.Message, 409);
        }
    }

    public async Task<AssetDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await OrgAssets().AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        return asset is null ? null : Map(asset);
    }

    public async Task<PagedAssetsResult> SearchAsync(SearchAssetsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgAssets().AsNoTracking().Where(a => a.BranchId == _org.BranchId);
        if (request.AssetCategoryId is { } catId) query = query.Where(a => a.AssetCategoryId == catId);
        if (request.AssetLocationId is { } locId) query = query.Where(a => a.AssetLocationId == locId);
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<AssetStatus>(request.Status, true, out var status))
            query = query.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(a =>
                a.Name.Contains(q) ||
                a.AssetNumber.Contains(q) ||
                (a.SerialNumber != null && a.SerialNumber.Contains(q)));
        }

        query = query.OrderBy(a => a.AssetNumber);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedAssetsResult(items.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<AssetDto> UpdateAsync(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        await EnsureActiveCategoryAsync(request.AssetCategoryId, cancellationToken);
        var asset = await GetRequiredAsync(id, cancellationToken);
        AssetsPersistenceHelper.ApplyRowVersion(_db, asset, request.RowVersion, a => a.RowVersion);

        try
        {
            asset.UpdateDetails(
                request.Name,
                request.Description,
                request.AssetCategoryId,
                request.SerialNumber,
                request.PurchaseDate,
                request.AcquisitionCost,
                request.AcquisitionReference,
                request.WarrantyStartDate,
                request.WarrantyEndDate,
                request.WarrantyNotes,
                _user.UserId,
                _clock.UtcNow);
            AddHistory(asset, EventUpdated, "Asset details updated.", null, null, null);
            await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
            return Map(asset);
        }
        catch (ArgumentException ex)
        {
            throw new AppException("assets.validation_failed", ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("assets.invalid_operation", ex.Message, 409);
        }
        catch (DbUpdateException ex) when (AssetsPersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("assets.duplicate_serial", "Serial number already exists in this organization.", 409);
        }
    }

    public async Task<AssetDto> ChangeLocationAsync(Guid id, ChangeAssetLocationRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _changeLocationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        await EnsureActiveLocationAsync(request.AssetLocationId, cancellationToken);
        var asset = await GetRequiredAsync(id, cancellationToken);
        AssetsPersistenceHelper.ApplyRowVersion(_db, asset, request.RowVersion, a => a.RowVersion);

        try
        {
            var previous = asset.AssetLocationId.ToString();
            asset.ChangeLocation(request.AssetLocationId, _user.UserId, _clock.UtcNow);
            AddHistory(asset, EventLocationChanged, "Asset location changed.", previous, request.AssetLocationId.ToString(), null);
            await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
            return Map(asset);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("assets.invalid_operation", ex.Message, 409);
        }
    }

    public async Task<AssetDto> StartMaintenanceAsync(Guid id, StartAssetMaintenanceRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _startMaintenanceValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var asset = await GetRequiredAsync(id, cancellationToken);
        AssetsPersistenceHelper.ApplyRowVersion(_db, asset, request.RowVersion, a => a.RowVersion);

        try
        {
            var previous = asset.Status.ToString();
            asset.StartMaintenance(request.Notes, _user.UserId, _clock.UtcNow);
            AddHistory(asset, EventMaintenanceStarted, "Maintenance started.", previous, asset.Status.ToString(), request.Notes);
            await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
            return Map(asset);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("assets.invalid_operation", ex.Message, 409);
        }
    }

    public async Task<AssetDto> CompleteMaintenanceAsync(Guid id, CompleteAssetMaintenanceRequest request, CancellationToken cancellationToken = default)
    {
        var asset = await GetRequiredAsync(id, cancellationToken);
        AssetsPersistenceHelper.ApplyRowVersion(_db, asset, request.RowVersion, a => a.RowVersion);

        try
        {
            var previous = asset.Status.ToString();
            asset.CompleteMaintenance(_user.UserId, _clock.UtcNow);
            AddHistory(asset, EventMaintenanceCompleted, "Maintenance completed.", previous, asset.Status.ToString(), null);
            await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
            return Map(asset);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("assets.invalid_operation", ex.Message, 409);
        }
    }

    public async Task<AssetDto> RetireAsync(Guid id, RetireAssetRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _retireValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var asset = await GetRequiredAsync(id, cancellationToken);
        AssetsPersistenceHelper.ApplyRowVersion(_db, asset, request.RowVersion, a => a.RowVersion);

        try
        {
            var previous = asset.Status.ToString();
            asset.Retire(request.Reason, _user.UserId, _clock.UtcNow);
            AddHistory(asset, EventRetired, "Asset retired.", previous, asset.Status.ToString(), request.Reason);
            await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
            return Map(asset);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("assets.invalid_operation", ex.Message, 409);
        }
    }

    public async Task<IReadOnlyList<AssetHistoryEntryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _ = await GetRequiredAsync(id, cancellationToken);
        var entries = await _db.AssetHistory.AsNoTracking()
            .Where(h => h.AssetId == id && h.OrganizationId == _org.OrganizationId)
            .OrderByDescending(h => h.OccurredAtUtc)
            .ThenByDescending(h => h.Id)
            .ToListAsync(cancellationToken);

        return entries.Select(h => new AssetHistoryEntryDto(
            h.Id, h.EventType, h.Summary, h.OldValue, h.NewValue, h.Notes, h.OccurredAtUtc, h.OccurredBy)).ToList();
    }

    private IQueryable<Domain.Assets.Asset> OrgAssets() =>
        _db.Assets.Where(a => a.OrganizationId == _org.OrganizationId);

    private async Task<Domain.Assets.Asset> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var asset = await OrgAssets().SingleOrDefaultAsync(a => a.Id == id && a.BranchId == _org.BranchId, cancellationToken);
        if (asset is null)
            throw new AppException("assets.asset_not_found", "Asset was not found.", 404);
        return asset;
    }

    private async Task EnsureActiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _db.AssetCategories.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == categoryId && c.OrganizationId == _org.OrganizationId, cancellationToken);
        if (category is null)
            throw new AppException("assets.category_not_found", "Asset category was not found.", 404);
        if (!category.IsActive)
            throw new AppException("assets.category_inactive", "Asset category is not active.", 409);
    }

    private async Task EnsureActiveLocationAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var location = await _db.AssetLocations.AsNoTracking()
            .SingleOrDefaultAsync(l =>
                l.Id == locationId &&
                l.OrganizationId == _org.OrganizationId &&
                l.BranchId == _org.BranchId, cancellationToken);
        if (location is null)
            throw new AppException("assets.location_not_found", "Asset location was not found.", 404);
        if (!location.IsActive)
            throw new AppException("assets.location_inactive", "Asset location is not active.", 409);
    }

    private void AddHistory(
        Domain.Assets.Asset asset,
        string eventType,
        string summary,
        string? oldValue,
        string? newValue,
        string? notes) =>
        _db.AssetHistory.Add(AssetHistoryEntry.Record(
            asset.Id,
            asset.OrganizationId,
            eventType,
            summary,
            _clock.UtcNow,
            _user.UserId,
            oldValue,
            newValue,
            notes));

    private static AssetDto Map(Domain.Assets.Asset a) =>
        new(
            a.Id,
            a.OrganizationId,
            a.BranchId,
            a.AssetNumber,
            a.Name,
            a.Description,
            a.AssetCategoryId,
            a.AssetLocationId,
            a.SerialNumber,
            a.PurchaseDate,
            a.AcquisitionCost,
            a.AcquisitionReference,
            a.WarrantyStartDate,
            a.WarrantyEndDate,
            a.WarrantyNotes,
            a.Status.ToString(),
            a.MaintenanceNotes,
            a.RetiredAtUtc,
            a.RetirementReason,
            a.CreatedAtUtc,
            a.RowVersion);
}
