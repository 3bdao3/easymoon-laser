using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Appointments.Application.Appointments;
using ErpClink.Modules.Appointments.Application.Appointments.Models;
using ErpClink.Modules.Appointments.Application.Common;
using ErpClink.Modules.Appointments.Domain.Appointments;
using ErpClink.Modules.Appointments.Infrastructure.Persistence;
using ErpClink.Modules.Doctors.Application.Contracts;
using ErpClink.Modules.Patients.Application.Contracts;
using ErpClink.Modules.Scheduling.Application.Schedules;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Appointments.Infrastructure.Appointments;

public sealed class AppointmentService : IAppointmentService
{
    private readonly AppointmentsDbContext _db;
    private readonly IAppointmentNumberGenerator _numbers;
    private readonly IScheduleAvailabilityService _availability;
    private readonly IDoctorClinicLookup _doctorClinic;
    private readonly IPatientLookup _patients;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _businessClock;
    private readonly IAppointmentsDomainEventDispatcher _events;
    private readonly IValidator<BookAppointmentRequest> _bookValidator;
    private readonly IValidator<RescheduleAppointmentRequest> _rescheduleValidator;
    private readonly IValidator<CancelAppointmentRequest> _cancelValidator;

    public AppointmentService(
        AppointmentsDbContext db,
        IAppointmentNumberGenerator numbers,
        IScheduleAvailabilityService availability,
        IDoctorClinicLookup doctorClinic,
        IPatientLookup patients,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock businessClock,
        IAppointmentsDomainEventDispatcher events,
        IValidator<BookAppointmentRequest> bookValidator,
        IValidator<RescheduleAppointmentRequest> rescheduleValidator,
        IValidator<CancelAppointmentRequest> cancelValidator)
    {
        _db = db;
        _numbers = numbers;
        _availability = availability;
        _doctorClinic = doctorClinic;
        _patients = patients;
        _org = org;
        _user = user;
        _businessClock = businessClock;
        _events = events;
        _bookValidator = bookValidator;
        _rescheduleValidator = rescheduleValidator;
        _cancelValidator = cancelValidator;
    }

    public async Task<AppointmentDto> BookAsync(BookAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_bookValidator, request, cancellationToken);

        var endTime = await ValidateSlotAndReferencesAsync(
            request.PatientId,
            request.DoctorId,
            request.ClinicId,
            request.Date,
            request.StartTime,
            excludeAppointmentId: null,
            cancellationToken);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var number = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
            var appointment = Appointment.Book(
                _org.OrganizationId,
                _org.BranchId,
                number,
                request.PatientId,
                request.DoctorId,
                request.ClinicId,
                request.Date,
                request.StartTime,
                endTime,
                request.Reason,
                request.Notes,
                _user.UserId,
                _businessClock.UtcNow);

            _db.Appointments.Add(appointment);
            foreach (var entry in appointment.StatusHistory)
                _db.AppointmentStatusHistory.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            await DispatchAsync(appointment, cancellationToken);
            return Map(appointment);
        }
        catch (DbUpdateException ex) when (IsUniqueSlotViolation(ex))
        {
            await tx.RollbackAsync(cancellationToken);
            throw new AppException("appointments.slot_conflict", "This time slot is already booked.", 409);
        }
    }

    public async Task<AppointmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await OrgAppointments().AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        return appointment is null ? null : Map(appointment);
    }

    public async Task<AppointmentDto?> GetByNumberAsync(string appointmentNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appointmentNumber))
            throw new AppException("appointments.invalid_request", "Appointment number is required.", 400);

        var appointment = await OrgAppointments().AsNoTracking()
            .SingleOrDefaultAsync(a => a.AppointmentNumber == appointmentNumber.Trim(), cancellationToken);
        return appointment is null ? null : Map(appointment);
    }

    public async Task<PagedAppointmentsResult> SearchAsync(
        SearchAppointmentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgAppointments().AsNoTracking();

        if (request.PatientId.HasValue)
            query = query.Where(a => a.PatientId == request.PatientId);
        if (request.DoctorId.HasValue)
            query = query.Where(a => a.DoctorId == request.DoctorId);
        if (request.ClinicId.HasValue)
            query = query.Where(a => a.ClinicId == request.ClinicId);
        if (request.Date.HasValue)
            query = query.Where(a => a.AppointmentDate == request.Date);
        if (request.FromDate.HasValue)
            query = query.Where(a => a.AppointmentDate >= request.FromDate);
        if (request.ToDate.HasValue)
            query = query.Where(a => a.AppointmentDate <= request.ToDate);
        if (!string.IsNullOrWhiteSpace(request.AppointmentNumber))
            query = query.Where(a => a.AppointmentNumber.Contains(request.AppointmentNumber.Trim()));
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<AppointmentStatus>(request.Status, ignoreCase: true, out var status))
            query = query.Where(a => a.Status == status);

        query = (request.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "appointmentnumber" => request.SortDescending
                ? query.OrderByDescending(a => a.AppointmentNumber)
                : query.OrderBy(a => a.AppointmentNumber),
            "status" => request.SortDescending
                ? query.OrderByDescending(a => a.Status)
                : query.OrderBy(a => a.Status),
            _ => query.OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.StartTime)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize)
            .Select(a => new AppointmentListItemDto(
                a.Id, a.AppointmentNumber, a.PatientId, a.DoctorId, a.ClinicId,
                a.AppointmentDate, a.StartTime, a.EndTime, a.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new PagedAppointmentsResult(items, paging.NormalizedPage, paging.NormalizedPageSize, total);
    }

    public async Task ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await GetRequiredAsync(id, cancellationToken);
        try
        {
            var before = appointment.StatusHistory.Count;
            appointment.Confirm(_user.UserId, _businessClock.UtcNow);
            PersistNewHistory(appointment, before);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(appointment, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("appointments.invalid_transition", ex.Message, 409);
        }
    }

    public async Task CancelAsync(Guid id, CancelAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_cancelValidator, request, cancellationToken);
        var appointment = await GetRequiredAsync(id, cancellationToken);
        try
        {
            var before = appointment.StatusHistory.Count;
            appointment.Cancel(request.Reason, _user.UserId, _businessClock.UtcNow);
            PersistNewHistory(appointment, before);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(appointment, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("appointments.invalid_transition", ex.Message, 409);
        }
    }

    public async Task RescheduleAsync(
        Guid id,
        RescheduleAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_rescheduleValidator, request, cancellationToken);
        var appointment = await GetRequiredAsync(id, cancellationToken);

        var endTime = await ValidateSlotAndReferencesAsync(
            appointment.PatientId,
            appointment.DoctorId,
            appointment.ClinicId,
            request.Date,
            request.StartTime,
            excludeAppointmentId: appointment.Id,
            cancellationToken);

        try
        {
            var before = appointment.RescheduleHistory.Count;
            appointment.Reschedule(request.Date, request.StartTime, endTime, _user.UserId, _businessClock.UtcNow);
            foreach (var entry in appointment.RescheduleHistory.Skip(before))
                _db.AppointmentRescheduleHistory.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(appointment, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("appointments.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateException ex) when (IsUniqueSlotViolation(ex))
        {
            throw new AppException("appointments.slot_conflict", "This time slot is already booked.", 409);
        }
    }

    public async Task MarkNoShowAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await GetRequiredAsync(id, cancellationToken);
        try
        {
            var before = appointment.StatusHistory.Count;
            appointment.MarkNoShow(_user.UserId, _businessClock.UtcNow);
            PersistNewHistory(appointment, before);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(appointment, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("appointments.invalid_transition", ex.Message, 409);
        }
    }

    private async Task<TimeOnly> ValidateSlotAndReferencesAsync(
        Guid patientId,
        Guid doctorId,
        Guid clinicId,
        DateOnly date,
        TimeOnly startTime,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        EnsureNotInPast(date, startTime);

        var patient = await _patients.GetAsync(patientId, cancellationToken)
            ?? throw new AppException("patients.not_found", "Patient was not found.", 404);
        if (!patient.IsActive)
            throw new AppException("patients.inactive", "Inactive patient cannot book an appointment.", 400);

        var doctor = await _doctorClinic.GetDoctorAsync(doctorId, cancellationToken)
            ?? throw new AppException("doctors.not_found", "Doctor was not found.", 404);
        if (!doctor.IsActive)
            throw new AppException("doctors.inactive", "Inactive doctor cannot receive appointments.", 400);

        var clinic = await _doctorClinic.GetClinicAsync(clinicId, cancellationToken)
            ?? throw new AppException("clinics.not_found", "Clinic was not found.", 404);
        if (!clinic.IsActive)
            throw new AppException("clinics.inactive", "Inactive clinic cannot receive appointments.", 400);

        if (patient.OrganizationId != _org.OrganizationId ||
            doctor.OrganizationId != _org.OrganizationId ||
            clinic.OrganizationId != _org.OrganizationId)
            throw new AppException("appointments.cross_organization", "Cross-organization booking is forbidden.", 403);

        var assigned = await _doctorClinic.HasActiveAssignmentAsync(doctorId, clinicId, date, cancellationToken);
        if (!assigned)
            throw new AppException(
                "appointments.assignment_required",
                "Doctor must have an active assignment to the clinic on the appointment date.",
                400);

        var availability = await _availability.GetAvailabilityAsync(doctorId, date, clinicId, cancellationToken);
        if (availability.IsHoliday)
            throw new AppException("appointments.holiday", "Cannot book on a clinic holiday.", 400);
        if (availability.IsDoctorUnavailable)
            throw new AppException("appointments.doctor_unavailable", "Doctor is unavailable on this date.", 400);

        var slot = availability.Slots.FirstOrDefault(s => s.Start == startTime);
        if (slot is null)
            throw new AppException(
                "appointments.invalid_slot",
                "Requested start time is not an available schedule slot.",
                400);

        var conflict = await OrgAppointments().AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.ClinicId == clinicId &&
            a.AppointmentDate == date &&
            a.StartTime == startTime &&
            (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.CheckedIn) &&
            (excludeAppointmentId == null || a.Id != excludeAppointmentId.Value), cancellationToken);

        if (conflict)
            throw new AppException("appointments.slot_conflict", "This time slot is already booked.", 409);

        return slot.End;
    }

    private void EnsureNotInPast(DateOnly date, TimeOnly startTime)
    {
        var today = _businessClock.Today;
        var now = _businessClock.NowLocal;
        if (date < today || (date == today && startTime < now))
            throw new AppException("appointments.past_not_allowed", "Cannot book an appointment in the past.", 400);
    }

    private IQueryable<Appointment> OrgAppointments() =>
        _db.Appointments.Where(a => a.OrganizationId == _org.OrganizationId);

    private async Task<Appointment> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await OrgAppointments().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (appointment is null)
            throw new AppException("appointments.not_found", "Appointment was not found.", 404);
        return appointment;
    }

    private void PersistNewHistory(Appointment appointment, int previousCount)
    {
        foreach (var entry in appointment.StatusHistory.Skip(previousCount))
            _db.AppointmentStatusHistory.Add(entry);
    }

    private async Task DispatchAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var events = appointment.DomainEvents.ToList();
        appointment.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static AppointmentDto Map(Appointment a) =>
        new(a.Id, a.OrganizationId, a.BranchId, a.AppointmentNumber, a.PatientId, a.DoctorId, a.ClinicId,
            a.AppointmentDate, a.StartTime, a.EndTime, a.Status.ToString(), a.Reason, a.Notes,
            a.CancellationReason, a.CancelledAtUtc, a.CancelledBy,
            a.CreatedAtUtc, a.CreatedBy, a.UpdatedAtUtc, a.UpdatedBy);

    private static bool IsUniqueSlotViolation(DbUpdateException ex)
    {
        if (ex.InnerException is not SqlException sql) return false;
        // 2601 / 2627 unique constraint
        if (sql.Number is not (2601 or 2627)) return false;
        return sql.Message.Contains("IX_Appointments_ActiveSlot", StringComparison.OrdinalIgnoreCase)
               || sql.Message.Contains("ActiveSlot", StringComparison.OrdinalIgnoreCase)
               || sql.Message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new AppException("appointments.validation_failed", string.Join(" ", result.Errors.Select(e => e.ErrorMessage)), 400);
    }
}
