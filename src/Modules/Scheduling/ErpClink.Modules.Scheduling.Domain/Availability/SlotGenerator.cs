namespace ErpClink.Modules.Scheduling.Domain.Availability;

public readonly record struct TimeSlot(TimeOnly Start, TimeOnly End);

/// <summary>
/// Deterministic slot calculator. Slots are not persisted — calculated on demand.
/// A slot is included only when the full duration fits inside the working period.
/// </summary>
public static class SlotGenerator
{
    public static IReadOnlyList<TimeSlot> Generate(TimeOnly start, TimeOnly end, int slotDurationMinutes)
    {
        if (slotDurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(slotDurationMinutes));
        if (start >= end)
            return [];

        var duration = TimeSpan.FromMinutes(slotDurationMinutes);
        var slots = new List<TimeSlot>();
        var cursor = start;

        while (true)
        {
            var slotEnd = cursor.Add(duration);
            if (slotEnd > end)
                break;

            slots.Add(new TimeSlot(cursor, slotEnd));
            cursor = slotEnd;
        }

        return slots;
    }
}
