using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Doctors.Application.Clinics;
using ErpClink.Modules.Doctors.Application.Clinics.Models;
using ErpClink.Modules.Doctors.Application.Common;
using ErpClink.Modules.Doctors.Domain.Clinics;
using ErpClink.Modules.Doctors.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Doctors.Infrastructure.Clinics;

public sealed class ClinicService : IClinicService
{
    private readonly DoctorsDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IDateTimeProvider _clock;
    private readonly IDoctorsDomainEventDispatcher _events;
    private readonly IValidator<CreateClinicRequest> _createValidator;
    private readonly IValidator<UpdateClinicRequest> _updateValidator;

    public ClinicService(
        DoctorsDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IDateTimeProvider clock,
        IDoctorsDomainEventDispatcher events,
        IValidator<CreateClinicRequest> createValidator,
        IValidator<UpdateClinicRequest> updateValidator)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ClinicDto> CreateAsync(CreateClinicRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);
        var code = request.Code.Trim().ToUpperInvariant();

        if (await OrgClinics().AnyAsync(c => c.Code == code, cancellationToken))
            throw new AppException("clinics.code_exists", "A clinic with this code already exists.", 409);

        var clinic = Clinic.Create(
            _org.OrganizationId,
            _org.BranchId,
            code,
            request.Name,
            request.Description,
            request.Location,
            _user.UserId,
            _clock.UtcNow);

        _db.Clinics.Add(clinic);
        await _db.SaveChangesAsync(cancellationToken);

        var events = clinic.DomainEvents.ToList();
        clinic.ClearDomainEvents();
        await _events.DispatchAsync(events, cancellationToken);

        return Map(clinic);
    }

    public async Task<ClinicDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var clinic = await OrgClinics().AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        return clinic is null ? null : Map(clinic);
    }

    public async Task<PagedClinicsResult> SearchAsync(SearchClinicsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgClinics().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var term = request.Query.Trim();
            query = query.Where(c => c.Code.Contains(term) || c.Name.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(request.Code))
            query = query.Where(c => c.Code.Contains(request.Code.Trim().ToUpperInvariant()));
        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(c => c.Name.Contains(request.Name.Trim()));
        if (request.BranchId.HasValue)
            query = query.Where(c => c.BranchId == request.BranchId.Value);
        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        query = (request.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "code" => request.SortDescending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
            "name" => request.SortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            _ => query.OrderByDescending(c => c.CreatedAtUtc)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize)
            .Select(c => new ClinicListItemDto(c.Id, c.Code, c.Name, c.Location, c.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedClinicsResult(items, paging.NormalizedPage, paging.NormalizedPageSize, total);
    }

    public async Task<ClinicDto> UpdateAsync(Guid id, UpdateClinicRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);
        var clinic = await GetRequiredAsync(id, cancellationToken);
        clinic.Update(request.Name, request.Description, request.Location, _user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(clinic);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var clinic = await GetRequiredAsync(id, cancellationToken);
        clinic.Activate(_user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var clinic = await GetRequiredAsync(id, cancellationToken);
        clinic.Deactivate(_user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Clinic> OrgClinics() =>
        _db.Clinics.Where(c => c.OrganizationId == _org.OrganizationId);

    private async Task<Clinic> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var clinic = await OrgClinics().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (clinic is null)
            throw new AppException("clinics.not_found", "Clinic was not found.", 404);
        return clinic;
    }

    private static ClinicDto Map(Clinic clinic) =>
        new(clinic.Id, clinic.OrganizationId, clinic.BranchId, clinic.Code, clinic.Name,
            clinic.Description, clinic.Location, clinic.IsActive,
            clinic.CreatedAtUtc, clinic.CreatedBy, clinic.UpdatedAtUtc, clinic.UpdatedBy);

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new AppException("clinics.validation_failed", string.Join(" ", result.Errors.Select(e => e.ErrorMessage)), 400);
    }
}
