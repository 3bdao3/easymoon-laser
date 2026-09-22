using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Billing.Application.Common;
using ErpClink.Modules.Billing.Application.Payments;
using ErpClink.Modules.Billing.Application.Payments.Models;
using ErpClink.Modules.Billing.Domain.Invoices;
using ErpClink.Modules.Billing.Domain.Payments;
using ErpClink.Modules.Billing.Infrastructure.FinanceIntegration;
using ErpClink.Modules.Billing.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Billing.Infrastructure.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly BillingDbContext _db;
    private readonly IPaymentNumberGenerator _numbers;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IBillingDomainEventDispatcher _events;
    private readonly BillingArIntegration _ar;
    private readonly IValidator<RecordPaymentRequest> _recordValidator;
    private readonly IValidator<ReversePaymentRequest> _reverseValidator;

    public PaymentService(
        BillingDbContext db,
        IPaymentNumberGenerator numbers,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IBillingDomainEventDispatcher events,
        BillingArIntegration ar,
        IValidator<RecordPaymentRequest> recordValidator,
        IValidator<ReversePaymentRequest> reverseValidator)
    {
        _db = db;
        _numbers = numbers;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _ar = ar;
        _recordValidator = recordValidator;
        _reverseValidator = reverseValidator;
    }

    public async Task<PaymentDto> RecordAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _recordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("billing.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        if (!Enum.TryParse<PaymentMethod>(request.Method, true, out var method))
            throw new AppException("billing.invalid_payment_method", "Invalid payment method.", 400);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var invoice = await _db.Invoices
                .Where(i => i.Id == request.InvoiceId && i.OrganizationId == _org.OrganizationId)
                .Include(i => i.Lines)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new AppException("billing.invoice_not_found", "Invoice was not found.", 404);

            if (request.InvoiceRowVersion is { Length: > 0 })
                _db.Entry(invoice).Property(i => i.RowVersion).OriginalValue = request.InvoiceRowVersion;

            invoice.RegisterPayment(request.Amount, _user.UserId, _clock.UtcNow);

            var paymentNumber = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
            var payment = Payment.Capture(
                _org.OrganizationId,
                _org.BranchId,
                paymentNumber,
                invoice.Id,
                request.PaymentDate,
                request.Amount,
                invoice.CurrencyCode,
                method,
                request.ReferenceNumber,
                request.Notes,
                _user.UserId,
                _clock.UtcNow);

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await DispatchAsync(invoice, payment, cancellationToken);
            await _ar.OnPaymentCapturedAsync(invoice, payment, cancellationToken);
            return Map(payment);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("exceeds outstanding", StringComparison.OrdinalIgnoreCase))
        {
            await tx.RollbackAsync(cancellationToken);
            throw new AppException("billing.overpayment", ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new AppException("billing.invalid_transition", ex.Message, 400);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new AppException("billing.concurrency_conflict", "Invoice was modified by another operation.", 409);
        }
    }

    public async Task<PaymentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await OrgPayments().AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        return payment is null ? null : Map(payment);
    }

    public async Task<PagedPaymentsResult> SearchAsync(SearchPaymentsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgPayments().AsNoTracking();

        if (request.InvoiceId.HasValue) query = query.Where(p => p.InvoiceId == request.InvoiceId);
        if (request.PatientId.HasValue)
        {
            var patientId = request.PatientId.Value;
            query = query.Where(p => _db.Invoices.Any(i => i.Id == p.InvoiceId && i.PatientId == patientId));
        }

        if (!string.IsNullOrWhiteSpace(request.PaymentNumber))
            query = query.Where(p => p.PaymentNumber.Contains(request.PaymentNumber.Trim()));
        if (request.DateFrom.HasValue) query = query.Where(p => p.PaymentDate >= request.DateFrom);
        if (request.DateTo.HasValue) query = query.Where(p => p.PaymentDate <= request.DateTo);
        if (TryParseStatus(request.Status, out var status)) query = query.Where(p => p.Status == status);

        query = query.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var page = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedPaymentsResult(page.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<PaymentDto> ReverseAsync(Guid id, ReversePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _reverseValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("billing.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var payment = await OrgPayments()
                .SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
                ?? throw new AppException("billing.payment_not_found", "Payment was not found.", 404);

            var invoice = await _db.Invoices
                .Where(i => i.Id == payment.InvoiceId && i.OrganizationId == _org.OrganizationId)
                .Include(i => i.Lines)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new AppException("billing.invoice_not_found", "Invoice was not found.", 404);

            payment.Reverse(request.Reason, _user.UserId, _clock.UtcNow);
            invoice.ReversePayment(payment.Amount, _user.UserId, _clock.UtcNow);

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await DispatchAsync(invoice, payment, cancellationToken);
            await _ar.OnPaymentReversedAsync(invoice, payment, cancellationToken);
            return Map(payment);
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new AppException("billing.invalid_transition", ex.Message, 400);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new AppException("billing.concurrency_conflict", "Payment or invoice was modified by another operation.", 409);
        }
    }

    private IQueryable<Payment> OrgPayments() =>
        _db.Payments.Where(p => p.OrganizationId == _org.OrganizationId);

    private async Task DispatchAsync(Invoice invoice, Payment payment, CancellationToken cancellationToken)
    {
        var events = invoice.DomainEvents.Concat(payment.DomainEvents).ToList();
        invoice.ClearDomainEvents();
        payment.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static bool TryParseStatus(string? value, out PaymentStatus status) =>
        Enum.TryParse(value, true, out status);

    private static PaymentDto Map(Payment p) =>
        new(
            p.Id, p.OrganizationId, p.BranchId, p.PaymentNumber, p.InvoiceId, p.PaymentDate, p.Amount,
            p.CurrencyCode, p.Method.ToString(), p.ReferenceNumber, p.Notes, p.Status.ToString(),
            p.ReversedAtUtc, p.ReversedBy, p.ReversalReason, p.CreatedAtUtc, p.CreatedBy,
            p.UpdatedAtUtc, p.UpdatedBy, p.RowVersion);
}
