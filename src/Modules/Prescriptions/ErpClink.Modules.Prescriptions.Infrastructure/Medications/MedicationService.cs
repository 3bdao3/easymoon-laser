using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Prescriptions.Application.Common;
using ErpClink.Modules.Prescriptions.Application.Medications;
using ErpClink.Modules.Prescriptions.Application.Medications.Models;
using ErpClink.Modules.Prescriptions.Domain.Medications;
using ErpClink.Modules.Prescriptions.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Prescriptions.Infrastructure.Medications;

public sealed class MedicationService : IMedicationService
{
    private readonly PrescriptionsDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IPrescriptionsDomainEventDispatcher _events;
    private readonly IValidator<CreateMedicationRequest> _createValidator;
    private readonly IValidator<UpdateMedicationRequest> _updateValidator;

    public MedicationService(
        PrescriptionsDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IPrescriptionsDomainEventDispatcher events,
        IValidator<CreateMedicationRequest> createValidator,
        IValidator<UpdateMedicationRequest> updateValidator)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<MedicationDto> CreateAsync(CreateMedicationRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("medications.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var medication = Medication.Create(
            _org.OrganizationId, request.Code, request.Name, request.GenericName,
            request.Strength, request.DosageForm, request.Route, _user.UserId, _clock.UtcNow);

        try
        {
            _db.Medications.Add(medication);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(medication, cancellationToken);
            return Map(medication);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new AppException("medications.duplicate_code", "Medication code already exists.", 409);
        }
    }

    public async Task<MedicationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var medication = await OrgMedications().AsNoTracking().SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
        return medication is null ? null : Map(medication);
    }

    public async Task<PagedMedicationsResult> SearchAsync(SearchMedicationsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgMedications().AsNoTracking();

        if (request.ActiveOnly == true)
            query = query.Where(m => m.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Code))
            query = query.Where(m => m.Code.Contains(request.Code.Trim()));
        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(m => m.Name.Contains(request.Name.Trim()));
        if (!string.IsNullOrWhiteSpace(request.GenericName))
            query = query.Where(m => m.GenericName != null && m.GenericName.Contains(request.GenericName.Trim()));
        if (!string.IsNullOrWhiteSpace(request.Strength))
            query = query.Where(m => m.Strength != null && m.Strength.Contains(request.Strength.Trim()));
        if (!string.IsNullOrWhiteSpace(request.DosageForm))
            query = query.Where(m => m.DosageForm != null && m.DosageForm.Contains(request.DosageForm.Trim()));
        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var q = request.Q.Trim();
            query = query.Where(m =>
                m.Code.Contains(q) ||
                m.Name.Contains(q) ||
                (m.GenericName != null && m.GenericName.Contains(q)));
        }

        query = query.OrderBy(m => m.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedMedicationsResult(items.Select(Map).ToList(), paging.NormalizedPage, paging.NormalizedPageSize, total);
    }

    public async Task<MedicationDto> UpdateAsync(Guid id, UpdateMedicationRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("medications.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var medication = await GetRequiredAsync(id, cancellationToken);
        medication.Update(request.Name, request.GenericName, request.Strength, request.DosageForm, request.Route, _user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        await DispatchAsync(medication, cancellationToken);
        return Map(medication);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var medication = await GetRequiredAsync(id, cancellationToken);
        medication.Activate(_user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        await DispatchAsync(medication, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var medication = await GetRequiredAsync(id, cancellationToken);
        medication.Deactivate(_user.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        await DispatchAsync(medication, cancellationToken);
    }

    private IQueryable<Medication> OrgMedications() =>
        _db.Medications.Where(m => m.OrganizationId == _org.OrganizationId);

    private async Task<Medication> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var medication = await OrgMedications().SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (medication is null)
            throw new AppException("medications.not_found", "Medication was not found.", 404);
        return medication;
    }

    private async Task DispatchAsync(Medication medication, CancellationToken cancellationToken)
    {
        var events = medication.DomainEvents.ToList();
        medication.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static MedicationDto Map(Medication m) =>
        new(m.Id, m.OrganizationId, m.Code, m.Name, m.GenericName, m.Strength, m.DosageForm, m.Route,
            m.IsActive, m.CreatedAtUtc, m.CreatedBy, m.UpdatedAtUtc, m.UpdatedBy);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
