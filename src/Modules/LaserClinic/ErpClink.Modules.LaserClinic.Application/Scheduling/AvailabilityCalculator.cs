using ErpClink.Modules.LaserClinic.Domain.Appointments;

namespace ErpClink.Modules.LaserClinic.Application.Scheduling;

/// <summary>Occupied clinic time including buffer. Optional booking metadata for UI timeline.</summary>
public sealed record OccupiedInterval(
    TimeOnly Start,
    TimeOnly End,
    TimeOnly ClinicalEnd,
    Guid? AppointmentId = null,
    string? CustomerName = null,
    int? DurationMinutes = null,
    string? ServiceNames = null);

public sealed record AvailableSlot(TimeOnly StartTime, TimeOnly EndTime, int DurationMinutes);

public enum DaySlotStatus
{
    Available = 0,
    Booked = 1,
    Unavailable = 2
}

public sealed record DayTimelineSlot(
    TimeOnly StartTime,
    TimeOnly EndTime,
    int DurationMinutes,
    DaySlotStatus Status,
    Guid? AppointmentId,
    string? CustomerName,
    string? ServiceNames,
    int? BookedDurationMinutes);

/// <summary>Computes available appointment start times within clinic hours.</summary>
public static class AvailabilityCalculator
{
    public static IReadOnlyList<AvailableSlot> GetAvailableSlots(
        TimeOnly openingTime,
        TimeOnly closingTime,
        int slotIntervalMinutes,
        int clinicalDurationMinutes,
        int bufferMinutes,
        IReadOnlyList<OccupiedInterval> occupied)
    {
        return GetDayTimeline(
                openingTime,
                closingTime,
                slotIntervalMinutes,
                clinicalDurationMinutes,
                bufferMinutes,
                occupied,
                nowLocal: null,
                date: null)
            .Where(s => s.Status == DaySlotStatus.Available)
            .Select(s => new AvailableSlot(s.StartTime, s.EndTime, s.DurationMinutes))
            .ToList();
    }

    /// <summary>
    /// Full interval grid for a day: Available / Booked / Unavailable.
    /// Past starts (when date is today and nowLocal is set) are Unavailable.
    /// </summary>
    public static IReadOnlyList<DayTimelineSlot> GetDayTimeline(
        TimeOnly openingTime,
        TimeOnly closingTime,
        int slotIntervalMinutes,
        int clinicalDurationMinutes,
        int bufferMinutes,
        IReadOnlyList<OccupiedInterval> occupied,
        TimeOnly? nowLocal,
        DateOnly? date,
        DateOnly? todayLocal = null)
    {
        if (closingTime <= openingTime)
            return [];
        if (slotIntervalMinutes <= 0 || clinicalDurationMinutes <= 0)
            return [];
        if (bufferMinutes < 0)
            return [];

        var blocked = DurationCalculator.EffectiveBlockedMinutes(clinicalDurationMinutes, bufferMinutes);
        var slots = new List<DayTimelineSlot>();
        var isToday = date.HasValue && todayLocal.HasValue && date.Value == todayLocal.Value;

        // Candidate starts from opening until closing (exclusive of closing as a start).
        for (var start = openingTime;
             start < closingTime;
             start = start.AddMinutes(slotIntervalMinutes))
        {
            var clinicalEnd = start.AddMinutes(clinicalDurationMinutes);
            var blockedEnd = start.AddMinutes(blocked);

            // Exact match to an existing appointment start → Booked
            var booking = occupied.FirstOrDefault(o => o.AppointmentId.HasValue && o.Start == start);
            if (booking is not null)
            {
                slots.Add(new DayTimelineSlot(
                    start,
                    booking.ClinicalEnd,
                    booking.DurationMinutes ?? clinicalDurationMinutes,
                    DaySlotStatus.Booked,
                    booking.AppointmentId,
                    booking.CustomerName,
                    booking.ServiceNames,
                    booking.DurationMinutes));
                continue;
            }

            if (blockedEnd > closingTime || clinicalEnd > closingTime)
            {
                slots.Add(new DayTimelineSlot(
                    start, clinicalEnd, clinicalDurationMinutes,
                    DaySlotStatus.Unavailable, null, null, null, null));
                continue;
            }

            if (isToday && nowLocal.HasValue && start <= nowLocal.Value)
            {
                slots.Add(new DayTimelineSlot(
                    start, clinicalEnd, clinicalDurationMinutes,
                    DaySlotStatus.Unavailable, null, null, null, null));
                continue;
            }

            if (Conflicts(start, blockedEnd, occupied))
            {
                slots.Add(new DayTimelineSlot(
                    start, clinicalEnd, clinicalDurationMinutes,
                    DaySlotStatus.Unavailable, null, null, null, null));
                continue;
            }

            slots.Add(new DayTimelineSlot(
                start, clinicalEnd, clinicalDurationMinutes,
                DaySlotStatus.Available, null, null, null, null));
        }

        return slots;
    }

    public static bool Conflicts(TimeOnly newStart, TimeOnly newEnd, IReadOnlyList<OccupiedInterval> occupied) =>
        occupied.Any(o => LaserAppointment.Overlaps(o.Start, o.End, newStart, newEnd));
}
