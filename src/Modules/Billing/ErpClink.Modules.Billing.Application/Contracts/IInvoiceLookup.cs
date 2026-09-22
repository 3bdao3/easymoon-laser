namespace ErpClink.Modules.Billing.Application.Contracts;

public interface IInvoiceLookup
{
    Task<InvoiceSummaryLookup?> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    Task<decimal> GetPatientOutstandingAsync(Guid patientId, CancellationToken cancellationToken = default);
}

public sealed record InvoiceSummaryLookup(
    Guid Id,
    Guid OrganizationId,
    Guid PatientId,
    string? InvoiceNumber,
    string Status,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    string CurrencyCode);
