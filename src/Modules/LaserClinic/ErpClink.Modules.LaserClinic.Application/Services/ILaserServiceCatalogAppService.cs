namespace ErpClink.Modules.LaserClinic.Application.Services;

public sealed record LaserServiceDto(
    Guid Id,
    string Name,
    int MinDurationMinutes,
    int MaxDurationMinutes,
    int RecommendedDurationMinutes,
    bool IsActive,
    int DisplayOrder,
    string? Notes,
    bool RequiresManualDuration);

public sealed record CreateLaserServiceRequest(
    string Name,
    int MinDurationMinutes,
    int MaxDurationMinutes,
    int DisplayOrder,
    string? Notes);

public sealed record UpdateLaserServiceRequest(
    string Name,
    int MinDurationMinutes,
    int MaxDurationMinutes,
    int DisplayOrder,
    string? Notes,
    bool IsActive);

public interface ILaserServiceCatalogAppService
{
    Task<IReadOnlyList<LaserServiceDto>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<LaserServiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LaserServiceDto> CreateAsync(CreateLaserServiceRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<LaserServiceDto> UpdateAsync(Guid id, UpdateLaserServiceRequest request, string? userId, CancellationToken cancellationToken = default);
}
