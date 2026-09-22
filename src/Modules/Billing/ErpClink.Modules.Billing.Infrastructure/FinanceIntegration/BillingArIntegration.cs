using ErpClink.Modules.Billing.Domain.Invoices;
using ErpClink.Modules.Billing.Domain.Payments;
using ErpClink.Modules.Finance.Application.Integration;
using ErpClink.Modules.Finance.Application.Subledgers;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Billing.Infrastructure.FinanceIntegration;

/// <summary>
/// Posts AR subledger movements from Billing events. Does not create GL journals.
/// Eventual consistency: called after Billing commit (same pattern as Inventory→Procurement).
/// </summary>
public sealed class BillingArIntegration
{
    private readonly ISubledgerPostingPort _subledger;
    private readonly ILogger<BillingArIntegration> _logger;

    public BillingArIntegration(ISubledgerPostingPort subledger, ILogger<BillingArIntegration> logger)
    {
        _subledger = subledger;
        _logger = logger;
    }

    public async Task OnInvoiceIssuedAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var result = await _subledger.PostArAsync(new SubledgerPostRequest(
            OrganizationId: invoice.OrganizationId,
            BranchId: invoice.BranchId,
            PartyId: invoice.PatientId,
            SourceModule: AccountingSourceModules.Billing,
            SourceType: "Invoice",
            SourceId: invoice.Id.ToString("N"),
            EventType: "InvoiceIssued",
            TransactionDate: invoice.InvoiceDate,
            Amount: invoice.TotalAmount,
            CurrencyCode: invoice.CurrencyCode,
            Direction: "Debit",
            Description: $"Invoice {invoice.InvoiceNumber}",
            PartyDisplayNameSnapshot: null,
            DueDate: null, // Invoice has no due date in V1 — aging uses TransactionDate
            CorrelationId: Guid.NewGuid(),
            IdempotencyKey: $"billing:invoice:{invoice.Id:N}:issued"), cancellationToken);

        Log(result, "InvoiceIssued", invoice.Id);
    }

    public async Task OnInvoiceVoidedAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        // Draft void never posted AR. Issued unpaid void compensates the receivable.
        if (invoice.IssuedAtUtc is null || invoice.TotalAmount <= 0)
            return;

        var result = await _subledger.PostArAsync(new SubledgerPostRequest(
            OrganizationId: invoice.OrganizationId,
            BranchId: invoice.BranchId,
            PartyId: invoice.PatientId,
            SourceModule: AccountingSourceModules.Billing,
            SourceType: "Invoice",
            SourceId: invoice.Id.ToString("N"),
            EventType: "InvoiceVoided",
            TransactionDate: invoice.InvoiceDate,
            Amount: invoice.TotalAmount,
            CurrencyCode: invoice.CurrencyCode,
            Direction: "Credit",
            Description: $"Void invoice {invoice.InvoiceNumber}",
            PartyDisplayNameSnapshot: null,
            DueDate: null,
            CorrelationId: Guid.NewGuid(),
            IdempotencyKey: $"billing:invoice:{invoice.Id:N}:voided"), cancellationToken);

        Log(result, "InvoiceVoided", invoice.Id);
    }

    public async Task OnPaymentCapturedAsync(Invoice invoice, Payment payment, CancellationToken cancellationToken = default)
    {
        var result = await _subledger.PostArAsync(new SubledgerPostRequest(
            OrganizationId: payment.OrganizationId,
            BranchId: payment.BranchId,
            PartyId: invoice.PatientId,
            SourceModule: AccountingSourceModules.Billing,
            SourceType: "Payment",
            SourceId: payment.Id.ToString("N"),
            EventType: "PaymentCaptured",
            TransactionDate: payment.PaymentDate,
            Amount: payment.Amount,
            CurrencyCode: payment.CurrencyCode,
            Direction: "Credit",
            Description: $"Payment {payment.PaymentNumber} for invoice {invoice.InvoiceNumber}",
            PartyDisplayNameSnapshot: null,
            DueDate: null,
            CorrelationId: Guid.NewGuid(),
            IdempotencyKey: $"billing:payment:{payment.Id:N}:captured"), cancellationToken);

        Log(result, "PaymentCaptured", payment.Id);
    }

    public async Task OnPaymentReversedAsync(Invoice invoice, Payment payment, CancellationToken cancellationToken = default)
    {
        var result = await _subledger.PostArAsync(new SubledgerPostRequest(
            OrganizationId: payment.OrganizationId,
            BranchId: payment.BranchId,
            PartyId: invoice.PatientId,
            SourceModule: AccountingSourceModules.Billing,
            SourceType: "Payment",
            SourceId: payment.Id.ToString("N"),
            EventType: "PaymentReversed",
            TransactionDate: payment.PaymentDate,
            Amount: payment.Amount,
            CurrencyCode: payment.CurrencyCode,
            Direction: "Debit",
            Description: $"Reverse payment {payment.PaymentNumber}",
            PartyDisplayNameSnapshot: null,
            DueDate: null,
            CorrelationId: Guid.NewGuid(),
            IdempotencyKey: $"billing:payment:{payment.Id:N}:reversed"), cancellationToken);

        Log(result, "PaymentReversed", payment.Id);
    }

    private void Log(SubledgerPostResult result, string eventType, Guid sourceId)
    {
        if (result.Outcome == SubledgerPostOutcome.Failed)
        {
            _logger.LogError(
                "AR subledger post failed for {EventType} {SourceId}: {Code} {Message}",
                eventType, sourceId, result.ErrorCode, result.ErrorMessage);
            throw new InvalidOperationException($"AR subledger post failed: {result.ErrorCode} {result.ErrorMessage}");
        }

        _logger.LogInformation(
            "AR subledger {Outcome} for {EventType} {SourceId} Tx={TransactionId}",
            result.Outcome, eventType, sourceId, result.TransactionId);
    }
}
