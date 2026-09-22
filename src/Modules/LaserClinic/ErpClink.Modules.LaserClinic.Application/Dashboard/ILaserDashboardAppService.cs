using ErpClink.Modules.LaserClinic.Application.Appointments;

namespace ErpClink.Modules.LaserClinic.Application.Dashboard;

public sealed record LaserDashboardDto(
    DateOnly Date,
    int TotalAppointments,
    int PendingCount,
    int ConfirmedCount,
    int AttendedCount,
    int CancelledCount,
    int NoShowCount,
    int RemainingCount,
    int OccupiedMinutes,
    int AvailableMinutes,
    int PulsesToday,
    int PulsesThisMonth,
    int PulsesAllTime,
    IReadOnlyList<LaserAppointmentDto> TodaysAppointments,
    IReadOnlyList<LaserAppointmentDto> Upcoming);

public interface ILaserDashboardAppService
{
    Task<LaserDashboardDto> GetAsync(DateOnly? date, CancellationToken cancellationToken = default);
}
