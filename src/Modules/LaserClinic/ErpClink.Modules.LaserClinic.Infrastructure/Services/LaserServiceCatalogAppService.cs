using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.LaserClinic.Application.Scheduling;
using ErpClink.Modules.LaserClinic.Application.Services;
using ErpClink.Modules.LaserClinic.Domain.Services;
using ErpClink.Modules.LaserClinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.LaserClinic.Infrastructure.Services;

public sealed class LaserServiceCatalogAppService : ILaserServiceCatalogAppService
{
    private readonly LaserClinicDbContext _db;

    public LaserServiceCatalogAppService(LaserClinicDbContext db) => _db = db;

    public async Task<IReadOnlyList<LaserServiceDto>> ListAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var q = _db.LaserServices.AsNoTracking().AsQueryable();
        if (activeOnly)
            q = q.Where(s => s.IsActive);

        var items = await q.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<LaserServiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.LaserServices.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<LaserServiceDto> CreateAsync(CreateLaserServiceRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        var entity = LaserService.Create(
            request.Name,
            request.MinDurationMinutes,
            request.MaxDurationMinutes,
            request.DisplayOrder,
            request.Price,
            request.Notes,
            userId,
            DateTime.UtcNow);
        _db.LaserServices.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<LaserServiceDto> UpdateAsync(Guid id, UpdateLaserServiceRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.LaserServices.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new AppException("laser.service.not_found", "الخدمة غير موجودة.", 404);

        entity.Update(
            request.Name,
            request.MinDurationMinutes,
            request.MaxDurationMinutes,
            request.DisplayOrder,
            request.Price,
            request.Notes,
            userId,
            DateTime.UtcNow);
        entity.SetActive(request.IsActive, userId, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, string? userId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.LaserServices.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new AppException("laser.service.not_found", "الخدمة غير موجودة.", 404);

        var used = await _db.AppointmentServices.AnyAsync(s => s.LaserServiceId == id, cancellationToken);
        if (used)
            entity.SetActive(false, userId, DateTime.UtcNow);
        else
            _db.LaserServices.Remove(entity);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static LaserServiceDto Map(LaserService s) =>
        new(
            s.Id,
            s.Name,
            s.MinDurationMinutes,
            s.MaxDurationMinutes,
            DurationCalculator.RecommendedMinutes(s),
            s.IsActive,
            s.DisplayOrder,
            s.Price,
            s.Notes,
            DurationCalculator.RequiresManualDuration(s));
}
