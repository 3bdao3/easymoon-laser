using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.LaserClinic.Domain.Settings;

public sealed class ClinicSettings : AggregateRoot
{
    private ClinicSettings()
    {
    }

    public Guid Id { get; private set; }
    public TimeOnly OpeningTime { get; private set; }
    public TimeOnly ClosingTime { get; private set; }
    public int AppointmentSlotIntervalMinutes { get; private set; }
    public int DefaultBufferMinutes { get; private set; }
    public bool IsActive { get; private set; }

    public static ClinicSettings CreateDefault(string? userId, DateTime utcNow)
    {
        var settings = new ClinicSettings
        {
            Id = Guid.NewGuid(),
            OpeningTime = new TimeOnly(16, 0),
            ClosingTime = new TimeOnly(22, 0),
            AppointmentSlotIntervalMinutes = 15,
            DefaultBufferMinutes = 0,
            IsActive = true
        };
        settings.SetCreated(userId, utcNow);
        return settings;
    }

    public void Update(
        TimeOnly openingTime,
        TimeOnly closingTime,
        int appointmentSlotIntervalMinutes,
        int defaultBufferMinutes,
        string? userId,
        DateTime utcNow)
    {
        if (closingTime <= openingTime)
            throw new ArgumentException("Closing time must be after opening time.");
        if (appointmentSlotIntervalMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(appointmentSlotIntervalMinutes));
        if (defaultBufferMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultBufferMinutes));

        OpeningTime = openingTime;
        ClosingTime = closingTime;
        AppointmentSlotIntervalMinutes = appointmentSlotIntervalMinutes;
        DefaultBufferMinutes = defaultBufferMinutes;
        SetUpdated(userId, utcNow);
    }
}
