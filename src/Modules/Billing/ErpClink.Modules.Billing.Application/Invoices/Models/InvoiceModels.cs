namespace ErpClink.Modules.Billing.Application.Invoices.Models;

public sealed record InvoiceLineInput(
    Guid? ServiceId,
    Guid? PackageId,
    decimal Quantity,
    decimal? LineDiscountAmount,
    int? SortOrder);

public sealed record CreateDraftInvoiceRequest(
    Guid PatientId,
    Guid? MedicalVisitId,
    DateOnly InvoiceDate,
    string CurrencyCode,
    string? Notes,
    decimal? InvoiceDiscountAmount,
    IReadOnlyList<InvoiceLineInput>? Lines);

public sealed record UpdateDraftInvoiceRequest(
    DateOnly InvoiceDate,
    string? Notes,
    decimal? InvoiceDiscountAmount,
    byte[]? RowVersion);

public sealed record AddInvoiceLineRequest(
    InvoiceLineInput Line,
    byte[]? RowVersion);

public sealed record UpdateInvoiceLineRequest(
    decimal Quantity,
    decimal LineDiscountAmount,
    int SortOrder,
    byte[]? RowVersion);

public sealed record VoidInvoiceRequest(string? Reason, byte[]? RowVersion);

public sealed record SearchInvoicesRequest(
    Guid? PatientId,
    Guid? MedicalVisitId,
    string? InvoiceNumber,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status,
    int Page = 1,
    int PageSize = 20);

public sealed record PatientInvoicesRequest(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status,
    int Page = 1,
    int PageSize = 20);

public sealed record InvoiceLineDto(
    Guid Id,
    string Source,
    Guid? ServiceId,
    Guid? PackageId,
    Guid? ServicePriceId,
    string DescriptionSnapshot,
    string? ServiceCodeSnapshot,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineDiscountAmount,
    decimal LineSubtotal,
    decimal LineTotal,
    string CurrencyCode,
    int SortOrder);

public sealed record InvoiceDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string? InvoiceNumber,
    Guid PatientId,
    Guid? MedicalVisitId,
    DateOnly InvoiceDate,
    string Status,
    string CurrencyCode,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    string? Notes,
    DateTime? IssuedAtUtc,
    string? IssuedBy,
    DateTime? VoidedAtUtc,
    string? VoidedBy,
    string? VoidReason,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    byte[] RowVersion,
    IReadOnlyList<InvoiceLineDto> Lines);

public sealed record PagedInvoicesResult(
    IReadOnlyList<InvoiceDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PatientOutstandingDto(Guid PatientId, decimal OutstandingAmount, string CurrencyCode);
