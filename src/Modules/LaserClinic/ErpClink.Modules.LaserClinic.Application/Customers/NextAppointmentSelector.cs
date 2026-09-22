using ErpClink.Modules.LaserClinic.Domain.Appointments;

namespace ErpClink.Modules.LaserClinic.Application.Customers;

/// <summary>Pure helpers for next-appointment selection (Egypt clinic local calendar).</summary>
public static class NextAppointmentSelector
{
    public static bool IsUpcoming(
        DateOnly appointmentDate,
        TimeOnly startTime,
        LaserAppointmentStatus status,
        DateOnly today,
        TimeOnly nowTime)
    {
        if (status is not (LaserAppointmentStatus.Pending or LaserAppointmentStatus.Confirmed))
            return false;

        return appointmentDate > today
               || (appointmentDate == today && startTime >= nowTime);
    }

    public static T? PickNext<T>(
        IEnumerable<T> appointments,
        Func<T, DateOnly> date,
        Func<T, TimeOnly> start)
        where T : class
    {
        return appointments
            .OrderBy(date)
            .ThenBy(start)
            .FirstOrDefault();
    }
}
