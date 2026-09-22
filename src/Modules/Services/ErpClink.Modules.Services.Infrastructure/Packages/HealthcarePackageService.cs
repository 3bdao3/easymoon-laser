using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Services.Application.Common;
using ErpClink.Modules.Services.Application.Packages;
using ErpClink.Modules.Services.Application.Packages.Models;
using ErpClink.Modules.Services.Domain.Packages;
using ErpClink.Modules.Services.Infrastructure.Common;
using ErpClink.Modules.Services.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Services.Infrastructure.Packages;

public sealed class HealthcarePackageService : IHealthcarePackageService
{
    private readonly ServicesDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IServicesDomainEventDispatcher _events;
    private readonly IValidator<CreateHealthcarePackageRequest> _createValidator;
    private readonly IValidator<UpdateHealthcarePackageRequest> _updateValidator;
    private readonly IValidator<PackageItemInput> _itemValidator;
    private readonly IValidator<UpdatePackageItemRequest> _updateItemValidator;

    public HealthcarePackageService(
        ServicesDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IServicesDomainEventDispatcher events,
        IValidator<CreateHealthcarePackageRequest> createValidator,
        IValidator<UpdateHealthcarePackageRequest> updateValidator,
        IValidator<PackageItemInput> itemValidator,
        IValidator<UpdatePackageItemRequest> updateItemValidator)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _itemValidator = itemValidator;
        _updateItemValidator = updateItemValidator;
    }

    public async Task<HealthcarePackageDto> CreateAsync(CreateHealthcarePackageRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("packages.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var package = HealthcarePackage.Create(
            _org.OrganizationId,
            request.PackageCode,
            request.Name,
            request.Description,
            _user.UserId,
            _clock.UtcNow);

        if (request.Items is { Count: > 0 })
        {
            foreach (var input in request.Items)
            {
                await ValidateServiceForPackageAsync(input, cancellationToken);
                try
                {
                    package.AddItem(input.ServiceId, input.Quantity, input.SortOrder, _user.UserId, _clock.UtcNow);
                }
                catch (InvalidOperationException ex)
                {
                    throw new AppException("packages.invalid_operation", ex.Message, 400);
                }
            }
        }

        try
        {
            _db.HealthcarePackages.Add(package);
            await ServicesPersistenceHelper.SaveChangesAsync(_db, "packages.concurrency_conflict", cancellationToken);
            await DispatchAsync(package, cancellationToken);
            return await MapPackageAsync(package.Id, cancellationToken);
        }
        catch (DbUpdateException ex) when (ServicesPersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("packages.duplicate_code", "Package code already exists.", 409);
        }
    }

    public async Task<HealthcarePackageDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await OrgPackages().AsNoTracking().AnyAsync(p => p.Id == id, cancellationToken);
        return exists ? await MapPackageAsync(id, cancellationToken) : null;
    }

    public async Task<PagedHealthcarePackagesResult> SearchAsync(SearchHealthcarePackagesRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgPackages().AsNoTracking();

        if (request.IsActive == true)
            query = query.Where(p => p.IsActive);
        else if (request.IsActive == false)
            query = query.Where(p => !p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.PackageCode))
            query = query.Where(p => p.PackageCode.Contains(request.PackageCode.Trim()));

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(p =>
                p.PackageCode.Contains(q) ||
                p.Name.Contains(q) ||
                (p.Description != null && p.Description.Contains(q)));
        }

        query = query.OrderBy(p => p.Name);
        var total = await query.CountAsync(cancellationToken);
        var packages = await query
            .Select(p => new { p.Id, p.PackageCode, p.Name, p.IsActive, ItemCount = p.Items.Count })
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedHealthcarePackagesResult(
            packages.Select(p => new HealthcarePackageListItemDto(p.Id, p.PackageCode, p.Name, p.IsActive, p.ItemCount)).ToList(),
            paging.NormalizedPage,
            paging.NormalizedPageSize,
            total);
    }

    public async Task<HealthcarePackageDto> UpdateAsync(Guid id, UpdateHealthcarePackageRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("packages.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var package = await GetRequiredAsync(id, cancellationToken);
        ServicesPersistenceHelper.ApplyRowVersion(_db, package, request.RowVersion, p => p.RowVersion);

        try
        {
            package.Update(request.Name, request.Description, _user.UserId, _clock.UtcNow);
            await ServicesPersistenceHelper.SaveChangesAsync(_db, "packages.concurrency_conflict", cancellationToken);
            await DispatchAsync(package, cancellationToken);
            return await MapPackageAsync(id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("packages.invalid_operation", ex.Message, 400);
        }
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var package = await GetRequiredAsync(id, cancellationToken);
        package.Activate(_user.UserId, _clock.UtcNow);
        await ServicesPersistenceHelper.SaveChangesAsync(_db, "packages.concurrency_conflict", cancellationToken);
        await DispatchAsync(package, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var package = await GetRequiredAsync(id, cancellationToken);
        package.Deactivate(_user.UserId, _clock.UtcNow);
        await ServicesPersistenceHelper.SaveChangesAsync(_db, "packages.concurrency_conflict", cancellationToken);
        await DispatchAsync(package, cancellationToken);
    }

    public async Task<HealthcarePackageDto> AddItemAsync(Guid id, PackageItemInput request, CancellationToken cancellationToken = default)
    {
        var validation = await _itemValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("packages.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        await ValidateServiceForPackageAsync(request, cancellationToken);
        var package = await GetRequiredAsync(id, cancellationToken);

        try
        {
            package.AddItem(request.ServiceId, request.Quantity, request.SortOrder, _user.UserId, _clock.UtcNow);
            await ServicesPersistenceHelper.SaveChangesAsync(_db, "packages.concurrency_conflict", cancellationToken);
            await DispatchAsync(package, cancellationToken);
            return await MapPackageAsync(id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("packages.invalid_operation", ex.Message, 400);
        }
        catch (DbUpdateException ex) when (ServicesPersistenceHelper.IsUniqueViolation(ex))
        {
            throw new AppException("packages.duplicate_item", "Service already exists in this package.", 409);
        }
    }

    public async Task<HealthcarePackageDto> UpdateItemAsync(Guid id, Guid itemId, UpdatePackageItemRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateItemValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("packages.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var package = await GetRequiredAsync(id, cancellationToken);
        ServicesPersistenceHelper.ApplyRowVersion(_db, package, request.RowVersion, p => p.RowVersion);

        try
        {
            package.UpdateItem(itemId, request.Quantity, request.SortOrder, _user.UserId, _clock.UtcNow);
            await ServicesPersistenceHelper.SaveChangesAsync(_db, "packages.concurrency_conflict", cancellationToken);
            await DispatchAsync(package, cancellationToken);
            return await MapPackageAsync(id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("packages.invalid_operation", ex.Message, 400);
        }
    }

    public async Task<HealthcarePackageDto> RemoveItemAsync(Guid id, Guid itemId, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        var package = await GetRequiredAsync(id, cancellationToken);
        ServicesPersistenceHelper.ApplyRowVersion(_db, package, rowVersion, p => p.RowVersion);

        try
        {
            package.RemoveItem(itemId, _user.UserId, _clock.UtcNow);
            await ServicesPersistenceHelper.SaveChangesAsync(_db, "packages.concurrency_conflict", cancellationToken);
            await DispatchAsync(package, cancellationToken);
            return await MapPackageAsync(id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("packages.invalid_operation", ex.Message, 400);
        }
    }

    private IQueryable<HealthcarePackage> OrgPackages() =>
        _db.HealthcarePackages.Where(p => p.OrganizationId == _org.OrganizationId);

    private async Task<HealthcarePackage> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var package = await OrgPackages()
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (package is null)
            throw new AppException("packages.not_found", "Package was not found.", 404);
        return package;
    }

    private async Task ValidateServiceForPackageAsync(PackageItemInput input, CancellationToken cancellationToken)
    {
        var service = await _db.HealthcareServices.AsNoTracking()
            .SingleOrDefaultAsync(s => s.OrganizationId == _org.OrganizationId && s.Id == input.ServiceId, cancellationToken);
        if (service is null)
            throw new AppException("services.not_found", "Service was not found.", 404);
        if (!service.IsActive)
            throw new AppException("services.inactive", "Inactive services cannot be added to a package.", 400);
    }

    private async Task DispatchAsync(HealthcarePackage package, CancellationToken cancellationToken)
    {
        var events = package.DomainEvents.ToList();
        package.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private async Task<HealthcarePackageDto> MapPackageAsync(Guid packageId, CancellationToken cancellationToken)
    {
        var package = await OrgPackages()
            .AsNoTracking()
            .Include(p => p.Items)
            .SingleAsync(p => p.Id == packageId, cancellationToken);

        var serviceIds = package.Items.Select(i => i.ServiceId).Distinct().ToList();
        var services = await _db.HealthcareServices.AsNoTracking()
            .Where(s => s.OrganizationId == _org.OrganizationId && serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var items = package.Items
            .OrderBy(i => i.SortOrder)
            .Select(i =>
            {
                services.TryGetValue(i.ServiceId, out var svc);
                return new PackageItemDto(
                    i.Id,
                    i.ServiceId,
                    svc?.ServiceCode ?? string.Empty,
                    svc?.Name ?? string.Empty,
                    i.Quantity,
                    i.SortOrder);
            })
            .ToList();

        return new HealthcarePackageDto(
            package.Id,
            package.OrganizationId,
            package.PackageCode,
            package.Name,
            package.Description,
            package.IsActive,
            package.CreatedAtUtc,
            package.CreatedBy,
            package.UpdatedAtUtc,
            package.UpdatedBy,
            package.RowVersion,
            items);
    }
}
