using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.LaserClinic.Application.Offers;
using ErpClink.Modules.LaserClinic.Domain.Offers;
using ErpClink.Modules.LaserClinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.LaserClinic.Infrastructure.Offers;

public sealed class LaserOfferAppService : ILaserOfferAppService
{
    private readonly LaserClinicDbContext _db;

    public LaserOfferAppService(LaserClinicDbContext db) => _db = db;

    public async Task<IReadOnlyList<LaserOfferDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var q = _db.LaserOffers.AsNoTracking().AsQueryable();
        if (activeOnly)
            q = q.Where(o => o.IsActive);

        var items = await q
            .OrderBy(o => o.DisplayOrder)
            .ThenByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<LaserOfferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.LaserOffers.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<LaserOfferDto> CreateAsync(
        CreateLaserOfferRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = LaserOffer.Create(
                request.Title,
                request.Description,
                request.Price,
                request.ValidFrom,
                request.ValidTo,
                request.DisplayOrder,
                userId,
                DateTime.UtcNow);
            _db.LaserOffers.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Map(entity);
        }
        catch (ArgumentException ex)
        {
            throw new AppException("laser.offer.invalid", ex.Message, 400);
        }
    }

    public async Task<LaserOfferDto> UpdateAsync(
        Guid id,
        UpdateLaserOfferRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.LaserOffers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new AppException("laser.offer.not_found", "العرض غير موجود.", 404);

        try
        {
            entity.Update(
                request.Title,
                request.Description,
                request.Price,
                request.ValidFrom,
                request.ValidTo,
                request.DisplayOrder,
                userId,
                DateTime.UtcNow);
            entity.SetActive(request.IsActive, userId, DateTime.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return Map(entity);
        }
        catch (ArgumentException ex)
        {
            throw new AppException("laser.offer.invalid", ex.Message, 400);
        }
    }

    private static LaserOfferDto Map(LaserOffer o) =>
        new(o.Id, o.Title, o.Description, o.Price, o.ValidFrom, o.ValidTo, o.IsActive, o.DisplayOrder, o.CreatedAtUtc);
}
