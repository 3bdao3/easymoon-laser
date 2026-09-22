using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Doctors.Application.Common;
using ErpClink.Modules.Doctors.Application.Doctors;
using ErpClink.Modules.Doctors.Application.Doctors.Models;
using ErpClink.Modules.Doctors.Domain.Doctors;
using ErpClink.Modules.Doctors.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Doctors.Infrastructure.Doctors;

public sealed class DoctorService : IDoctorService
{
    private readonly DoctorsDbContext _db;
    private readonly IDoctorNumberGenerator _numbers;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IDateTimeProvider _clock;
    private readonly IDoctorsDomainEventDispatcher _events;
    private readonly IValidator<RegisterDoctorRequest> _registerValidator;
    private readonly IValidator<UpdateDoctorRequest> _updateValidator;
    private readonly IValidator<AssignDoctorToClinicRequest> _assignValidator;

    public DoctorService(
        DoctorsDbContext db,
        IDoctorNumberGenerator numbers,
        IOrganizationContext org,
        ICurrentUser user,
        IDateTimeProvider clock,
        IDoctorsDomainEventDispatcher events,
        IValidator<RegisterDoctorRequest> registerValidator,
        IValidator<UpdateDoctorRequest> updateValidator,
        IValidator<AssignDoctorToClinicRequest> assignValidator)
    {
        _db = db;
        _numbers = numbers;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _registerValidator = registerValidator;
        _updateValidator = updateValidator;
        _assignValidator = assignValidator;
    }

    public async Task<DoctorDto> RegisterAsync(RegisterDoctorRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_registerValidator, request, cancellationToken);
        await EnsureSpecialtyAsync(request.SpecialtyId, cancellationToken);

        var number = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
        var doctor = Doctor.Register(
            _org.OrganizationId,
            _org.BranchId,
            number,
            request.UserId,
            request.FirstName,
            request.LastName,
            request.DisplayName,
            request.SpecialtyId,
            request.LicenseNumber,
            request.PhoneNumber,
            request.Email,
            _user.UserId,
            _clock.UtcNow);

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync(cancellationToken);
        await DispatchAsync(doctor, cancellationToken);
        return await MapDoctorAsync(doctor, cancellationToken);
    }

    public async Task<DoctorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doctor = await OrgDoctors().AsNoTracking().SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        return doctor is null ? null : await MapDoctorAsync(doctor, cancellationToken);
    }

    public async Task<DoctorDto?> GetByNumberAsync(string doctorNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(doctorNumber))
            throw new AppException("doctors.invalid_request", "Doctor number is required.", 400);

        var doctor = await OrgDoctors().AsNoTracking()
            .SingleOrDefaultAsync(d => d.DoctorNumber == doctorNumber.Trim(), cancellationToken);
        return doctor is null ? null : await MapDoctorAsync(doctor, cancellationToken);
    }

    public async Task<PagedDoctorsResult> SearchAsync(SearchDoctorsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgDoctors().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var term = request.Query.Trim();
            query = query.Where(d =>
                d.DoctorNumber.Contains(term) ||
                d.FirstName.Contains(term) ||
                d.LastName.Contains(term) ||
                d.DisplayName.Contains(term) ||
                (d.PhoneNumber != null && d.PhoneNumber.Contains(term)) ||
                (d.Email != null && d.Email.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.DoctorNumber))
            query = query.Where(d => d.DoctorNumber.Contains(request.DoctorNumber.Trim()));
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(d => d.DisplayName.Contains(name) || d.FirstName.Contains(name) || d.LastName.Contains(name));
        }
        if (!string.IsNullOrWhiteSpace(request.Phone))
            query = query.Where(d => d.PhoneNumber != null && d.PhoneNumber.Contains(request.Phone.Trim()));
        if (!string.IsNullOrWhiteSpace(request.Email))
            query = query.Where(d => d.Email != null && d.Email.Contains(request.Email.Trim()));
        if (request.SpecialtyId.HasValue)
            query = query.Where(d => d.SpecialtyId == request.SpecialtyId);
        if (request.IsActive.HasValue)
            query = query.Where(d => d.IsActive == request.IsActive.Value);
        if (request.ClinicId.HasValue)
        {
            var clinicId = request.ClinicId.Value;
            query = query.Where(d => _db.DoctorClinicAssignments.Any(a =>
                a.DoctorId == d.Id && a.ClinicId == clinicId && a.IsActive && a.OrganizationId == _org.OrganizationId));
        }

        query = (request.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "doctornumber" => request.SortDescending ? query.OrderByDescending(d => d.DoctorNumber) : query.OrderBy(d => d.DoctorNumber),
            "name" => request.SortDescending ? query.OrderByDescending(d => d.DisplayName) : query.OrderBy(d => d.DisplayName),
            _ => query.OrderByDescending(d => d.CreatedAtUtc)
        };

        var total = await query.CountAsync(cancellationToken);
        var doctors = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        var specialtyIds = doctors.Where(d => d.SpecialtyId.HasValue).Select(d => d.SpecialtyId!.Value).Distinct().ToList();
        var specialties = await _db.Specialties.AsNoTracking()
            .Where(s => s.OrganizationId == _org.OrganizationId && specialtyIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var items = doctors.Select(d => new DoctorListItemDto(
            d.Id,
            d.DoctorNumber,
            d.DisplayName,
            d.SpecialtyId,
            d.SpecialtyId.HasValue && specialties.TryGetValue(d.SpecialtyId.Value, out var n) ? n : null,
            d.PhoneNumber,
            d.Email,
            d.IsActive)).ToList();

        return new PagedDoctorsResult(items, paging.NormalizedPage, paging.NormalizedPageSize, total);
    }

    public async Task<DoctorDto> UpdateAsync(Guid id, UpdateDoctorRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);
        await EnsureSpecialtyAsync(request.SpecialtyId, cancellationToken);
        var doctor = await GetRequiredAsync(id, cancellationToken);

        try
        {
            doctor.Update(
                request.FirstName,
                request.LastName,
                request.DisplayName,
                request.SpecialtyId,
                request.LicenseNumber,
                request.PhoneNumber,
                request.Email,
                request.UserId,
                _user.UserId,
                _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("doctors.update_failed", ex.Message, 400);
        }

        return await MapDoctorAsync(doctor, cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doctor = await GetRequiredAsync(id, cancellationToken);
        doctor.Activate(_user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        await DispatchAsync(doctor, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doctor = await GetRequiredAsync(id, cancellationToken);
        doctor.Deactivate(_user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        await DispatchAsync(doctor, cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorClinicAssignmentDto>> GetClinicAssignmentsAsync(
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        _ = await GetRequiredAsync(doctorId, cancellationToken);

        return await (
            from a in _db.DoctorClinicAssignments.AsNoTracking()
            join c in _db.Clinics.AsNoTracking() on a.ClinicId equals c.Id
            where a.OrganizationId == _org.OrganizationId && a.DoctorId == doctorId
            orderby a.IsActive descending, a.StartDate descending
            select new DoctorClinicAssignmentDto(
                a.Id, a.DoctorId, a.ClinicId, c.Code, c.Name, a.StartDate, a.EndDate, a.IsActive)
        ).ToListAsync(cancellationToken);
    }

    public async Task<DoctorClinicAssignmentDto> AssignToClinicAsync(
        Guid doctorId,
        AssignDoctorToClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_assignValidator, request, cancellationToken);

        var doctor = await GetRequiredAsync(doctorId, cancellationToken);
        if (!doctor.IsActive)
            throw new AppException("doctors.inactive", "Inactive doctor cannot be assigned to a clinic.", 400);

        var clinic = await OrgClinics().SingleOrDefaultAsync(c => c.Id == request.ClinicId, cancellationToken)
            ?? throw new AppException("clinics.not_found", "Clinic was not found.", 404);

        if (clinic.OrganizationId != doctor.OrganizationId)
            throw new AppException("doctors.cross_organization", "Cross-organization assignment is forbidden.", 403);

        if (!clinic.IsActive)
            throw new AppException("clinics.inactive", "Inactive clinic cannot receive active doctor assignments.", 400);

        var duplicate = await _db.DoctorClinicAssignments.AnyAsync(a =>
            a.OrganizationId == _org.OrganizationId &&
            a.DoctorId == doctorId &&
            a.ClinicId == request.ClinicId &&
            a.IsActive, cancellationToken);

        if (duplicate)
            throw new AppException("doctors.assignment_exists", "An active assignment to this clinic already exists.", 409);

        try
        {
            var assignment = DoctorClinicAssignment.Create(
                _org.OrganizationId,
                _org.BranchId,
                doctorId,
                request.ClinicId,
                request.StartDate,
                request.EndDate,
                _user.UserId,
                _clock.UtcNow);

            _db.DoctorClinicAssignments.Add(assignment);
            doctor.NotifyAssignedToClinic(request.ClinicId, assignment.Id, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(doctor, cancellationToken);

            return new DoctorClinicAssignmentDto(
                assignment.Id, doctorId, clinic.Id, clinic.Code, clinic.Name,
                assignment.StartDate, assignment.EndDate, assignment.IsActive);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("doctors.assignment_invalid", ex.Message, 400);
        }
    }

    public async Task DeactivateClinicAssignmentAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        _ = await GetRequiredAsync(doctorId, cancellationToken);

        var assignment = await _db.DoctorClinicAssignments.SingleOrDefaultAsync(a =>
            a.OrganizationId == _org.OrganizationId &&
            a.DoctorId == doctorId &&
            a.ClinicId == clinicId &&
            a.IsActive, cancellationToken);

        if (assignment is null)
            throw new AppException("doctors.assignment_not_found", "Active clinic assignment was not found.", 404);

        assignment.Deactivate(_user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Doctor> OrgDoctors() =>
        _db.Doctors.Where(d => d.OrganizationId == _org.OrganizationId);

    private IQueryable<Domain.Clinics.Clinic> OrgClinics() =>
        _db.Clinics.Where(c => c.OrganizationId == _org.OrganizationId);

    private async Task<Doctor> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await OrgDoctors().SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (doctor is null)
            throw new AppException("doctors.not_found", "Doctor was not found.", 404);
        return doctor;
    }

    private async Task EnsureSpecialtyAsync(Guid? specialtyId, CancellationToken cancellationToken)
    {
        if (!specialtyId.HasValue) return;
        var exists = await _db.Specialties.AnyAsync(s =>
            s.Id == specialtyId && s.OrganizationId == _org.OrganizationId && s.IsActive, cancellationToken);
        if (!exists)
            throw new AppException("specialties.not_found", "Specialty was not found or is inactive.", 400);
    }

    private async Task<DoctorDto> MapDoctorAsync(Doctor doctor, CancellationToken cancellationToken)
    {
        string? specialtyName = null;
        if (doctor.SpecialtyId.HasValue)
        {
            specialtyName = await _db.Specialties.AsNoTracking()
                .Where(s => s.Id == doctor.SpecialtyId && s.OrganizationId == _org.OrganizationId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new DoctorDto(
            doctor.Id, doctor.OrganizationId, doctor.BranchId, doctor.DoctorNumber, doctor.UserId,
            doctor.FirstName, doctor.LastName, doctor.DisplayName, doctor.SpecialtyId, specialtyName,
            doctor.LicenseNumber, doctor.PhoneNumber, doctor.Email, doctor.IsActive,
            doctor.CreatedAtUtc, doctor.CreatedBy, doctor.UpdatedAtUtc, doctor.UpdatedBy);
    }

    private async Task DispatchAsync(Doctor doctor, CancellationToken cancellationToken)
    {
        var events = doctor.DomainEvents.ToList();
        doctor.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new AppException("doctors.validation_failed", string.Join(" ", result.Errors.Select(e => e.ErrorMessage)), 400);
    }
}
