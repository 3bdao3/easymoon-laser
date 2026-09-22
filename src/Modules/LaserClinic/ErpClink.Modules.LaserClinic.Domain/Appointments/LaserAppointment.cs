using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.LaserClinic.Domain.Appointments;

public enum LaserAppointmentStatus
{
    Pending = 0,
    Confirmed = 1,
    Attended = 2,
    Cancelled = 3,
    NoShow = 4
}

public sealed class AppointmentServiceLine
{
    private AppointmentServiceLine()
    {
    }

    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid LaserServiceId { get; private set; }
    public int DurationMinutes { get; private set; }
    public int? PulsesConsumed { get; private set; }

    public static AppointmentServiceLine Create(
        Guid appointmentId,
        Guid laserServiceId,
        int durationMinutes,
        int? pulsesConsumed = null)
    {
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        if (pulsesConsumed is < 1)
            throw new ArgumentOutOfRangeException(nameof(pulsesConsumed));

        return new AppointmentServiceLine
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            LaserServiceId = laserServiceId,
            DurationMinutes = durationMinutes,
            PulsesConsumed = pulsesConsumed
        };
    }
}

public sealed class LaserAppointment : AggregateRoot
{
    private readonly List<AppointmentServiceLine> _services = [];

    private LaserAppointment()
    {
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateOnly AppointmentDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public LaserAppointmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public decimal? AmountPaid { get; private set; }

    public IReadOnlyCollection<AppointmentServiceLine> Services => _services.AsReadOnly();

    public bool OccupiesSlot =>
        Status is LaserAppointmentStatus.Pending
            or LaserAppointmentStatus.Confirmed
            or LaserAppointmentStatus.Attended;

    public static LaserAppointment Create(
        Guid customerId,
        DateOnly appointmentDate,
        TimeOnly startTime,
        int durationMinutes,
        IReadOnlyList<(Guid LaserServiceId, int DurationMinutes, int? PulsesConsumed)> services,
        string? notes,
        string? userId,
        DateTime utcNow)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer is required.", nameof(customerId));
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        if (services is null || services.Count == 0)
            throw new ArgumentException("At least one service is required.", nameof(services));

        var end = startTime.AddMinutes(durationMinutes);
        var appointment = new LaserAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            AppointmentDate = appointmentDate,
            StartTime = startTime,
            EndTime = end,
            DurationMinutes = durationMinutes,
            Status = LaserAppointmentStatus.Pending,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
        appointment.SetCreated(userId, utcNow);

        foreach (var line in services)
        {
            appointment._services.Add(
                AppointmentServiceLine.Create(
                    appointment.Id,
                    line.LaserServiceId,
                    line.DurationMinutes,
                    line.PulsesConsumed));
        }

        return appointment;
    }

    public void UpdateNotes(string? notes, string? userId, DateTime utcNow)
    {
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        SetUpdated(userId, utcNow);
    }

    public void SetAmountPaid(decimal? amountPaid, string? userId, DateTime utcNow)
    {
        if (amountPaid is < 0)
            throw new ArgumentOutOfRangeException(nameof(amountPaid), "Amount paid cannot be negative.");
        AmountPaid = amountPaid;
        SetUpdated(userId, utcNow);
    }

    public void ChangeStatus(LaserAppointmentStatus status, string? userId, DateTime utcNow)
    {
        Status = status;
        SetUpdated(userId, utcNow);
    }

    public void Reschedule(DateOnly date, TimeOnly startTime, int durationMinutes, string? userId, DateTime utcNow)
    {
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        AppointmentDate = date;
        StartTime = startTime;
        EndTime = startTime.AddMinutes(durationMinutes);
        DurationMinutes = durationMinutes;
        SetUpdated(userId, utcNow);
    }

    public void ReplaceServices(
        IReadOnlyList<(Guid LaserServiceId, int DurationMinutes, int? PulsesConsumed)> services,
        string? userId,
        DateTime utcNow)
    {
        if (services is null || services.Count == 0)
            throw new ArgumentException("At least one service is required.", nameof(services));

        _services.Clear();
        foreach (var line in services)
        {
            _services.Add(
                AppointmentServiceLine.Create(Id, line.LaserServiceId, line.DurationMinutes, line.PulsesConsumed));
        }

        SetUpdated(userId, utcNow);
    }

    /// <summary>
    /// Classic interval overlap: ExistingStart &lt; NewEnd AND ExistingEnd &gt; NewStart.
    /// </summary>
    public static bool Overlaps(TimeOnly existingStart, TimeOnly existingEnd, TimeOnly newStart, TimeOnly newEnd) =>
        existingStart < newEnd && existingEnd > newStart;
}
