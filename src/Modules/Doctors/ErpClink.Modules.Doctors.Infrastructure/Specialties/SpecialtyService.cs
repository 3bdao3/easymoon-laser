using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Doctors.Application.Specialties;
using ErpClink.Modules.Doctors.Application.Specialties.Models;
using ErpClink.Modules.Doctors.Domain.Specialties;
using ErpClink.Modules.Doctors.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Doctors.Infrastructure.Specialties;

public sealed class SpecialtyService : ISpecialtyService
{
    private readonly DoctorsDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IDateTimeProvider _clock;
    private readonly IValidator<CreateSpecialtyRequest> _createValidator;
    private readonly IValidator<UpdateSpecialtyRequest> _updateValidator;

    public SpecialtyService(
        DoctorsDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IDateTimeProvider clock,
        IValidator<CreateSpecialtyRequest> createValidator,
        IValidator<UpdateSpecialtyRequest> updateValidator)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<SpecialtyDto> CreateAsync(CreateSpecialtyRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await OrgSpecialties().AnyAsync(s => s.Code == code, cancellationToken))
            throw new AppException("specialties.code_exists", "A specialty with this code already exists.", 409);

        var specialty = Specialty.Create(_org.OrganizationId, code, request.Name, request.Description, _user.UserId, _clock.UtcNow);
        _db.Specialties.Add(specialty);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(specialty);
    }

    public async Task<SpecialtyDto> UpdateAsync(Guid id, UpdateSpecialtyRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);
        var specialty = await GetRequiredAsync(id, cancellationToken);
        specialty.Update(request.Name, request.Description, _user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(specialty);
    }

    public async Task<IReadOnlyList<SpecialtyDto>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = OrgSpecialties().AsNoTracking();
        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var items = await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var specialty = await GetRequiredAsync(id, cancellationToken);
        specialty.Activate(_user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var specialty = await GetRequiredAsync(id, cancellationToken);
        specialty.Deactivate(_user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Specialty> OrgSpecialties() =>
        _db.Specialties.Where(s => s.OrganizationId == _org.OrganizationId);

    private async Task<Specialty> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var specialty = await OrgSpecialties().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (specialty is null)
            throw new AppException("specialties.not_found", "Specialty was not found.", 404);
        return specialty;
    }

    private static SpecialtyDto Map(Specialty s) =>
        new(s.Id, s.OrganizationId, s.Code, s.Name, s.Description, s.IsActive,
            s.CreatedAtUtc, s.CreatedBy, s.UpdatedAtUtc, s.UpdatedBy);

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new AppException("specialties.validation_failed", string.Join(" ", result.Errors.Select(e => e.ErrorMessage)), 400);
    }
}
