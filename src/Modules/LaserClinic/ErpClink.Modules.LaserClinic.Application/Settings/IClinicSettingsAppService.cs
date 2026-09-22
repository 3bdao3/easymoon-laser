namespace ErpClink.Modules.LaserClinic.Application.Settings;

public sealed record ClinicSettingsDto(
    Guid Id,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    int AppointmentSlotIntervalMinutes,
    int DefaultBufferMinutes,
    bool IsActive);

public sealed record UpdateClinicSettingsRequest(
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    int AppointmentSlotIntervalMinutes,
    int DefaultBufferMinutes);

public interface IClinicSettingsAppService
{
    Task<ClinicSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<ClinicSettingsDto> UpdateAsync(UpdateClinicSettingsRequest request, string? userId, CancellationToken cancellationToken = default);
}
