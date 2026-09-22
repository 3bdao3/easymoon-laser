using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Billing.Application.Common;
using ErpClink.Modules.Billing.Application.Invoices;
using ErpClink.Modules.Billing.Application.Invoices.Models;
using ErpClink.Modules.Billing.Domain.Invoices;
using ErpClink.Modules.Billing.Infrastructure.FinanceIntegration;
using ErpClink.Modules.Billing.Infrastructure.Persistence;
using ErpClink.Modules.MedicalVisits.Application.Contracts;
using ErpClink.Modules.Patients.Application.Contracts;
using ErpClink.Modules.Services.Application.Contracts;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Billing.Infrastructure.Invoices;

public sealed class InvoiceService : IInvoiceService
{
    private readonly BillingDbContext _db;
    private readonly IInvoiceNumberGenerator _numbers;
    private readonly IPatientLookup _patients;
    private readonly IMedicalVisitBillingPort _visits;
    private readonly IServiceLookup _services;
    private readonly IPackageLookup _packages;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IBillingDomainEventDispatcher _events;
    private readonly BillingArIntegration _ar;
    private readonly IValidator<CreateDraftInvoiceRequest> _createValidator;
    private readonly IValidator<UpdateDraftInvoiceRequest> _updateValidator;
    private readonly IValidator<AddInvoiceLineRequest> _addLineValidator;
    private readonly IValidator<UpdateInvoiceLineRequest> _updateLineValidator;
    private readonly IValidator<VoidInvoiceRequest> _voidValidator;

    public InvoiceService(
        BillingDbContext db,
        IInvoiceNumberGenerator numbers,
        IPatientLookup patients,
        IMedicalVisitBillingPort visits,
        IServiceLookup services,
        IPackageLookup packages,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IBillingDomainEventDispatcher events,
        BillingArIntegration ar,
        IValidator<CreateDraftInvoiceRequest> createValidator,
        IValidator<UpdateDraftInvoiceRequest> updateValidator,
        IValidator<AddInvoiceLineRequest> addLineValidator,
        IValidator<UpdateInvoiceLineRequest> updateLineValidator,
        IValidator<VoidInvoiceRequest> voidValidator)
    {
        _db = db;
        _numbers = numbers;
        _patients = patients;
        _visits = visits;
        _services = services;
        _packages = packages;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _ar = ar;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _addLineValidator = addLineValidator;
        _updateLineValidator = updateLineValidator;
        _voidValidator = voidValidator;
    }

    public async Task<InvoiceDto> CreateDraftAsync(CreateDraftInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("billing.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        await EnsurePatientAsync(request.PatientId, cancellationToken);
        await EnsureVisitAsync(request.MedicalVisitId, request.PatientId, cancellationToken);

        var invoice = Invoice.CreateDraft(
            _org.OrganizationId,
            _org.BranchId,
            request.PatientId,
            request.MedicalVisitId,
            request.InvoiceDate,
            request.CurrencyCode,
            request.Notes,
            _user.UserId,
            _clock.UtcNow);

        if (request.InvoiceDiscountAmount is > 0)
            invoice.SetInvoiceDiscount(request.InvoiceDiscountAmount.Value, _user.UserId, _clock.UtcNow);

        if (request.Lines is { Count: > 0 })
        {
            foreach (var line in request.Lines)
                await AddResolvedLineAsync(invoice, line, cancellationToken);
        }

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken);
        await DispatchAsync(invoice, cancellationToken);
        return Map(invoice);
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await OrgInvoices().AsNoTracking()
            .Include(i => i.Lines)
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        return invoice is null ? null : Map(invoice);
    }

    public async Task<InvoiceDto?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new AppException("billing.invalid_request", "Invoice number is required.", 400);

        var invoice = await OrgInvoices().AsNoTracking()
            .Include(i => i.Lines)
            .SingleOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber.Trim(), cancellationToken);
        return invoice is null ? null : Map(invoice);
    }

    public async Task<PagedInvoicesResult> SearchAsync(SearchInvoicesRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        IQueryable<Invoice> query = OrgInvoices().AsNoTracking().Include(i => i.Lines);

        if (request.PatientId.HasValue) query = query.Where(i => i.PatientId == request.PatientId);
        if (request.MedicalVisitId.HasValue) query = query.Where(i => i.MedicalVisitId == request.MedicalVisitId);
        if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
            query = query.Where(i => i.InvoiceNumber != null && i.InvoiceNumber.Contains(request.InvoiceNumber.Trim()));
        if (request.DateFrom.HasValue) query = query.Where(i => i.InvoiceDate >= request.DateFrom);
        if (request.DateTo.HasValue) query = query.Where(i => i.InvoiceDate <= request.DateTo);
        if (TryParseStatus(request.Status, out var status)) query = query.Where(i => i.Status == status);

        query = query.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var page = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedInvoicesResult(page.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<PagedInvoicesResult> GetPatientInvoicesAsync(
        Guid patientId,
        PatientInvoicesRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePatientExistsAsync(patientId, cancellationToken);
        return await SearchAsync(new SearchInvoicesRequest(
            patientId, null, null, request.DateFrom, request.DateTo, request.Status, request.Page, request.PageSize),
            cancellationToken);
    }

    public async Task<PatientOutstandingDto> GetPatientOutstandingAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        await EnsurePatientExistsAsync(patientId, cancellationToken);
        var outstanding = await OrgInvoices().AsNoTracking()
            .Where(i => i.PatientId == patientId
                        && (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid))
            .SumAsync(i => i.OutstandingAmount, cancellationToken);

        return new PatientOutstandingDto(patientId, outstanding, "EGP");
    }

    public async Task<InvoiceDto> UpdateDraftAsync(Guid id, UpdateDraftInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("billing.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var invoice = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(invoice, request.RowVersion);
        try
        {
            invoice.UpdateDraftHeader(request.InvoiceDate, request.Notes, _user.UserId, _clock.UtcNow);
            if (request.InvoiceDiscountAmount.HasValue)
                invoice.SetInvoiceDiscount(request.InvoiceDiscountAmount.Value, _user.UserId, _clock.UtcNow);

            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(invoice, cancellationToken);
            return Map(invoice);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("billing.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("billing.concurrency_conflict", "Invoice was modified by another operation.", 409);
        }
    }

    public async Task<InvoiceDto> AddLineAsync(Guid id, AddInvoiceLineRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _addLineValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("billing.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var invoice = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(invoice, request.RowVersion);
        try
        {
            await AddResolvedLineAsync(invoice, request.Line, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(invoice, cancellationToken);
            return Map(invoice);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("billing.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("billing.concurrency_conflict", "Invoice was modified by another operation.", 409);
        }
    }

    public async Task<InvoiceDto> UpdateLineAsync(
        Guid id,
        Guid lineId,
        UpdateInvoiceLineRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateLineValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("billing.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var invoice = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(invoice, request.RowVersion);
        try
        {
            invoice.UpdateLine(lineId, request.Quantity, request.LineDiscountAmount, request.SortOrder, _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(invoice, cancellationToken);
            return Map(invoice);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("billing.invalid_transition", ex.Message, 400);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("billing.concurrency_conflict", "Invoice was modified by another operation.", 409);
        }
    }

    public async Task<InvoiceDto> RemoveLineAsync(Guid id, Guid lineId, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        var invoice = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(invoice, rowVersion);
        try
        {
            var line = invoice.Lines.SingleOrDefault(l => l.Id == lineId);
            invoice.RemoveLine(lineId, _user.UserId, _clock.UtcNow);
            if (line is not null)
                _db.InvoiceLines.Remove(line);

            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(invoice, cancellationToken);
            return Map(invoice);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("billing.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("billing.concurrency_conflict", "Invoice was modified by another operation.", 409);
        }
    }

    public async Task<InvoiceDto> IssueAsync(Guid id, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        var invoice = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(invoice, rowVersion);
        try
        {
            var number = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
            invoice.Issue(number, _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(invoice, cancellationToken);
            await _ar.OnInvoiceIssuedAsync(invoice, cancellationToken);
            return Map(invoice);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("without lines", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("billing.empty_invoice", ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("billing.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("billing.concurrency_conflict", "Invoice was modified by another operation.", 409);
        }
    }

    public async Task<InvoiceDto> VoidAsync(Guid id, VoidInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _voidValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("billing.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var invoice = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(invoice, request.RowVersion);
        try
        {
            invoice.Void(request.Reason, _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(invoice, cancellationToken);
            await _ar.OnInvoiceVoidedAsync(invoice, cancellationToken);
            return Map(invoice);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("billing.invalid_transition", ex.Message, 400);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("billing.concurrency_conflict", "Invoice was modified by another operation.", 409);
        }
    }

    private async Task AddResolvedLineAsync(Invoice invoice, InvoiceLineInput input, CancellationToken cancellationToken)
    {
        if (input.ServiceId.HasValue)
        {
            var svc = await _services.GetServiceAsync(input.ServiceId.Value, cancellationToken)
                ?? throw new AppException("services.not_found", "Service was not found.", 404);
            if (!svc.IsActive)
                throw new AppException("services.inactive", "Inactive service cannot be added to an invoice.", 400);

            var discount = input.LineDiscountAmount ?? 0;
            invoice.AddServiceLine(
                svc.Id,
                svc.CurrentPriceId,
                svc.Name,
                svc.ServiceCode,
                input.Quantity,
                svc.CurrentPrice,
                discount,
                svc.CurrencyCode,
                input.SortOrder,
                _user.UserId,
                _clock.UtcNow);
            return;
        }

        if (!input.PackageId.HasValue)
            throw new AppException("billing.invalid_line", "Line must specify a service or package.", 400);

        var package = await _packages.GetPackageAsync(input.PackageId.Value, cancellationToken)
            ?? throw new AppException("packages.not_found", "Package was not found.", 404);
        if (!package.IsActive)
            throw new AppException("packages.inactive", "Inactive package cannot be added to an invoice.", 400);

        var items = await _packages.GetPackageItemsAsync(package.Id, cancellationToken);
        if (items.Count == 0)
            throw new AppException("packages.empty", "Package has no billable items.", 400);

        foreach (var item in items)
        {
            var svc = await _services.GetServiceAsync(item.ServiceId, cancellationToken)
                ?? throw new AppException("services.not_found", "Package references a missing service.", 404);
            if (!svc.IsActive)
                throw new AppException("services.inactive", "Package contains an inactive service.", 400);

            var qty = item.Quantity * input.Quantity;
            var description = $"{package.Name}: {item.ServiceName}";
            invoice.AddPackageItemLine(
                svc.Id,
                package.Id,
                svc.CurrentPriceId,
                description,
                item.ServiceCode,
                qty,
                item.UnitPrice,
                0,
                svc.CurrencyCode,
                input.SortOrder,
                _user.UserId,
                _clock.UtcNow);
        }
    }

    private async Task EnsurePatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetAsync(patientId, cancellationToken);
        if (patient is null || patient.OrganizationId != _org.OrganizationId)
            throw new AppException("patients.not_found", "Patient was not found.", 404);
        if (!patient.IsActive)
            throw new AppException("patients.inactive", "Patient must be active.", 400);
    }

    private async Task EnsurePatientExistsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetAsync(patientId, cancellationToken);
        if (patient is null || patient.OrganizationId != _org.OrganizationId)
            throw new AppException("patients.not_found", "Patient was not found.", 404);
    }

    private async Task EnsureVisitAsync(Guid? medicalVisitId, Guid patientId, CancellationToken cancellationToken)
    {
        if (medicalVisitId is not Guid visitId)
            return;

        var visit = await _visits.GetAsync(visitId, cancellationToken)
            ?? throw new AppException("medical_visits.not_found", "Medical visit was not found.", 404);

        if (visit.OrganizationId != _org.OrganizationId)
            throw new AppException("medical_visits.not_found", "Medical visit was not found.", 404);

        if (visit.PatientId != patientId)
            throw new AppException("billing.visit_patient_mismatch", "Medical visit does not belong to the patient.", 400);
    }

    private IQueryable<Invoice> OrgInvoices() =>
        _db.Invoices.Where(i => i.OrganizationId == _org.OrganizationId);

    private async Task<Invoice> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await OrgInvoices()
            .Include(i => i.Lines)
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
            throw new AppException("billing.invoice_not_found", "Invoice was not found.", 404);
        return invoice;
    }

    private void ApplyRowVersion(Invoice invoice, byte[]? rowVersion)
    {
        if (rowVersion is { Length: > 0 })
            _db.Entry(invoice).Property(i => i.RowVersion).OriginalValue = rowVersion;
    }

    private async Task DispatchAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var events = invoice.DomainEvents.ToList();
        invoice.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static bool TryParseStatus(string? value, out InvoiceStatus status) =>
        Enum.TryParse(value, true, out status);

    private static InvoiceDto Map(Invoice i) =>
        new(
            i.Id, i.OrganizationId, i.BranchId, i.InvoiceNumber, i.PatientId, i.MedicalVisitId,
            i.InvoiceDate, i.Status.ToString(), i.CurrencyCode, i.SubTotal, i.DiscountAmount, i.TaxAmount,
            i.TotalAmount, i.PaidAmount, i.OutstandingAmount, i.Notes, i.IssuedAtUtc, i.IssuedBy,
            i.VoidedAtUtc, i.VoidedBy, i.VoidReason, i.CreatedAtUtc, i.CreatedBy, i.UpdatedAtUtc, i.UpdatedBy,
            i.RowVersion,
            i.Lines.OrderBy(l => l.SortOrder).Select(l => new InvoiceLineDto(
                l.Id, l.Source.ToString(), l.ServiceId, l.PackageId, l.ServicePriceId,
                l.DescriptionSnapshot, l.ServiceCodeSnapshot, l.Quantity, l.UnitPrice,
                l.LineDiscountAmount, l.LineSubtotal, l.LineTotal, l.CurrencyCode, l.SortOrder)).ToList());
}
