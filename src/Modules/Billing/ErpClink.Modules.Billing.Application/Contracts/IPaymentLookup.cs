namespace ErpClink.Modules.Billing.Application.Contracts;

public interface IPaymentLookup
{
    Task<PaymentSummaryLookup?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
}

public sealed record PaymentSummaryLookup(
    Guid Id,
    Guid OrganizationId,
    Guid InvoiceId,
    string PaymentNumber,
    decimal Amount,
    string CurrencyCode,
    string Status,
    DateOnly PaymentDate);
