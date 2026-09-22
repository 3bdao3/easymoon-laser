using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.Services.Application.Contracts;
using ErpClink.Modules.Services.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Services.Infrastructure.Contracts;

public sealed class ServiceLookup : IServiceLookup
{
    private readonly ServicesDbContext _db;
    private readonly IOrganizationContext _org;

    public ServiceLookup(ServicesDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<ServiceLookupDto?> GetServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var service = await _db.HealthcareServices.AsNoTracking()
            .Include(s => s.Prices)
            .SingleOrDefaultAsync(s => s.OrganizationId == _org.OrganizationId && s.Id == serviceId, cancellationToken);
        if (service is null)
            return null;

        var current = service.Prices.FirstOrDefault(p => p.EffectiveToUtc is null);
        return new ServiceLookupDto(
            service.Id,
            service.OrganizationId,
            service.ServiceCode,
            service.Name,
            service.DefaultPrice,
            service.CurrencyCode,
            service.IsActive,
            current?.Id);
    }

    public async Task<decimal?> GetCurrentPriceAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var dto = await GetServiceAsync(serviceId, cancellationToken);
        return dto?.CurrentPrice;
    }
}

public sealed class PackageLookup : IPackageLookup
{
    private readonly ServicesDbContext _db;
    private readonly IOrganizationContext _org;

    public PackageLookup(ServicesDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<PackageLookupDto?> GetPackageAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = await _db.HealthcarePackages.AsNoTracking()
            .SingleOrDefaultAsync(p => p.OrganizationId == _org.OrganizationId && p.Id == packageId, cancellationToken);
        if (package is null)
            return null;

        return new PackageLookupDto(
            package.Id,
            package.OrganizationId,
            package.PackageCode,
            package.Name,
            package.IsActive);
    }

    public async Task<IReadOnlyList<PackageItemLookupDto>> GetPackageItemsAsync(
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package = await _db.HealthcarePackages.AsNoTracking()
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.OrganizationId == _org.OrganizationId && p.Id == packageId, cancellationToken);
        if (package is null)
            return Array.Empty<PackageItemLookupDto>();

        var serviceIds = package.Items.Select(i => i.ServiceId).Distinct().ToList();
        var services = await _db.HealthcareServices.AsNoTracking()
            .Include(s => s.Prices)
            .Where(s => s.OrganizationId == _org.OrganizationId && serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        return package.Items
            .OrderBy(i => i.SortOrder)
            .Select(i =>
            {
                services.TryGetValue(i.ServiceId, out var svc);
                return new PackageItemLookupDto(
                    i.ServiceId,
                    svc?.ServiceCode ?? string.Empty,
                    svc?.Name ?? string.Empty,
                    i.Quantity,
                    svc?.DefaultPrice ?? 0,
                    svc?.CurrencyCode ?? "EGP");
            })
            .ToList();
    }
}
