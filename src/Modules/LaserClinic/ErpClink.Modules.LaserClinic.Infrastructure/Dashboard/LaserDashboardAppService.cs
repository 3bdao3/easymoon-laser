using ErpClink.Modules.LaserClinic.Application.Appointments;
using ErpClink.Modules.LaserClinic.Application.Dashboard;
using ErpClink.Modules.LaserClinic.Domain.Appointments;
using ErpClink.Modules.LaserClinic.Infrastructure.Persistence;
using ErpClink.Modules.LaserClinic.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.LaserClinic.Infrastructure.Dashboard;

public sealed class LaserDashboardAppService : ILaserDashboardAppService
{
    private readonly LaserClinicDbContext _db;
    private readonly ILaserAppointmentAppService _appointments;
    private readonly ClinicSettingsAppService _settings;

    public LaserDashboardAppService(
        LaserClinicDbContext db,
        ILaserAppointmentAppService appointments,
        ClinicSettingsAppService settings)
    {
        _db = db;
        _appointments = appointments;
        _settings = settings;
    }

    public async Task<LaserDashboardDto> GetAsync(DateOnly? date, CancellationToken cancellationToken = default)
    {
        var day = date ?? DateOnly.FromDateTime(DateTime.Today);
        var activeCustomerIds = (await _db.Customers.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken)).ToHashSet();

        var todays = (await _appointments.ListAsync(day, null, cancellationToken))
            .Where(a => activeCustomerIds.Contains(a.CustomerId)
                        && a.Status != LaserAppointmentStatus.Cancelled)
            .ToList();
        var settings = await _settings.EnsureAsync(cancellationToken);
        var totalMinutes = (int)(settings.ClosingTime - settings.OpeningTime).TotalMinutes;

        var occupying = todays.Where(a =>
            a.Status is LaserAppointmentStatus.Pending
                or LaserAppointmentStatus.Confirmed
                or LaserAppointmentStatus.Attended).ToList();

        var occupied = occupying.Sum(a => a.DurationMinutes);
        var remaining = todays.Count(a =>
            a.Status is LaserAppointmentStatus.Pending or LaserAppointmentStatus.Confirmed);

        var upcoming = (await _appointments.ListAsync(null, null, cancellationToken))
            .Where(a => activeCustomerIds.Contains(a.CustomerId)
                        && (a.AppointmentDate > day
                            || (a.AppointmentDate == day && a.Status is LaserAppointmentStatus.Pending or LaserAppointmentStatus.Confirmed)))
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
            .Take(10)
            .ToList();

        var monthStart = new DateOnly(day.Year, day.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var activeAppointments = _db.Appointments.AsNoTracking()
            .Where(a => a.Status != LaserAppointmentStatus.Cancelled
                        && _db.Customers.Any(c => c.Id == a.CustomerId && c.IsActive));

        var pulseLines = _db.AppointmentServices.AsNoTracking()
            .Where(line => line.PulsesConsumed != null && line.PulsesConsumed > 0)
            .Join(
                activeAppointments,
                line => line.AppointmentId,
                a => a.Id,
                (line, a) => new { a.AppointmentDate, Pulses = line.PulsesConsumed!.Value });

        var pulsesToday = await pulseLines
            .Where(x => x.AppointmentDate == day)
            .SumAsync(x => (int?)x.Pulses ?? 0, cancellationToken);

        var pulsesThisMonth = await pulseLines
            .Where(x => x.AppointmentDate >= monthStart && x.AppointmentDate < nextMonth)
            .SumAsync(x => (int?)x.Pulses ?? 0, cancellationToken);

        var pulsesAllTime = await pulseLines
            .SumAsync(x => (int?)x.Pulses ?? 0, cancellationToken);

        return new LaserDashboardDto(
            day,
            todays.Count,
            todays.Count(a => a.Status == LaserAppointmentStatus.Pending),
            todays.Count(a => a.Status == LaserAppointmentStatus.Confirmed),
            todays.Count(a => a.Status == LaserAppointmentStatus.Attended),
            todays.Count(a => a.Status == LaserAppointmentStatus.Cancelled),
            todays.Count(a => a.Status == LaserAppointmentStatus.NoShow),
            remaining,
            occupied,
            Math.Max(0, totalMinutes - occupied),
            pulsesToday,
            pulsesThisMonth,
            pulsesAllTime,
            todays.OrderBy(a => a.StartTime).ToList(),
            upcoming);
    }
}
