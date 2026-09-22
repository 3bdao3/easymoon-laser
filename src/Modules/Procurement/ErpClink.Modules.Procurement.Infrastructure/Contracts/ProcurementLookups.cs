using ErpClink.BuildingBlocks.Application.Abstractions;

using ErpClink.BuildingBlocks.Application.Common;

using ErpClink.Modules.Procurement.Application.Contracts;

using ErpClink.Modules.Procurement.Domain.PurchaseOrders;

using ErpClink.Modules.Procurement.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;



namespace ErpClink.Modules.Procurement.Infrastructure.Contracts;



public sealed class SupplierLookup : ISupplierLookup

{

    private readonly ProcurementDbContext _db;

    private readonly IOrganizationContext _org;



    public SupplierLookup(ProcurementDbContext db, IOrganizationContext org)

    {

        _db = db;

        _org = org;

    }



    public async Task<SupplierSummaryLookup?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken = default)

    {

        return await _db.Suppliers.AsNoTracking()

            .Where(s => s.Id == supplierId && s.OrganizationId == _org.OrganizationId)

            .Select(s => new SupplierSummaryLookup(s.Id, s.OrganizationId, s.SupplierCode, s.Name, s.IsActive))

            .SingleOrDefaultAsync(cancellationToken);

    }

}



public sealed class PurchaseOrderLookup : IPurchaseOrderLookup

{

    private readonly ProcurementDbContext _db;

    private readonly IOrganizationContext _org;



    public PurchaseOrderLookup(ProcurementDbContext db, IOrganizationContext org)

    {

        _db = db;

        _org = org;

    }



    public async Task<PurchaseOrderDetailLookup?> GetApprovedByIdAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)

    {

        var receivable = new[]
        {
            PurchaseOrderStatus.Approved,
            PurchaseOrderStatus.Ordered,
            PurchaseOrderStatus.PartiallyReceived,
            PurchaseOrderStatus.Received
        };

        var po = await _db.PurchaseOrders.AsNoTracking()

            .Include(p => p.Lines)

            .Where(p => p.Id == purchaseOrderId

                        && p.OrganizationId == _org.OrganizationId

                        && receivable.Contains(p.Status))

            .SingleOrDefaultAsync(cancellationToken);



        if (po is null)

            return null;



        return new PurchaseOrderDetailLookup(

            po.Id,

            po.PurchaseOrderNumber,

            po.SupplierId,

            po.Status.ToString(),

            po.Lines.OrderBy(l => l.SortOrder).Select(l => new PurchaseOrderLineLookup(

                l.Id, l.CatalogItemId, l.DescriptionSnapshot, l.Quantity, l.QuantityReceived, l.UnitCost)).ToList());

    }

}



public sealed class PurchaseOrderReceivingPort : IPurchaseOrderReceivingPort

{

    private readonly ProcurementDbContext _db;

    private readonly IOrganizationContext _org;

    private readonly ICurrentUser _user;

    private readonly IBusinessClock _clock;



    private static readonly PurchaseOrderStatus[] ReceivableStatuses =

    [

        PurchaseOrderStatus.Approved,

        PurchaseOrderStatus.Ordered,

        PurchaseOrderStatus.PartiallyReceived

    ];



    public PurchaseOrderReceivingPort(

        ProcurementDbContext db,

        IOrganizationContext org,

        ICurrentUser user,

        IBusinessClock clock)

    {

        _db = db;

        _org = org;

        _user = user;

        _clock = clock;

    }



    public async Task<PurchaseOrderReceivingSnapshot?> GetReceivableAsync(

        Guid purchaseOrderId,

        CancellationToken cancellationToken = default)

    {

        var po = await _db.PurchaseOrders.AsNoTracking()

            .Include(p => p.Lines)

            .Where(p => p.Id == purchaseOrderId

                        && p.OrganizationId == _org.OrganizationId

                        && ReceivableStatuses.Contains(p.Status))

            .SingleOrDefaultAsync(cancellationToken);



        if (po is null)

            return null;



        return MapSnapshot(po);

    }



    public async Task ApplyReceiptAsync(ApplyPurchaseOrderReceiptRequest request, CancellationToken cancellationToken = default)

    {

        var po = await _db.PurchaseOrders

            .Include(p => p.Lines)

            .Where(p => p.Id == request.PurchaseOrderId && p.OrganizationId == _org.OrganizationId)

            .SingleOrDefaultAsync(cancellationToken);



        if (po is null)

            throw new AppException("procurement.purchase_order_not_found", "Purchase order was not found.", 404);



        if (request.PurchaseOrderRowVersion is { Length: > 0 })

            _db.Entry(po).Property(p => p.RowVersion).OriginalValue = request.PurchaseOrderRowVersion;



        try

        {

            var deltas = request.Lines

                .Select(l => (l.PurchaseOrderLineId, l.QuantityReceivedDelta))

                .ToList();

            po.ApplyReceipt(deltas, _user.UserId, _clock.UtcNow);

            await _db.SaveChangesAsync(cancellationToken);

        }

        catch (InvalidOperationException ex)

        {

            throw new AppException("procurement.invalid_receipt", ex.Message, 409);

        }

        catch (DbUpdateConcurrencyException)

        {

            throw new AppException("procurement.concurrency_conflict", "Purchase order was modified by another operation.", 409);

        }

    }

    public async Task ReverseReceiptAsync(ApplyPurchaseOrderReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Lines)
            .Where(p => p.Id == request.PurchaseOrderId && p.OrganizationId == _org.OrganizationId)
            .SingleOrDefaultAsync(cancellationToken);

        if (po is null)
            throw new AppException("procurement.purchase_order_not_found", "Purchase order was not found.", 404);

        try
        {
            var deltas = request.Lines
                .Select(l => (l.PurchaseOrderLineId, l.QuantityReceivedDelta))
                .ToList();
            po.ReverseReceipt(deltas, _user.UserId, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("procurement.invalid_receipt", ex.Message, 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("procurement.concurrency_conflict", "Purchase order was modified by another operation.", 409);
        }
    }

    private static PurchaseOrderReceivingSnapshot MapSnapshot(PurchaseOrder po) =>

        new(

            po.Id,

            po.OrganizationId,

            po.BranchId,

            po.PurchaseOrderNumber,

            po.SupplierId,

            po.Status.ToString(),

            po.Lines.OrderBy(l => l.SortOrder).Select(l => new PurchaseOrderReceivingLineSnapshot(

                l.Id,

                l.CatalogItemId,

                l.DescriptionSnapshot,

                l.Quantity,

                l.QuantityReceived,

                l.UnitCost)).ToList(),

            po.RowVersion);

}

