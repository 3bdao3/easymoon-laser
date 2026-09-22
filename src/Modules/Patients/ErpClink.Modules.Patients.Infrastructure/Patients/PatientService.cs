using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Patients.Application.Common;
using ErpClink.Modules.Patients.Application.Patients;
using ErpClink.Modules.Patients.Application.Patients.Models;
using ErpClink.Modules.Patients.Domain.Patients;
using ErpClink.Modules.Patients.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Patients.Infrastructure.Patients;

public sealed class PatientService : IPatientService
{
    private readonly PatientsDbContext _dbContext;
    private readonly IPatientNumberGenerator _patientNumberGenerator;
    private readonly IOrganizationContext _organizationContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IValidator<RegisterPatientRequest> _registerValidator;
    private readonly IValidator<UpdatePatientRequest> _updateValidator;
    private readonly IValidator<AddAllergyRequest> _addAllergyValidator;
    private readonly IValidator<UpdateAllergyRequest> _updateAllergyValidator;

    public PatientService(
        PatientsDbContext dbContext,
        IPatientNumberGenerator patientNumberGenerator,
        IOrganizationContext organizationContext,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IDomainEventDispatcher domainEventDispatcher,
        IValidator<RegisterPatientRequest> registerValidator,
        IValidator<UpdatePatientRequest> updateValidator,
        IValidator<AddAllergyRequest> addAllergyValidator,
        IValidator<UpdateAllergyRequest> updateAllergyValidator)
    {
        _dbContext = dbContext;
        _patientNumberGenerator = patientNumberGenerator;
        _organizationContext = organizationContext;
        _currentUser = currentUser;
        _clock = clock;
        _domainEventDispatcher = domainEventDispatcher;
        _registerValidator = registerValidator;
        _updateValidator = updateValidator;
        _addAllergyValidator = addAllergyValidator;
        _updateAllergyValidator = updateAllergyValidator;
    }

    public async Task<RegisterPatientResult> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_registerValidator, request, cancellationToken);

        var organizationId = _organizationContext.OrganizationId;
        var branchId = _organizationContext.BranchId;

        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            var nationalIdExists = await OrganizationPatients()
                .AnyAsync(p => p.NationalId == request.NationalId.Trim(), cancellationToken);
            if (nationalIdExists)
            {
                throw new AppException("patients.national_id_exists", "A patient with this national ID already exists in the organization.", 409);
            }
        }

        var possibleDuplicates = await FindPossibleDuplicatesAsync(organizationId, request, cancellationToken);
        if (possibleDuplicates.Count > 0 && !request.AllowPossibleDuplicate)
        {
            throw new AppException(
                "patients.possible_duplicate",
                "Possible duplicate patient(s) found. Review matches or resubmit with AllowPossibleDuplicate=true.",
                409);
        }

        var patientNumber = await _patientNumberGenerator.GenerateAsync(organizationId, cancellationToken);

        try
        {
            var patient = Patient.Register(
                organizationId,
                branchId,
                patientNumber,
                request.FirstName,
                request.MiddleName,
                request.LastName,
                request.DateOfBirth,
                request.Gender,
                request.NationalId,
                request.PhoneNumber,
                request.Email,
                request.AddressLine1,
                request.AddressLine2,
                request.City,
                request.EmergencyContactName,
                request.EmergencyContactPhone,
                _currentUser.UserId,
                _clock.UtcNow);

            _dbContext.Patients.Add(patient);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await DispatchAndClearAsync(patient, cancellationToken);

            return new RegisterPatientResult(MapPatient(patient), possibleDuplicates);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("patients.register_failed", ex.Message, 400);
        }
    }

    public async Task<PatientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await OrganizationPatients()
            .Include(p => p.Allergies)
            .Include(p => p.MedicalHistory)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        return patient is null ? null : MapPatient(patient);
    }

    public async Task<PatientDto?> GetByPatientNumberAsync(string patientNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(patientNumber))
        {
            throw new AppException("patients.invalid_request", "Patient number is required.", 400);
        }

        var patient = await OrganizationPatients()
            .Include(p => p.Allergies)
            .Include(p => p.MedicalHistory)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.PatientNumber == patientNumber.Trim(), cancellationToken);

        return patient is null ? null : MapPatient(patient);
    }

    public async Task<PagedPatientsResult> SearchAsync(SearchPatientsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrganizationPatients().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var term = request.Query.Trim();
            query = query.Where(p =>
                p.PatientNumber.Contains(term) ||
                p.FirstName.Contains(term) ||
                p.LastName.Contains(term) ||
                p.PhoneNumber.Contains(term) ||
                (p.NationalId != null && p.NationalId.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.PatientNumber))
        {
            var value = request.PatientNumber.Trim();
            query = query.Where(p => p.PatientNumber.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            var value = request.FirstName.Trim();
            query = query.Where(p => p.FirstName.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            var value = request.LastName.Trim();
            query = query.Where(p => p.LastName.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var value = request.PhoneNumber.Trim();
            query = query.Where(p => p.PhoneNumber.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            var value = request.NationalId.Trim();
            query = query.Where(p => p.NationalId != null && p.NationalId.Contains(value));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        query = ApplySort(query, request.SortBy, request.SortDescending);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .Select(p => new PatientListItemDto(
                p.Id,
                p.PatientNumber,
                p.MiddleName == null || p.MiddleName == ""
                    ? p.FirstName + " " + p.LastName
                    : p.FirstName + " " + p.MiddleName + " " + p.LastName,
                p.DateOfBirth,
                p.PhoneNumber,
                p.NationalId,
                p.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedPatientsResult(items, paging.NormalizedPage, paging.NormalizedPageSize, total);
    }

    public async Task<PatientDto> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);
        var patient = await GetRequiredPatientAsync(id, includeChildren: false, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            var nationalId = request.NationalId.Trim();
            var conflict = await OrganizationPatients()
                .AnyAsync(p => p.Id != id && p.NationalId == nationalId, cancellationToken);
            if (conflict)
            {
                throw new AppException("patients.national_id_exists", "A patient with this national ID already exists in the organization.", 409);
            }
        }

        try
        {
            patient.UpdateDemographics(
                request.FirstName,
                request.MiddleName,
                request.LastName,
                request.DateOfBirth,
                request.Gender,
                request.NationalId,
                request.PhoneNumber,
                request.Email,
                request.AddressLine1,
                request.AddressLine2,
                request.City,
                request.EmergencyContactName,
                request.EmergencyContactPhone,
                _currentUser.UserId,
                _clock.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("patients.update_failed", ex.Message, 400);
        }

        return (await GetByIdAsync(id, cancellationToken))!;
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await GetRequiredPatientAsync(id, includeChildren: false, cancellationToken);
        patient.Activate(_currentUser.UserId, _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await GetRequiredPatientAsync(id, includeChildren: false, cancellationToken);
        patient.Deactivate(_currentUser.UserId, _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AllergyDto>> GetAllergiesAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        _ = await GetRequiredPatientAsync(patientId, includeChildren: false, cancellationToken);

        return await _dbContext.PatientAllergies
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.IsActive)
            .ThenBy(a => a.Name)
            .Select(a => new AllergyDto(a.Id, a.Name, a.Reaction, a.Severity, a.Notes, a.IsActive, a.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<AllergyDto> AddAllergyAsync(Guid patientId, AddAllergyRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_addAllergyValidator, request, cancellationToken);
        var patient = await GetRequiredPatientAsync(patientId, includeChildren: false, cancellationToken);

        if (!patient.IsActive)
        {
            throw new AppException("patients.allergy_add_failed", "Patient is inactive.", 400);
        }

        var allergy = PatientAllergy.Create(
            patient.Id,
            request.Name,
            request.Reaction,
            request.Severity,
            request.Notes,
            _currentUser.UserId,
            _clock.UtcNow);

        _dbContext.PatientAllergies.Add(allergy);
        patient.Touch(_currentUser.UserId, _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AllergyDto(allergy.Id, allergy.Name, allergy.Reaction, allergy.Severity, allergy.Notes, allergy.IsActive, allergy.CreatedAtUtc);
    }

    public async Task<AllergyDto> UpdateAllergyAsync(
        Guid patientId,
        Guid allergyId,
        UpdateAllergyRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateAllergyValidator, request, cancellationToken);
        var patient = await GetRequiredPatientAsync(patientId, includeChildren: false, cancellationToken);

        if (!patient.IsActive)
        {
            throw new AppException("patients.allergy_update_failed", "Patient is inactive.", 400);
        }

        var allergy = await _dbContext.PatientAllergies
            .SingleOrDefaultAsync(a => a.PatientId == patientId && a.Id == allergyId, cancellationToken);

        if (allergy is null)
        {
            throw new AppException("patients.allergy_update_failed", "Allergy was not found for this patient.", 404);
        }

        try
        {
            allergy.Update(
                request.Name,
                request.Reaction,
                request.Severity,
                request.Notes,
                _currentUser.UserId,
                _clock.UtcNow);

            patient.Touch(_currentUser.UserId, _clock.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new AllergyDto(allergy.Id, allergy.Name, allergy.Reaction, allergy.Severity, allergy.Notes, allergy.IsActive, allergy.CreatedAtUtc);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("patients.allergy_update_failed", ex.Message, 400);
        }
    }

    public async Task DeactivateAllergyAsync(Guid patientId, Guid allergyId, CancellationToken cancellationToken = default)
    {
        var patient = await GetRequiredPatientAsync(patientId, includeChildren: false, cancellationToken);

        var allergy = await _dbContext.PatientAllergies
            .SingleOrDefaultAsync(a => a.PatientId == patientId && a.Id == allergyId, cancellationToken);

        if (allergy is null)
        {
            throw new AppException("patients.allergy_deactivate_failed", "Allergy was not found for this patient.", 404);
        }

        allergy.Deactivate(_currentUser.UserId, _clock.UtcNow);
        patient.Touch(_currentUser.UserId, _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Patient> OrganizationPatients() =>
        _dbContext.Patients.Where(p => p.OrganizationId == _organizationContext.OrganizationId);

    private async Task<Patient> GetRequiredPatientAsync(Guid id, bool includeChildren, CancellationToken cancellationToken)
    {
        IQueryable<Patient> query = OrganizationPatients();
        if (includeChildren)
        {
            query = query.Include(p => p.Allergies).Include(p => p.MedicalHistory).AsSplitQuery();
        }

        var patient = await query.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (patient is null)
        {
            // Cross-org and missing patients both return not found (no existence leak).
            throw new AppException("patients.not_found", "Patient was not found.", 404);
        }

        return patient;
    }

    private async Task<IReadOnlyList<PatientListItemDto>> FindPossibleDuplicatesAsync(
        Guid organizationId,
        RegisterPatientRequest request,
        CancellationToken cancellationToken)
    {
        var phone = request.PhoneNumber.Trim();
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();

        return await _dbContext.Patients
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId)
            .Where(p =>
                p.PhoneNumber == phone ||
                (p.FirstName == firstName && p.LastName == lastName && p.DateOfBirth == request.DateOfBirth))
            .OrderBy(p => p.PatientNumber)
            .Take(10)
            .Select(p => new PatientListItemDto(
                p.Id,
                p.PatientNumber,
                p.MiddleName == null || p.MiddleName == ""
                    ? p.FirstName + " " + p.LastName
                    : p.FirstName + " " + p.MiddleName + " " + p.LastName,
                p.DateOfBirth,
                p.PhoneNumber,
                p.NationalId,
                p.IsActive))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Patient> ApplySort(IQueryable<Patient> query, string? sortBy, bool descending)
    {
        return (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "patientnumber" => descending ? query.OrderByDescending(p => p.PatientNumber) : query.OrderBy(p => p.PatientNumber),
            "firstname" => descending ? query.OrderByDescending(p => p.FirstName) : query.OrderBy(p => p.FirstName),
            "lastname" => descending ? query.OrderByDescending(p => p.LastName) : query.OrderBy(p => p.LastName),
            "phone" => descending ? query.OrderByDescending(p => p.PhoneNumber) : query.OrderBy(p => p.PhoneNumber),
            "dob" or "dateofbirth" => descending ? query.OrderByDescending(p => p.DateOfBirth) : query.OrderBy(p => p.DateOfBirth),
            _ => query.OrderByDescending(p => p.CreatedAtUtc)
        };
    }

    private async Task DispatchAndClearAsync(Patient patient, CancellationToken cancellationToken)
    {
        var events = patient.DomainEvents.ToList();
        patient.ClearDomainEvents();
        if (events.Count > 0)
        {
            await _domainEventDispatcher.DispatchAsync(events, cancellationToken);
        }
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var message = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
        throw new AppException("patients.validation_failed", message, 400);
    }

    private static PatientDto MapPatient(Patient patient) =>
        new(
            patient.Id,
            patient.OrganizationId,
            patient.BranchId,
            patient.PatientNumber,
            patient.FirstName,
            patient.MiddleName,
            patient.LastName,
            patient.FullName,
            patient.DateOfBirth,
            patient.Gender,
            patient.NationalId,
            patient.PhoneNumber,
            patient.Email,
            patient.AddressLine1,
            patient.AddressLine2,
            patient.City,
            patient.EmergencyContactName,
            patient.EmergencyContactPhone,
            patient.IsActive,
            patient.CreatedAtUtc,
            patient.CreatedBy,
            patient.UpdatedAtUtc,
            patient.UpdatedBy,
            patient.Allergies
                .OrderByDescending(a => a.IsActive)
                .ThenBy(a => a.Name)
                .Select(a => new AllergyDto(a.Id, a.Name, a.Reaction, a.Severity, a.Notes, a.IsActive, a.CreatedAtUtc))
                .ToList(),
            patient.MedicalHistory
                .Where(h => h.IsActive)
                .OrderByDescending(h => h.CreatedAtUtc)
                .Select(h => new MedicalHistoryItemDto(h.Id, h.Category, h.Description, h.IsActive, h.CreatedAtUtc))
                .ToList());
}
