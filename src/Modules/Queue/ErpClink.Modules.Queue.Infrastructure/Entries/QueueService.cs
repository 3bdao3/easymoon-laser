using System.Transactions;
using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Appointments.Application.Contracts;
using ErpClink.Modules.Queue.Application.Common;
using ErpClink.Modules.Queue.Application.Entries;
using ErpClink.Modules.Queue.Application.Entries.Models;
using ErpClink.Modules.Queue.Domain.Entries;
using ErpClink.Modules.Queue.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Queue.Infrastructure.Entries;

public sealed class QueueService : IQueueService
{
    private readonly QueueDbContext _db;
    private readonly IQueueNumberGenerator _numbers;
    private readonly IAppointmentLookup _appointments;
    private readonly IAppointmentCheckInPort _checkIn;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IQueueDomainEventDispatcher _events;
    private readonly IValidator<CheckInRequest> _checkInValidator;

    public QueueService(
        QueueDbContext db,
        IQueueNumberGenerator numbers,
        IAppointmentLookup appointments,
        IAppointmentCheckInPort checkIn,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IQueueDomainEventDispatcher events,
        IValidator<CheckInRequest> checkInValidator)
    {
        _db = db;
        _numbers = numbers;
        _appointments = appointments;
        _checkIn = checkIn;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _checkInValidator = checkInValidator;
    }

    public async Task<QueueEntryDto> CheckInAsync(CheckInRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _checkInValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("queue.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var appointment = await _appointments.GetAsync(request.AppointmentId, cancellationToken)
            ?? throw new AppException("appointments.not_found", "Appointment was not found.", 404);

        EnsureEligibleForCheckIn(appointment);

        var priority = ParsePriority(request.Priority);
        var today = _clock.Today;

        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        try
        {
            var number = await _numbers.GenerateAsync(
                _org.OrganizationId, _org.BranchId, appointment.ClinicId, today, cancellationToken);

            var entry = QueueEntry.CheckIn(
                _org.OrganizationId,
                _org.BranchId,
                number,
                appointment.PatientId,
                appointment.Id,
                appointment.DoctorId,
                appointment.ClinicId,
                today,
                priority,
                _user.UserId,
                _clock.UtcNow);

            _db.QueueEntries.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
            await _checkIn.CheckInAsync(appointment.Id, cancellationToken);
            scope.Complete();

            await DispatchAsync(entry, cancellationToken);
            return Map(entry);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new AppException("queue.duplicate_checkin", "Appointment already has an active queue entry.", 409);
        }
    }

    public async Task<QueueEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await OrgEntries().AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entry is null ? null : Map(entry);
    }

    public async Task<IReadOnlyList<QueueListItemDto>> GetTodayAsync(
        TodayQueueRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = _clock.Today;
        var query = OrgEntries().AsNoTracking().Where(e => e.QueueDate == today && e.BranchId == _org.BranchId);

        if (request.ClinicId.HasValue)
            query = query.Where(e => e.ClinicId == request.ClinicId);
        if (request.DoctorId.HasValue)
            query = query.Where(e => e.DoctorId == request.DoctorId);
        if (TryParseStatus(request.Status, out var status))
            query = query.Where(e => e.Status == status);
        if (TryParsePriority(request.Priority, out var priority))
            query = query.Where(e => e.Priority == priority);

        var items = await query
            .OrderByDescending(e => e.Priority)
            .ThenBy(e => e.CheckInTimeUtc)
            .ToListAsync(cancellationToken);

        return items.Select(ToListItem).ToList();
    }

    public async Task<PagedQueueResult> SearchAsync(SearchQueueRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgEntries().AsNoTracking();

        if (request.Date.HasValue)
            query = query.Where(e => e.QueueDate == request.Date);
        if (request.FromDate.HasValue)
            query = query.Where(e => e.QueueDate >= request.FromDate);
        if (request.ToDate.HasValue)
            query = query.Where(e => e.QueueDate <= request.ToDate);
        if (request.PatientId.HasValue)
            query = query.Where(e => e.PatientId == request.PatientId);
        if (request.AppointmentId.HasValue)
            query = query.Where(e => e.AppointmentId == request.AppointmentId);
        if (request.DoctorId.HasValue)
            query = query.Where(e => e.DoctorId == request.DoctorId);
        if (request.ClinicId.HasValue)
            query = query.Where(e => e.ClinicId == request.ClinicId);
        if (!string.IsNullOrWhiteSpace(request.QueueNumber))
            query = query.Where(e => e.QueueNumber.Contains(request.QueueNumber.Trim()));
        if (TryParseStatus(request.Status, out var status))
            query = query.Where(e => e.Status == status);
        if (TryParsePriority(request.Priority, out var priority))
            query = query.Where(e => e.Priority == priority);

        query = (request.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "queuenumber" => request.SortDescending
                ? query.OrderByDescending(e => e.QueueNumber)
                : query.OrderBy(e => e.QueueNumber),
            "status" => request.SortDescending
                ? query.OrderByDescending(e => e.Status)
                : query.OrderBy(e => e.Status),
            _ => query.OrderByDescending(e => e.Priority).ThenBy(e => e.CheckInTimeUtc)
        };

        var total = await query.CountAsync(cancellationToken);
        var pageItems = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        var items = pageItems.Select(ToListItem).ToList();

        return new PagedQueueResult(items, paging.NormalizedPage, paging.NormalizedPageSize, total);
    }

    public Task CallAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, e => e.Call(_user.UserId, _clock.UtcNow), cancellationToken);

    public Task StartServiceAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, e => e.StartService(_user.UserId, _clock.UtcNow), cancellationToken);

    public Task CompleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, e => e.Complete(_user.UserId, _clock.UtcNow), cancellationToken);

    public Task SkipAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, e => e.Skip(_user.UserId, _clock.UtcNow), cancellationToken);

    public Task CancelAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, e => e.Cancel(_user.UserId, _clock.UtcNow), cancellationToken);

    private async Task TransitionAsync(Guid id, Action<QueueEntry> action, CancellationToken cancellationToken)
    {
        var entry = await GetRequiredAsync(id, cancellationToken);
        try
        {
            action(entry);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(entry, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("queue.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("queue.concurrency_conflict", "Queue entry was modified by another operation.", 409);
        }
    }

    private void EnsureEligibleForCheckIn(AppointmentLookupResult appointment)
    {
        if (appointment.OrganizationId != _org.OrganizationId || appointment.BranchId != _org.BranchId)
            throw new AppException("appointments.not_found", "Appointment was not found.", 404);

        if (string.Equals(appointment.Status, "CheckedIn", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(
                "queue.duplicate_checkin",
                "Appointment already has an active queue entry.",
                409);
        }

        if (!string.Equals(appointment.Status, "Scheduled", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(appointment.Status, "Confirmed", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(
                "queue.checkin_not_eligible",
                $"Appointment status {appointment.Status} is not eligible for check-in.",
                400);
        }

        // Unresolved early/late window — allow check-in on appointment date (business clock).
        if (appointment.AppointmentDate != _clock.Today)
        {
            throw new AppException(
                "queue.checkin_wrong_date",
                "Check-in is only allowed on the appointment date.",
                400);
        }
    }

    private IQueryable<QueueEntry> OrgEntries() =>
        _db.QueueEntries.Where(e => e.OrganizationId == _org.OrganizationId);

    private async Task<QueueEntry> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await OrgEntries().SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
            throw new AppException("queue.not_found", "Queue entry was not found.", 404);
        return entry;
    }

    private async Task DispatchAsync(QueueEntry entry, CancellationToken cancellationToken)
    {
        var events = entry.DomainEvents.ToList();
        entry.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static QueuePriority ParsePriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
            return QueuePriority.Normal;
        return Enum.Parse<QueuePriority>(priority, ignoreCase: true);
    }

    private static bool TryParseStatus(string? value, out QueueStatus status) =>
        Enum.TryParse(value, true, out status);

    private static bool TryParsePriority(string? value, out QueuePriority priority) =>
        Enum.TryParse(value, true, out priority);

    private static QueueEntryDto Map(QueueEntry e) =>
        new(e.Id, e.OrganizationId, e.BranchId, e.QueueNumber, e.PatientId, e.AppointmentId,
            e.DoctorId, e.ClinicId, e.QueueDate, e.Priority.ToString(), e.Status.ToString(),
            e.CheckInTimeUtc, e.CalledTimeUtc, e.ServiceStartTimeUtc, e.ServiceEndTimeUtc,
            e.CreatedAtUtc, e.CreatedBy, e.UpdatedAtUtc, e.UpdatedBy);

    private static QueueListItemDto ToListItem(QueueEntry e) =>
        new(e.Id, e.QueueNumber, e.PatientId, e.AppointmentId, e.DoctorId, e.ClinicId, e.QueueDate,
            e.Priority.ToString(), e.Status.ToString(), e.CheckInTimeUtc, e.CalledTimeUtc,
            e.ServiceStartTimeUtc, e.ServiceEndTimeUtc);

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is not SqlException sql) return false;
        if (sql.Number is not (2601 or 2627)) return false;
        return sql.Message.Contains("IX_QueueEntries_ActiveAppointment", StringComparison.OrdinalIgnoreCase)
               || sql.Message.Contains("QueueNumber", StringComparison.OrdinalIgnoreCase)
               || sql.Message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }
}
