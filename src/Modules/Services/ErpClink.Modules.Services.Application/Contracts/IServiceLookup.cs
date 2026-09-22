namespace ErpClink.Modules.Services.Application.Contracts;

public sealed record ServiceLookupDto(
    Guid Id,
    Guid OrganizationId,
    string ServiceCode,
    string Name,
    decimal CurrentPrice,
    string CurrencyCode,
    bool IsActive,
    Guid? CurrentPriceId);

public sealed record PackageItemLookupDto(
    Guid ServiceId,
    string ServiceCode,
    string ServiceName,
    decimal Quantity,
    decimal UnitPrice,
    string CurrencyCode);

public interface IServiceLookup
{
    Task<ServiceLookupDto?> GetServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);

    Task<decimal?> GetCurrentPriceAsync(Guid serviceId, CancellationToken cancellationToken = default);
}

public sealed record PackageLookupDto(
    Guid Id,
    Guid OrganizationId,
    string PackageCode,
    string Name,
    bool IsActive);

public interface IPackageLookup
{
    Task<PackageLookupDto?> GetPackageAsync(Guid packageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageItemLookupDto>> GetPackageItemsAsync(
        Guid packageId,
        CancellationToken cancellationToken = default);
}
