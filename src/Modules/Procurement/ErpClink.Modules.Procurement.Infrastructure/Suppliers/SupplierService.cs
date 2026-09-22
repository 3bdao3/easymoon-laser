using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Procurement.Application.Common;
using ErpClink.Modules.Procurement.Application.Suppliers;
using ErpClink.Modules.Procurement.Application.Suppliers.Models;
using ErpClink.Modules.Procurement.Domain.Suppliers;
using ErpClink.Modules.Procurement.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Procurement.Infrastructure.Suppliers;

public sealed class SupplierService : ISupplierService
{
    private readonly ProcurementDbContext _db;
    private readonly ISupplierNumberGenerator _numbers;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IProcurementDomainEventDispatcher _events;
    private readonly IValidator<CreateSupplierRequest> _createValidator;
    private readonly IValidator<UpdateSupplierRequest> _updateValidator;

    public SupplierService(
        ProcurementDbContext db,
        ISupplierNumberGenerator numbers,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IProcurementDomainEventDispatcher events,
        IValidator<CreateSupplierRequest> createValidator,
        IValidator<UpdateSupplierRequest> updateValidator)
    {
        _db = db;
        _numbers = numbers;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("procurement.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var code = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
        var supplier = Supplier.Create(
            _org.OrganizationId,
            code,
            request.Name,
            request.ContactName,
            request.Phone,
            request.Email,
            request.Address,
            request.Notes,
            _user.UserId,
            _clock.UtcNow);

        _db.Suppliers.Add(supplier);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new AppException("procurement.duplicate_supplier_code", "Supplier code already exists.", 409);
        }

        await DispatchAsync(supplier, cancellationToken);
        return Map(supplier);
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await OrgSuppliers().AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        return supplier is null ? null : Map(supplier);
    }

    public async Task<SupplierDto?> GetByCodeAsync(string supplierCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(supplierCode))
            throw new AppException("procurement.invalid_request", "Supplier code is required.", 400);

        var supplier = await OrgSuppliers().AsNoTracking()
            .SingleOrDefaultAsync(s => s.SupplierCode == supplierCode.Trim(), cancellationToken);
        return supplier is null ? null : Map(supplier);
    }

    public async Task<PagedSuppliersResult> SearchAsync(SearchSuppliersRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = OrgSuppliers().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(s =>
                s.Name.Contains(term)
                || s.SupplierCode.Contains(term)
                || (s.ContactName != null && s.ContactName.Contains(term)));
        }

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        query = query.OrderBy(s => s.Name);
        var total = await query.CountAsync(cancellationToken);
        var page = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedSuppliersResult(page.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("procurement.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var supplier = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(supplier, request.RowVersion);
        try
        {
            supplier.Update(request.Name, request.ContactName, request.Phone, request.Email, request.Address, request.Notes,
                _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(supplier, cancellationToken);
            return Map(supplier);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Supplier was modified by another operation.", 409);
        }
    }

    public async Task ActivateAsync(Guid id, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        var supplier = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(supplier, rowVersion);
        try
        {
            supplier.Activate(_user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(supplier, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Supplier was modified by another operation.", 409);
        }
    }

    public async Task DeactivateAsync(Guid id, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        var supplier = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(supplier, rowVersion);
        try
        {
            supplier.Deactivate(_user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(supplier, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Supplier was modified by another operation.", 409);
        }
    }

    private IQueryable<Supplier> OrgSuppliers() =>
        _db.Suppliers.Where(s => s.OrganizationId == _org.OrganizationId);

    private async Task<Supplier> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await OrgSuppliers().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (supplier is null)
            throw new AppException("procurement.supplier_not_found", "Supplier was not found.", 404);
        return supplier;
    }

    private void ApplyRowVersion(Supplier supplier, byte[]? rowVersion)
    {
        if (rowVersion is { Length: > 0 })
            _db.Entry(supplier).Property(s => s.RowVersion).OriginalValue = rowVersion;
    }

    private async Task DispatchAsync(Supplier supplier, CancellationToken cancellationToken)
    {
        var events = supplier.DomainEvents.ToList();
        supplier.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static SupplierDto Map(Supplier s) =>
        new(s.Id, s.OrganizationId, s.SupplierCode, s.Name, s.ContactName, s.Phone, s.Email, s.Address, s.Notes,
            s.IsActive, s.CreatedAtUtc, s.CreatedBy, s.UpdatedAtUtc, s.UpdatedBy, s.RowVersion);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
