using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.LaserClinic.Application.Settings;
using ErpClink.Modules.LaserClinic.Domain.Settings;
using ErpClink.Modules.LaserClinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.LaserClinic.Infrastructure.Settings;

public sealed class ClinicSettingsAppService : IClinicSettingsAppService
{
    private readonly LaserClinicDbContext _db;

    public ClinicSettingsAppService(LaserClinicDbContext db) => _db = db;

    public async Task<ClinicSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await EnsureAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<ClinicSettingsDto> UpdateAsync(UpdateClinicSettingsRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        var settings = await EnsureAsync(cancellationToken);
        try
        {
            settings.Update(
                request.OpeningTime,
                request.ClosingTime,
                request.AppointmentSlotIntervalMinutes,
                request.DefaultBufferMinutes,
                userId,
                DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            throw new AppException("laser.settings.invalid", ex.Message, 400);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(settings);
    }

    internal async Task<ClinicSettings> EnsureAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.ClinicSettings.FirstOrDefaultAsync(s => s.IsActive, cancellationToken);
        if (settings is not null)
            return settings;

        settings = ClinicSettings.CreateDefault("system", DateTime.UtcNow);
        _db.ClinicSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static ClinicSettingsDto Map(ClinicSettings s) =>
        new(s.Id, s.OpeningTime, s.ClosingTime, s.AppointmentSlotIntervalMinutes, s.DefaultBufferMinutes, s.IsActive);
}
