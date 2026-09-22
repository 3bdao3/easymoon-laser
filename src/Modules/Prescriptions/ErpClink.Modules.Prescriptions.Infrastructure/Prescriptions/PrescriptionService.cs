using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.MedicalVisits.Application.Contracts;
using ErpClink.Modules.Patients.Application.Contracts;
using ErpClink.Modules.Prescriptions.Application.Common;
using ErpClink.Modules.Prescriptions.Application.Prescriptions;
using ErpClink.Modules.Prescriptions.Application.Prescriptions.Models;
using ErpClink.Modules.Prescriptions.Domain.Medications;
using ErpClink.Modules.Prescriptions.Domain.Prescriptions;
using ErpClink.Modules.Prescriptions.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Prescriptions.Infrastructure.Prescriptions;

public sealed class PrescriptionService : IPrescriptionService
{
    private readonly PrescriptionsDbContext _db;
    private readonly IPrescriptionNumberGenerator _numbers;
    private readonly IMedicalVisitPrescriptionPort _visits;
    private readonly IPatientLookup _patients;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IPrescriptionsDomainEventDispatcher _events;
    private readonly IValidator<CreatePrescriptionRequest> _createValidator;
    private readonly IValidator<UpdatePrescriptionRequest> _updateValidator;
    private readonly IValidator<PrescriptionItemInput> _itemValidator;
    private readonly IValidator<UpdatePrescriptionItemRequest> _updateItemValidator;
    private readonly IValidator<CancelPrescriptionRequest> _cancelValidator;

    public PrescriptionService(
        PrescriptionsDbContext db,
        IPrescriptionNumberGenerator numbers,
        IMedicalVisitPrescriptionPort visits,
        IPatientLookup patients,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IPrescriptionsDomainEventDispatcher events,
        IValidator<CreatePrescriptionRequest> createValidator,
        IValidator<UpdatePrescriptionRequest> updateValidator,
        IValidator<PrescriptionItemInput> itemValidator,
        IValidator<UpdatePrescriptionItemRequest> updateItemValidator,
        IValidator<CancelPrescriptionRequest> cancelValidator)
    {
        _db = db;
        _numbers = numbers;
        _visits = visits;
        _patients = patients;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _itemValidator = itemValidator;
        _updateItemValidator = updateItemValidator;
        _cancelValidator = cancelValidator;
    }

    public async Task<PrescriptionDto> CreateAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("prescriptions.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var visit = await _visits.GetAsync(request.MedicalVisitId, cancellationToken)
            ?? throw new AppException("medical_visits.not_found", "Medical visit was not found.", 404);

        if (visit.OrganizationId != _org.OrganizationId || visit.BranchId != _org.BranchId)
            throw new AppException("medical_visits.not_found", "Medical visit was not found.", 404);

        if (string.Equals(visit.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            throw new AppException("prescriptions.visit_not_eligible", "Cannot create a prescription for a cancelled visit.", 400);

        var patient = await _patients.GetAsync(visit.PatientId, cancellationToken);
        if (patient is null || patient.OrganizationId != _org.OrganizationId)
            throw new AppException("patients.not_found", "Patient was not found.", 404);

        try
        {
            var number = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
            var prescription = Prescription.CreateDraft(
                _org.OrganizationId, _org.BranchId, number, visit.Id, visit.PatientId, visit.DoctorId,
                visit.VisitDate, request.Notes, _user.UserId, _clock.UtcNow);

            if (request.Items is { Count: > 0 })
            {
                foreach (var item in request.Items)
                {
                    var med = await ResolveActiveMedicationAsync(item.MedicationId, cancellationToken);
                    prescription.AddItem(
                        med.Id, BuildSnapshot(med), item.Dosage, item.Frequency, item.Duration,
                        item.Route ?? med.Route, item.Instructions, item.Quantity, item.Notes,
                        item.SortOrder, _user.UserId, _clock.UtcNow);
                }
            }

            _db.Prescriptions.Add(prescription);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(prescription, cancellationToken);
            return Map(prescription);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new AppException(
                "prescriptions.duplicate_prescription",
                "An active prescription already exists for this medical visit.",
                409);
        }
    }

    public async Task<PrescriptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var prescription = await OrgPrescriptions().AsNoTracking()
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        return prescription is null ? null : Map(prescription);
    }

    public async Task<PrescriptionDto?> GetByNumberAsync(string prescriptionNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prescriptionNumber))
            throw new AppException("prescriptions.invalid_request", "Prescription number is required.", 400);

        var prescription = await OrgPrescriptions().AsNoTracking()
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.PrescriptionNumber == prescriptionNumber.Trim(), cancellationToken);
        return prescription is null ? null : Map(prescription);
    }

    public async Task<PagedPrescriptionsResult> SearchAsync(SearchPrescriptionsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgPrescriptions().AsNoTracking();

        if (request.PatientId.HasValue) query = query.Where(p => p.PatientId == request.PatientId);
        if (request.DoctorId.HasValue) query = query.Where(p => p.DoctorId == request.DoctorId);
        if (request.MedicalVisitId.HasValue) query = query.Where(p => p.MedicalVisitId == request.MedicalVisitId);
        if (!string.IsNullOrWhiteSpace(request.PrescriptionNumber))
            query = query.Where(p => p.PrescriptionNumber.Contains(request.PrescriptionNumber.Trim()));
        if (request.DateFrom.HasValue) query = query.Where(p => p.PrescriptionDate >= request.DateFrom);
        if (request.DateTo.HasValue) query = query.Where(p => p.PrescriptionDate <= request.DateTo);
        if (TryParseStatus(request.Status, out var status)) query = query.Where(p => p.Status == status);

        query = query.OrderByDescending(p => p.PrescriptionDate).ThenByDescending(p => p.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var page = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize)
            .Select(p => new PrescriptionListItemDto(
                p.Id, p.PrescriptionNumber, p.PrescriptionDate, p.PatientId, p.DoctorId, p.MedicalVisitId,
                p.Status.ToString(), p.Items.Count))
            .ToListAsync(cancellationToken);

        return new PagedPrescriptionsResult(page, paging.NormalizedPage, paging.NormalizedPageSize, total);
    }

    public async Task<PagedPrescriptionsResult> GetPatientHistoryAsync(
        Guid patientId,
        PatientPrescriptionHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await _patients.GetAsync(patientId, cancellationToken);
        if (patient is null || patient.OrganizationId != _org.OrganizationId)
            throw new AppException("patients.not_found", "Patient was not found.", 404);

        return await SearchAsync(new SearchPrescriptionsRequest(
            patientId, null, null, null, request.DateFrom, request.DateTo, request.Status,
            request.Page, request.PageSize), cancellationToken);
    }

    public async Task<PrescriptionDto> UpdateAsync(Guid id, UpdatePrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("prescriptions.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var prescription = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(prescription, request.RowVersion);
        try
        {
            prescription.UpdateNotes(request.Notes, _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(prescription, cancellationToken);
            return Map(prescription);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("prescriptions.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("prescriptions.concurrency_conflict", "Prescription was modified by another operation.", 409);
        }
    }

    public async Task<PrescriptionDto> AddItemAsync(Guid id, PrescriptionItemInput request, CancellationToken cancellationToken = default)
    {
        var validation = await _itemValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("prescriptions.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var prescription = await GetRequiredAsync(id, cancellationToken);
        var med = await ResolveActiveMedicationAsync(request.MedicationId, cancellationToken);
        try
        {
            var item = prescription.AddItem(
                med.Id, BuildSnapshot(med), request.Dosage, request.Frequency, request.Duration,
                request.Route ?? med.Route, request.Instructions, request.Quantity, request.Notes,
                request.SortOrder, _user.UserId, _clock.UtcNow);
            _db.PrescriptionItems.Add(item);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(prescription, cancellationToken);
            return Map(prescription);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("prescriptions.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("prescriptions.concurrency_conflict", "Prescription was modified by another operation.", 409);
        }
    }

    public async Task<PrescriptionDto> UpdateItemAsync(
        Guid id,
        Guid itemId,
        UpdatePrescriptionItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateItemValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("prescriptions.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var prescription = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(prescription, request.RowVersion);
        try
        {
            prescription.UpdateItem(
                itemId, request.Dosage, request.Frequency, request.Duration, request.Route,
                request.Instructions, request.Quantity, request.Notes, request.SortOrder,
                _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(prescription, cancellationToken);
            return Map(prescription);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("prescriptions.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("prescriptions.concurrency_conflict", "Prescription was modified by another operation.", 409);
        }
    }

    public async Task<PrescriptionDto> RemoveItemAsync(
        Guid id,
        Guid itemId,
        byte[]? rowVersion,
        CancellationToken cancellationToken = default)
    {
        var prescription = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(prescription, rowVersion);
        try
        {
            var item = prescription.Items.SingleOrDefault(i => i.Id == itemId);
            prescription.RemoveItem(itemId, _user.UserId, _clock.UtcNow);
            if (item is not null)
                _db.PrescriptionItems.Remove(item);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(prescription, cancellationToken);
            return Map(prescription);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("prescriptions.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("prescriptions.concurrency_conflict", "Prescription was modified by another operation.", 409);
        }
    }

    public async Task IssueAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var prescription = await GetRequiredAsync(id, cancellationToken);
        foreach (var item in prescription.Items)
        {
            var med = await _db.Medications.AsNoTracking()
                .SingleOrDefaultAsync(m => m.Id == item.MedicationId && m.OrganizationId == _org.OrganizationId, cancellationToken);
            if (med is null || !med.IsActive)
                throw new AppException("prescriptions.inactive_medication", "All medications must be active to issue.", 400);
        }

        try
        {
            prescription.Issue(_user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(prescription, cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("without items", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("prescriptions.empty_prescription", ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("prescriptions.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("prescriptions.concurrency_conflict", "Prescription was modified by another operation.", 409);
        }
    }

    public async Task CancelAsync(Guid id, CancelPrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("prescriptions.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var prescription = await GetRequiredAsync(id, cancellationToken);
        try
        {
            prescription.Cancel(request.Reason, _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(prescription, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("prescriptions.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("prescriptions.concurrency_conflict", "Prescription was modified by another operation.", 409);
        }
    }

    private async Task<Medication> ResolveActiveMedicationAsync(Guid medicationId, CancellationToken cancellationToken)
    {
        var med = await _db.Medications.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == medicationId && m.OrganizationId == _org.OrganizationId, cancellationToken)
            ?? throw new AppException("medications.not_found", "Medication was not found.", 404);

        if (!med.IsActive)
            throw new AppException("medications.inactive", "Inactive medication cannot be prescribed.", 400);

        return med;
    }

    private static string BuildSnapshot(Medication med)
    {
        var parts = new List<string> { med.Name };
        if (!string.IsNullOrWhiteSpace(med.Strength)) parts.Add(med.Strength);
        if (!string.IsNullOrWhiteSpace(med.DosageForm)) parts.Add(med.DosageForm);
        return string.Join(" ", parts);
    }

    private IQueryable<Prescription> OrgPrescriptions() =>
        _db.Prescriptions.Where(p => p.OrganizationId == _org.OrganizationId);

    private async Task<Prescription> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var prescription = await OrgPrescriptions()
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (prescription is null)
            throw new AppException("prescriptions.not_found", "Prescription was not found.", 404);
        return prescription;
    }

    private void ApplyRowVersion(Prescription prescription, byte[]? rowVersion)
    {
        if (rowVersion is { Length: > 0 })
            _db.Entry(prescription).Property(p => p.RowVersion).OriginalValue = rowVersion;
    }

    private async Task DispatchAsync(Prescription prescription, CancellationToken cancellationToken)
    {
        var events = prescription.DomainEvents.ToList();
        prescription.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static bool TryParseStatus(string? value, out PrescriptionStatus status) =>
        Enum.TryParse(value, true, out status);

    private static PrescriptionDto Map(Prescription p) =>
        new(p.Id, p.OrganizationId, p.BranchId, p.PrescriptionNumber, p.MedicalVisitId, p.PatientId, p.DoctorId,
            p.PrescriptionDate, p.Status.ToString(), p.Notes, p.IssuedAtUtc, p.IssuedBy,
            p.CancelledAtUtc, p.CancelledBy, p.CancellationReason, p.CreatedAtUtc, p.CreatedBy,
            p.UpdatedAtUtc, p.UpdatedBy, p.RowVersion,
            p.Items.OrderBy(i => i.SortOrder).Select(i => new PrescriptionItemDto(
                i.Id, i.MedicationId, i.MedicationNameSnapshot, i.Dosage, i.Frequency, i.Duration,
                i.Route, i.Instructions, i.Quantity, i.Notes, i.SortOrder)).ToList());

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is not SqlException sql) return false;
        if (sql.Number is not (2601 or 2627)) return false;
        return sql.Message.Contains("IX_Prescriptions_ActiveMedicalVisit", StringComparison.OrdinalIgnoreCase)
               || sql.Message.Contains("PrescriptionNumber", StringComparison.OrdinalIgnoreCase)
               || sql.Message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }
}
