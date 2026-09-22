namespace ErpClink.Modules.Billing.Application.Payments.Models;

public sealed record RecordPaymentRequest(
    Guid InvoiceId,
    DateOnly PaymentDate,
    decimal Amount,
    string Method,
    string? ReferenceNumber,
    string? Notes,
    byte[]? InvoiceRowVersion);

public sealed record ReversePaymentRequest(string? Reason);

public sealed record SearchPaymentsRequest(
    Guid? InvoiceId,
    Guid? PatientId,
    string? PaymentNumber,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status,
    int Page = 1,
    int PageSize = 20);

public sealed record PaymentDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string PaymentNumber,
    Guid InvoiceId,
    DateOnly PaymentDate,
    decimal Amount,
    string CurrencyCode,
    string Method,
    string? ReferenceNumber,
    string? Notes,
    string Status,
    DateTime? ReversedAtUtc,
    string? ReversedBy,
    string? ReversalReason,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    byte[] RowVersion);

public sealed record PagedPaymentsResult(
    IReadOnlyList<PaymentDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
