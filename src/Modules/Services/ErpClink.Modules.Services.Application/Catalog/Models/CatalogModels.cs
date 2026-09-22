namespace ErpClink.Modules.Services.Application.Catalog.Models;

public sealed record CreateHealthcareServiceRequest(
    string ServiceCode,
    string Name,
    string? Description,
    Guid? CategoryId,
    decimal DefaultPrice,
    string CurrencyCode,
    int? DurationMinutes);

public sealed record UpdateHealthcareServiceRequest(
    string Name,
    string? Description,
    Guid? CategoryId,
    decimal? DefaultPrice,
    string? CurrencyCode,
    int? DurationMinutes,
    byte[]? RowVersion);

public sealed record SearchHealthcareServicesRequest(
    string? Query,
    string? ServiceCode,
    string? Name,
    Guid? CategoryId,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20);

public sealed record HealthcareServiceDto(
    Guid Id,
    Guid OrganizationId,
    string ServiceCode,
    string Name,
    string? Description,
    Guid? CategoryId,
    decimal DefaultPrice,
    string CurrencyCode,
    int? DurationMinutes,
    bool IsActive,
    Guid? CurrentPriceId,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    byte[] RowVersion);

public sealed record HealthcareServiceListItemDto(
    Guid Id,
    string ServiceCode,
    string Name,
    Guid? CategoryId,
    decimal DefaultPrice,
    string CurrencyCode,
    bool IsActive);

public sealed record PagedHealthcareServicesResult(
    IReadOnlyList<HealthcareServiceListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
