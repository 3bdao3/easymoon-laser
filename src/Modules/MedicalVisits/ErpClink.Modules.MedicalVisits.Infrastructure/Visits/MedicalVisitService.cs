using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Appointments.Application.Contracts;
using ErpClink.Modules.MedicalVisits.Application.Common;
using ErpClink.Modules.MedicalVisits.Application.Visits;
using ErpClink.Modules.MedicalVisits.Application.Visits.Models;
using ErpClink.Modules.MedicalVisits.Domain.Visits;
using ErpClink.Modules.MedicalVisits.Infrastructure.Persistence;
using ErpClink.Modules.Patients.Application.Contracts;
using ErpClink.Modules.Queue.Application.Contracts;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.MedicalVisits.Infrastructure.Visits;

public sealed class MedicalVisitService : IMedicalVisitService
{
    private readonly MedicalVisitsDbContext _db;
    private readonly IVisitNumberGenerator _numbers;
    private readonly IAppointmentLookup _appointments;
    private readonly IQueueVisitPort _queue;
    private readonly IPatientLookup _patients;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IMedicalVisitsDomainEventDispatcher _events;
    private readonly IValidator<StartMedicalVisitRequest> _startValidator;
    private readonly IValidator<UpdateClinicalNotesRequest> _notesValidator;
    private readonly IValidator<CancelMedicalVisitRequest> _cancelValidator;

    public MedicalVisitService(
        MedicalVisitsDbContext db,
        IVisitNumberGenerator numbers,
        IAppointmentLookup appointments,
        IQueueVisitPort queue,
        IPatientLookup patients,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IMedicalVisitsDomainEventDispatcher events,
        IValidator<StartMedicalVisitRequest> startValidator,
        IValidator<UpdateClinicalNotesRequest> notesValidator,
        IValidator<CancelMedicalVisitRequest> cancelValidator)
    {
        _db = db;
        _numbers = numbers;
        _appointments = appointments;
        _queue = queue;
        _patients = patients;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _startValidator = startValidator;
        _notesValidator = notesValidator;
        _cancelValidator = cancelValidator;
    }

    public async Task<MedicalVisitDto> StartAsync(
        StartMedicalVisitRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _startValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("medical_visits.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var appointment = await _appointments.GetAsync(request.AppointmentId, cancellationToken)
            ?? throw new AppException("appointments.not_found", "Appointment was not found.", 404);

        if (appointment.OrganizationId != _org.OrganizationId || appointment.BranchId != _org.BranchId)
            throw new AppException("appointments.not_found", "Appointment was not found.", 404);

        if (!string.Equals(appointment.Status, "CheckedIn", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(
                "medical_visits.appointment_not_checked_in",
                $"Appointment status {appointment.Status} is not eligible. CheckedIn is required.",
                400);
        }

        var patient = await _patients.GetAsync(appointment.PatientId, cancellationToken)
            ?? throw new AppException("patients.not_found", "Patient was not found.", 404);
        if (patient.OrganizationId != _org.OrganizationId)
            throw new AppException("patients.not_found", "Patient was not found.", 404);

        var queue = await ResolveQueueAsync(request.QueueEntryId, appointment.Id, appointment.PatientId, cancellationToken);

        // Avoid ambient TransactionScope across module DbContexts (would require MSDTC).
        // Queue InService then visit create; unique index still protects duplicate visits.
        try
        {
            if (queue is not null)
                await _queue.EnsureInServiceAsync(queue.Id, cancellationToken);

            var number = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
            var visit = MedicalVisit.Start(
                _org.OrganizationId,
                _org.BranchId,
                number,
                appointment.PatientId,
                appointment.Id,
                appointment.DoctorId,
                appointment.ClinicId,
                queue?.Id,
                appointment.AppointmentDate,
                _user.UserId,
                _clock.UtcNow);

            _db.MedicalVisits.Add(visit);
            await _db.SaveChangesAsync(cancellationToken);

            await DispatchAsync(visit, cancellationToken);
            return Map(visit);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new AppException(
                "medical_visits.duplicate_visit",
                "An active medical visit already exists for this appointment.",
                409);
        }
    }

    public async Task<MedicalVisitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var visit = await OrgVisits().AsNoTracking().SingleOrDefaultAsync(v => v.Id == id, cancellationToken);
        return visit is null ? null : Map(visit);
    }

    public async Task<MedicalVisitDto?> GetByNumberAsync(string visitNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(visitNumber))
            throw new AppException("medical_visits.invalid_request", "Visit number is required.", 400);

        var visit = await OrgVisits().AsNoTracking()
            .SingleOrDefaultAsync(v => v.VisitNumber == visitNumber.Trim(), cancellationToken);
        return visit is null ? null : Map(visit);
    }

    public async Task<PagedMedicalVisitsResult> GetPatientHistoryAsync(
        Guid patientId,
        PatientHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await _patients.GetAsync(patientId, cancellationToken);
        if (patient is null || patient.OrganizationId != _org.OrganizationId)
            throw new AppException("patients.not_found", "Patient was not found.", 404);

        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgVisits().AsNoTracking().Where(v => v.PatientId == patientId);

        if (request.DateFrom.HasValue)
            query = query.Where(v => v.VisitDate >= request.DateFrom);
        if (request.DateTo.HasValue)
            query = query.Where(v => v.VisitDate <= request.DateTo);
        if (request.DoctorId.HasValue)
            query = query.Where(v => v.DoctorId == request.DoctorId);
        if (request.ClinicId.HasValue)
            query = query.Where(v => v.ClinicId == request.ClinicId);
        if (TryParseStatus(request.Status, out var status))
            query = query.Where(v => v.Status == status);

        query = query.OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.CreatedAtUtc);

        var total = await query.CountAsync(cancellationToken);
        var pageItems = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedMedicalVisitsResult(pageItems.Select(ToListItem).ToList(), paging.NormalizedPage, paging.NormalizedPageSize, total);
    }

    public async Task<PatientVisitContextDto> GetPatientVisitContextAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patient = await _patients.GetAsync(patientId, cancellationToken);
        if (patient is null || patient.OrganizationId != _org.OrganizationId)
            throw new AppException("patients.not_found", "Patient was not found.", 404);

        var visits = await OrgVisits().AsNoTracking()
            .Where(v => v.PatientId == patientId)
            .ToListAsync(cancellationToken);

        return PatientVisitContextFactory.FromVisits(visits);
    }

    public async Task<PagedMedicalVisitsResult> SearchAsync(
        SearchMedicalVisitsRequest request,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgVisits().AsNoTracking();

        if (request.PatientId.HasValue)
            query = query.Where(v => v.PatientId == request.PatientId);
        if (request.DoctorId.HasValue)
            query = query.Where(v => v.DoctorId == request.DoctorId);
        if (request.ClinicId.HasValue)
            query = query.Where(v => v.ClinicId == request.ClinicId);
        if (request.AppointmentId.HasValue)
            query = query.Where(v => v.AppointmentId == request.AppointmentId);
        if (request.VisitDate.HasValue)
            query = query.Where(v => v.VisitDate == request.VisitDate);
        if (request.DateFrom.HasValue)
            query = query.Where(v => v.VisitDate >= request.DateFrom);
        if (request.DateTo.HasValue)
            query = query.Where(v => v.VisitDate <= request.DateTo);
        if (!string.IsNullOrWhiteSpace(request.VisitNumber))
            query = query.Where(v => v.VisitNumber.Contains(request.VisitNumber.Trim()));
        if (TryParseStatus(request.Status, out var status))
            query = query.Where(v => v.Status == status);

        query = query.OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.CreatedAtUtc);

        var total = await query.CountAsync(cancellationToken);
        var pageItems = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedMedicalVisitsResult(pageItems.Select(ToListItem).ToList(), paging.NormalizedPage, paging.NormalizedPageSize, total);
    }

    public async Task<MedicalVisitDto> UpdateClinicalNotesAsync(
        Guid id,
        UpdateClinicalNotesRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _notesValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("medical_visits.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var visit = await GetRequiredAsync(id, cancellationToken);

        if (request.RowVersion is { Length: > 0 })
            _db.Entry(visit).Property(v => v.RowVersion).OriginalValue = request.RowVersion;

        try
        {
            visit.UpdateClinicalNotes(
                request.ChiefComplaint,
                request.ClinicalNotes,
                request.ExaminationFindings,
                request.DiagnosisNotes,
                request.FollowUpNotes,
                _user.UserId,
                _clock.UtcNow);

            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(visit, cancellationToken);
            return Map(visit);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("medical_visits.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("medical_visits.concurrency_conflict", "Medical visit was modified by another operation.", 409);
        }
    }

    public async Task CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var visit = await GetRequiredAsync(id, cancellationToken);
        try
        {
            visit.Complete(_user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(visit, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("medical_visits.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("medical_visits.concurrency_conflict", "Medical visit was modified by another operation.", 409);
        }
    }

    public async Task CancelAsync(Guid id, CancelMedicalVisitRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("medical_visits.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var visit = await GetRequiredAsync(id, cancellationToken);
        try
        {
            visit.Cancel(request.Reason, _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(visit, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("medical_visits.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("medical_visits.concurrency_conflict", "Medical visit was modified by another operation.", 409);
        }
    }

    private async Task<QueueVisitLookupResult?> ResolveQueueAsync(
        Guid? queueEntryId,
        Guid appointmentId,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        QueueVisitLookupResult? queue;
        if (queueEntryId.HasValue)
        {
            queue = await _queue.GetByIdAsync(queueEntryId.Value, cancellationToken)
                ?? throw new AppException("queue.not_found", "Queue entry was not found.", 404);
        }
        else
        {
            queue = await _queue.GetActiveByAppointmentAsync(appointmentId, cancellationToken);
            if (queue is null)
            {
                throw new AppException(
                    "medical_visits.queue_required",
                    "An active queue entry is required to start a medical visit.",
                    400);
            }
        }

        if (queue.OrganizationId != _org.OrganizationId || queue.BranchId != _org.BranchId)
            throw new AppException("queue.not_found", "Queue entry was not found.", 404);

        if (queue.AppointmentId != appointmentId)
        {
            throw new AppException(
                "medical_visits.queue_mismatch",
                "Queue entry does not belong to the appointment.",
                400);
        }

        if (queue.PatientId != patientId)
        {
            throw new AppException(
                "medical_visits.patient_mismatch",
                "Queue entry patient does not match the appointment.",
                400);
        }

        if (!string.Equals(queue.Status, "Waiting", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(queue.Status, "Called", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(queue.Status, "InService", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(
                "medical_visits.queue_not_eligible",
                $"Queue status {queue.Status} is not eligible for starting a medical visit.",
                400);
        }

        return queue;
    }

    private IQueryable<MedicalVisit> OrgVisits() =>
        _db.MedicalVisits.Where(v => v.OrganizationId == _org.OrganizationId);

    private async Task<MedicalVisit> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var visit = await OrgVisits().SingleOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (visit is null)
            throw new AppException("medical_visits.not_found", "Medical visit was not found.", 404);
        return visit;
    }

    private async Task DispatchAsync(MedicalVisit visit, CancellationToken cancellationToken)
    {
        var events = visit.DomainEvents.ToList();
        visit.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static bool TryParseStatus(string? value, out MedicalVisitStatus status) =>
        Enum.TryParse(value, true, out status);

    private static MedicalVisitDto Map(MedicalVisit v) =>
        new(v.Id, v.OrganizationId, v.BranchId, v.VisitNumber, v.PatientId, v.AppointmentId,
            v.DoctorId, v.ClinicId, v.QueueEntryId, v.VisitDate, v.Status.ToString(),
            v.ChiefComplaint, v.ClinicalNotes, v.ExaminationFindings, v.DiagnosisNotes, v.FollowUpNotes,
            v.CompletedAtUtc, v.CompletedBy, v.CancelledAtUtc, v.CancelledBy, v.CancellationReason,
            v.CreatedAtUtc, v.CreatedBy, v.UpdatedAtUtc, v.UpdatedBy, v.RowVersion);

    private static MedicalVisitListItemDto ToListItem(MedicalVisit v) =>
        new(v.Id, v.VisitNumber, v.VisitDate, v.PatientId, v.DoctorId, v.ClinicId, v.AppointmentId,
            v.Status.ToString(), v.ChiefComplaint, v.DiagnosisNotes, v.FollowUpNotes);

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is not SqlException sql) return false;
        if (sql.Number is not (2601 or 2627)) return false;
        return sql.Message.Contains("IX_MedicalVisits_ActiveAppointment", StringComparison.OrdinalIgnoreCase)
               || sql.Message.Contains("VisitNumber", StringComparison.OrdinalIgnoreCase)
               || sql.Message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }
}
