using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Procurement.Application.Common;
using ErpClink.Modules.Procurement.Application.PurchaseOrders;
using ErpClink.Modules.Procurement.Application.PurchaseOrders.Models;
using ErpClink.Modules.Procurement.Domain.PurchaseOrders;
using ErpClink.Modules.Procurement.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Procurement.Infrastructure.PurchaseOrders;

public sealed class PurchaseOrderService : IPurchaseOrderService
{
    private readonly ProcurementDbContext _db;
    private readonly IPurchaseOrderNumberGenerator _numbers;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IProcurementDomainEventDispatcher _events;
    private readonly IValidator<CreateDraftPurchaseOrderRequest> _createValidator;
    private readonly IValidator<UpdateDraftPurchaseOrderRequest> _updateValidator;
    private readonly IValidator<AddPurchaseOrderLineRequest> _addLineValidator;
    private readonly IValidator<UpdatePurchaseOrderLineRequest> _updateLineValidator;
    private readonly IValidator<SetPurchaseOrderDiscountRequest> _setDiscountValidator;
    private readonly IValidator<CancelPurchaseOrderRequest> _cancelValidator;

    public PurchaseOrderService(
        ProcurementDbContext db,
        IPurchaseOrderNumberGenerator numbers,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IProcurementDomainEventDispatcher events,
        IValidator<CreateDraftPurchaseOrderRequest> createValidator,
        IValidator<UpdateDraftPurchaseOrderRequest> updateValidator,
        IValidator<AddPurchaseOrderLineRequest> addLineValidator,
        IValidator<UpdatePurchaseOrderLineRequest> updateLineValidator,
        IValidator<SetPurchaseOrderDiscountRequest> setDiscountValidator,
        IValidator<CancelPurchaseOrderRequest> cancelValidator)
    {
        _db = db;
        _numbers = numbers;
        _org = org;
        _user = user;
        _clock = clock;
        _events = events;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _addLineValidator = addLineValidator;
        _updateLineValidator = updateLineValidator;
        _setDiscountValidator = setDiscountValidator;
        _cancelValidator = cancelValidator;
    }

    public async Task<PurchaseOrderDto> CreateDraftAsync(CreateDraftPurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("procurement.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        await EnsureSupplierAsync(request.SupplierId, cancellationToken);
        var number = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
        var lineInputs = MapLines(request.Lines);

        var po = PurchaseOrder.CreateDraft(
            _org.OrganizationId,
            _org.BranchId,
            number,
            request.SupplierId,
            request.OrderDate,
            request.CurrencyCode,
            request.Notes,
            lineInputs,
            _user.UserId,
            _clock.UtcNow);

        if (request.DiscountAmount is > 0)
            po.SetDiscount(request.DiscountAmount.Value, _user.UserId, _clock.UtcNow);

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync(cancellationToken);
        await DispatchAsync(po, cancellationToken);
        return Map(po);
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var po = await OrgPurchaseOrders().AsNoTracking()
            .Include(p => p.Lines)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        return po is null ? null : Map(po);
    }

    public async Task<PurchaseOrderDto?> GetByNumberAsync(string purchaseOrderNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(purchaseOrderNumber))
            throw new AppException("procurement.invalid_request", "Purchase order number is required.", 400);

        var po = await OrgPurchaseOrders().AsNoTracking()
            .Include(p => p.Lines)
            .SingleOrDefaultAsync(p => p.PurchaseOrderNumber == purchaseOrderNumber.Trim(), cancellationToken);
        return po is null ? null : Map(po);
    }

    public async Task<PagedPurchaseOrdersResult> SearchAsync(SearchPurchaseOrdersRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        IQueryable<PurchaseOrder> query = OrgPurchaseOrders().AsNoTracking().Include(p => p.Lines);

        if (request.SupplierId.HasValue) query = query.Where(p => p.SupplierId == request.SupplierId);
        if (!string.IsNullOrWhiteSpace(request.PurchaseOrderNumber))
            query = query.Where(p => p.PurchaseOrderNumber.Contains(request.PurchaseOrderNumber.Trim()));
        if (request.DateFrom.HasValue) query = query.Where(p => p.OrderDate >= request.DateFrom);
        if (request.DateTo.HasValue) query = query.Where(p => p.OrderDate <= request.DateTo);
        if (TryParseStatus(request.Status, out var status)) query = query.Where(p => p.Status == status);

        query = query.OrderByDescending(p => p.OrderDate).ThenByDescending(p => p.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var page = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedPurchaseOrdersResult(page.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<PurchaseOrderDto> UpdateDraftAsync(Guid id, UpdateDraftPurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("procurement.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        await EnsureSupplierAsync(request.SupplierId, cancellationToken);
        var po = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(po, request.RowVersion);
        try
        {
            po.UpdateDraftHeader(request.SupplierId, request.OrderDate, request.Notes, _user.UserId, _clock.UtcNow);
            if (request.DiscountAmount.HasValue)
                po.SetDiscount(request.DiscountAmount.Value, _user.UserId, _clock.UtcNow);

            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(po, cancellationToken);
            return Map(po);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("procurement.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Purchase order was modified by another operation.", 409);
        }
    }

    public async Task<PurchaseOrderDto> AddLineAsync(Guid id, AddPurchaseOrderLineRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _addLineValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("procurement.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var po = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(po, request.RowVersion);
        try
        {
            po.AddLine(MapLine(request.Line), _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(po, cancellationToken);
            return Map(po);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("procurement.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Purchase order was modified by another operation.", 409);
        }
    }

    public async Task<PurchaseOrderDto> UpdateLineAsync(
        Guid id,
        Guid lineId,
        UpdatePurchaseOrderLineRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateLineValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("procurement.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var po = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(po, request.RowVersion);
        try
        {
            po.UpdateLine(lineId, request.Quantity, request.LineDiscountAmount, request.SortOrder, _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(po, cancellationToken);
            return Map(po);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("procurement.invalid_transition", ex.Message, 400);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Purchase order was modified by another operation.", 409);
        }
    }

    public async Task<PurchaseOrderDto> RemoveLineAsync(Guid id, Guid lineId, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        var po = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(po, rowVersion);
        try
        {
            var line = po.Lines.SingleOrDefault(l => l.Id == lineId);
            po.RemoveLine(lineId, _user.UserId, _clock.UtcNow);
            if (line is not null)
                _db.PurchaseOrderLines.Remove(line);

            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(po, cancellationToken);
            return Map(po);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("procurement.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Purchase order was modified by another operation.", 409);
        }
    }

    public async Task<PurchaseOrderDto> SetDiscountAsync(Guid id, SetPurchaseOrderDiscountRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _setDiscountValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("procurement.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var po = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(po, request.RowVersion);
        try
        {
            po.SetDiscount(request.DiscountAmount, _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(po, cancellationToken);
            return Map(po);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("procurement.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Purchase order was modified by another operation.", 409);
        }
    }

    public async Task<PurchaseOrderDto> SubmitAsync(Guid id, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        var po = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(po, rowVersion);
        try
        {
            po.Submit(_user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(po, cancellationToken);
            return Map(po);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("without lines", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("procurement.empty_purchase_order", ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("procurement.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Purchase order was modified by another operation.", 409);
        }
    }

    public async Task<PurchaseOrderDto> ApproveAsync(Guid id, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        var po = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(po, rowVersion);
        try
        {
            po.Approve(_user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(po, cancellationToken);
            return Map(po);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("procurement.invalid_transition", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Purchase order was modified by another operation.", 409);
        }
    }

    public async Task<PurchaseOrderDto> CancelAsync(Guid id, CancelPurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("procurement.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var po = await GetRequiredAsync(id, cancellationToken);
        ApplyRowVersion(po, request.RowVersion);
        try
        {
            po.Cancel(request.Reason, _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await DispatchAsync(po, cancellationToken);
            return Map(po);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("procurement.invalid_transition", ex.Message, 400);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Purchase order was modified by another operation.", 409);
        }
    }

    private async Task EnsureSupplierAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        var exists = await _db.Suppliers.AsNoTracking()
            .AnyAsync(s => s.Id == supplierId && s.OrganizationId == _org.OrganizationId, cancellationToken);
        if (!exists)
            throw new AppException("procurement.supplier_not_found", "Supplier was not found.", 404);
    }

    private IQueryable<PurchaseOrder> OrgPurchaseOrders() =>
        _db.PurchaseOrders.Where(p => p.OrganizationId == _org.OrganizationId);

    private async Task<PurchaseOrder> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var po = await OrgPurchaseOrders()
            .Include(p => p.Lines)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (po is null)
            throw new AppException("procurement.purchase_order_not_found", "Purchase order was not found.", 404);
        return po;
    }

    private void ApplyRowVersion(PurchaseOrder po, byte[]? rowVersion)
    {
        if (rowVersion is { Length: > 0 })
            _db.Entry(po).Property(p => p.RowVersion).OriginalValue = rowVersion;
    }

    private async Task DispatchAsync(PurchaseOrder po, CancellationToken cancellationToken)
    {
        var events = po.DomainEvents.ToList();
        po.ClearDomainEvents();
        if (events.Count > 0)
            await _events.DispatchAsync(events, cancellationToken);
    }

    private static bool TryParseStatus(string? value, out PurchaseOrderStatus status) =>
        Enum.TryParse(value, true, out status);

    private static IReadOnlyList<PurchaseOrderLineInput> MapLines(IReadOnlyList<PurchaseOrderLineInputDto>? lines) =>
        lines?.Select(MapLine).ToList() ?? [];

    private static PurchaseOrderLineInput MapLine(PurchaseOrderLineInputDto dto) =>
        new(dto.CatalogItemId, dto.DescriptionSnapshot, dto.Quantity, dto.UnitCost, dto.LineDiscountAmount ?? 0, dto.SortOrder);

    private static PurchaseOrderDto Map(PurchaseOrder p) =>
        new(
            p.Id, p.OrganizationId, p.BranchId, p.PurchaseOrderNumber, p.SupplierId, p.OrderDate,
            p.Status.ToString(), p.CurrencyCode, p.SubTotal, p.DiscountAmount, p.TaxAmount, p.TotalAmount, p.Notes,
            p.SubmittedAtUtc, p.SubmittedBy, p.ApprovedAtUtc, p.ApprovedBy, p.CancelledAtUtc, p.CancelledBy,
            p.CancellationReason, p.CreatedAtUtc, p.CreatedBy, p.UpdatedAtUtc, p.UpdatedBy, p.RowVersion,
            p.Lines.OrderBy(l => l.SortOrder).Select(l => new PurchaseOrderLineDto(
                l.Id, l.CatalogItemId, l.DescriptionSnapshot, l.Quantity, l.QuantityReceived, l.UnitCost, l.LineDiscountAmount,
                l.LineSubtotal, l.LineTotal, l.SortOrder)).ToList());
}
