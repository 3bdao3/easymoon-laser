namespace ErpClink.Modules.LaserClinic.Application.Offers;

public sealed record LaserOfferDto(
    Guid Id,
    string Title,
    string? Description,
    decimal Price,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsActive,
    int DisplayOrder,
    DateTime CreatedAtUtc);

public sealed record CreateLaserOfferRequest(
    string Title,
    string? Description,
    decimal Price,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int DisplayOrder);

public sealed record UpdateLaserOfferRequest(
    string Title,
    string? Description,
    decimal Price,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int DisplayOrder,
    bool IsActive);

public interface ILaserOfferAppService
{
    Task<IReadOnlyList<LaserOfferDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<LaserOfferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LaserOfferDto> CreateAsync(CreateLaserOfferRequest request, string? userId, CancellationToken cancellationToken = default);
    Task<LaserOfferDto> UpdateAsync(Guid id, UpdateLaserOfferRequest request, string? userId, CancellationToken cancellationToken = default);
}
