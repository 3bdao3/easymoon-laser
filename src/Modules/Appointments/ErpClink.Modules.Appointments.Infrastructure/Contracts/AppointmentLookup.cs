using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Appointments.Application.Contracts;
using ErpClink.Modules.Appointments.Domain.Appointments;
using ErpClink.Modules.Appointments.Infrastructure.Events;
using ErpClink.Modules.Appointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Appointments.Infrastructure.Contracts;

public sealed class AppointmentLookup : IAppointmentLookup
{
    private readonly AppointmentsDbContext _db;
    private readonly IOrganizationContext _org;

    public AppointmentLookup(AppointmentsDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<AppointmentLookupResult?> GetAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return await _db.Appointments.AsNoTracking()
            .Where(a => a.Id == appointmentId && a.OrganizationId == _org.OrganizationId)
            .Select(a => new AppointmentLookupResult(
                a.Id, a.OrganizationId, a.BranchId, a.AppointmentNumber,
                a.PatientId, a.DoctorId, a.ClinicId, a.AppointmentDate, a.StartTime, a.EndTime,
                a.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

public sealed class AppointmentCheckInPort : IAppointmentCheckInPort
{
    private readonly AppointmentsDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly Application.Common.IAppointmentsDomainEventDispatcher _events;

    public AppointmentCheckInPort(
        AppointmentsDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        Application.Common.IAppointmentsDomainEventDispatcher events)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
    }

    public async Task CheckInAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await _db.Appointments
            .SingleOrDefaultAsync(a => a.Id == appointmentId && a.OrganizationId == _org.OrganizationId, cancellationToken)
            ?? throw new AppException("appointments.not_found", "Appointment was not found.", 404);

        try
        {
            var before = appointment.StatusHistory.Count;
            appointment.CheckIn(_user.UserId, _clock.UtcNow);
            foreach (var entry in appointment.StatusHistory.Skip(before))
                _db.AppointmentStatusHistory.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);

            var domainEvents = appointment.DomainEvents.ToList();
            appointment.ClearDomainEvents();
            if (domainEvents.Count > 0)
                await _events.DispatchAsync(domainEvents, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("appointments.invalid_transition", ex.Message, 409);
        }
    }
}
