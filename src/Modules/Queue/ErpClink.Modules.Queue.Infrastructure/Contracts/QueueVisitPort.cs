using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Queue.Application.Contracts;
using ErpClink.Modules.Queue.Domain.Entries;
using ErpClink.Modules.Queue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Queue.Infrastructure.Contracts;

public sealed class QueueVisitPort : IQueueVisitPort
{
    private readonly QueueDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly Application.Common.IQueueDomainEventDispatcher _events;

    public QueueVisitPort(
        QueueDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        Application.Common.IQueueDomainEventDispatcher events)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
    }

    public async Task<QueueVisitLookupResult?> GetByIdAsync(Guid queueEntryId, CancellationToken cancellationToken = default)
    {
        return await _db.QueueEntries.AsNoTracking()
            .Where(e => e.Id == queueEntryId && e.OrganizationId == _org.OrganizationId)
            .Select(e => new QueueVisitLookupResult(
                e.Id, e.OrganizationId, e.BranchId, e.AppointmentId, e.PatientId,
                e.DoctorId, e.ClinicId, e.QueueDate, e.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<QueueVisitLookupResult?> GetActiveByAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        return await _db.QueueEntries.AsNoTracking()
            .Where(e =>
                e.OrganizationId == _org.OrganizationId &&
                e.AppointmentId == appointmentId &&
                (e.Status == QueueStatus.Waiting || e.Status == QueueStatus.Called || e.Status == QueueStatus.InService))
            .Select(e => new QueueVisitLookupResult(
                e.Id, e.OrganizationId, e.BranchId, e.AppointmentId, e.PatientId,
                e.DoctorId, e.ClinicId, e.QueueDate, e.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task EnsureInServiceAsync(Guid queueEntryId, CancellationToken cancellationToken = default)
    {
        var entry = await _db.QueueEntries
            .SingleOrDefaultAsync(e => e.Id == queueEntryId && e.OrganizationId == _org.OrganizationId, cancellationToken)
            ?? throw new AppException("queue.not_found", "Queue entry was not found.", 404);

        if (entry.Status == QueueStatus.InService)
            return;

        if (entry.Status == QueueStatus.Waiting)
            entry.Call(_user.UserId, _clock.UtcNow);

        if (entry.Status == QueueStatus.Called)
            entry.StartService(_user.UserId, _clock.UtcNow);
        else if (entry.Status != QueueStatus.InService)
        {
            throw new AppException(
                "queue.invalid_for_visit",
                $"Queue entry status {entry.Status} is not valid for starting a medical visit.",
                400);
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            var events = entry.DomainEvents.ToList();
            entry.ClearDomainEvents();
            if (events.Count > 0)
                await _events.DispatchAsync(events, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("queue.concurrency_conflict", "Queue entry was modified by another operation.", 409);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("queue.invalid_transition", ex.Message, 409);
        }
    }
}
