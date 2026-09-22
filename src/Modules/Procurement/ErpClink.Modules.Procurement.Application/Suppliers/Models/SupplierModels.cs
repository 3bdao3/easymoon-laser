namespace ErpClink.Modules.Procurement.Application.Suppliers.Models;

public sealed record CreateSupplierRequest(
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes);

public sealed record UpdateSupplierRequest(
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    byte[]? RowVersion);

public sealed record SearchSuppliersRequest(
    string? Search,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20);

public sealed record SupplierDto(
    Guid Id,
    Guid OrganizationId,
    string SupplierCode,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    byte[] RowVersion);

public sealed record PagedSuppliersResult(
    IReadOnlyList<SupplierDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
