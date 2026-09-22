using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.Billing.Application.Contracts;
using ErpClink.Modules.Billing.Domain.Invoices;
using ErpClink.Modules.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Billing.Infrastructure.Contracts;

public sealed class InvoiceLookup : IInvoiceLookup
{
    private readonly BillingDbContext _db;
    private readonly IOrganizationContext _org;

    public InvoiceLookup(BillingDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<InvoiceSummaryLookup?> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _db.Invoices.AsNoTracking()
            .Where(i => i.Id == invoiceId && i.OrganizationId == _org.OrganizationId)
            .Select(i => new InvoiceSummaryLookup(
                i.Id, i.OrganizationId, i.PatientId, i.InvoiceNumber, i.Status.ToString(),
                i.TotalAmount, i.PaidAmount, i.OutstandingAmount, i.CurrencyCode))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal> GetPatientOutstandingAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _db.Invoices.AsNoTracking()
            .Where(i => i.OrganizationId == _org.OrganizationId
                        && i.PatientId == patientId
                        && (i.Status == InvoiceStatus.Issued
                            || i.Status == InvoiceStatus.PartiallyPaid))
            .SumAsync(i => i.OutstandingAmount, cancellationToken);
    }
}

public sealed class PaymentLookup : IPaymentLookup
{
    private readonly BillingDbContext _db;
    private readonly IOrganizationContext _org;

    public PaymentLookup(BillingDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<PaymentSummaryLookup?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        return await _db.Payments.AsNoTracking()
            .Where(p => p.Id == paymentId && p.OrganizationId == _org.OrganizationId)
            .Select(p => new PaymentSummaryLookup(
                p.Id, p.OrganizationId, p.InvoiceId, p.PaymentNumber, p.Amount, p.CurrencyCode,
                p.Status.ToString(), p.PaymentDate))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
