using ErpClink.Modules.Inventory.Application.Categories.Models;
using ErpClink.Modules.Inventory.Application.GoodsReceipts.Models;
using ErpClink.Modules.Inventory.Application.Items.Models;
using ErpClink.Modules.Inventory.Application.Stock.Models;
using ErpClink.Modules.Inventory.Application.Warehouses.Models;
using FluentValidation;

namespace ErpClink.Modules.Inventory.Application.Validators;

public sealed class CreateWarehouseRequestValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
}

public sealed class UpdateWarehouseRequestValidator : AbstractValidator<UpdateWarehouseRequest>
{
    public UpdateWarehouseRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
}

public sealed class CreateInventoryCategoryRequestValidator : AbstractValidator<CreateInventoryCategoryRequest>
{
    public CreateInventoryCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateInventoryCategoryRequestValidator : AbstractValidator<UpdateInventoryCategoryRequest>
{
    public UpdateInventoryCategoryRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
}

public sealed class CreateInventoryItemRequestValidator : AbstractValidator<CreateInventoryItemRequest>
{
    public CreateInventoryItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(32);
        RuleFor(x => x.MinStockQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateInventoryItemRequestValidator : AbstractValidator<UpdateInventoryItemRequest>
{
    public UpdateInventoryItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(32);
        RuleFor(x => x.MinStockQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Direction).Must(d => d is "In" or "Out").WithMessage("Direction must be In or Out.");
        RuleFor(x => x.UnitCost).NotNull().When(x => x.Direction == "In")
            .WithMessage("UnitCost is required for Adjust In.");
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).When(x => x.UnitCost.HasValue);
    }
}

public sealed class CreateGoodsReceiptFromPoRequestValidator : AbstractValidator<CreateGoodsReceiptFromPoRequest>
{
    public CreateGoodsReceiptFromPoRequestValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.PurchaseOrderLineId).NotEmpty();
            line.RuleFor(l => l.InventoryItemId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}

public sealed class CancelGoodsReceiptRequestValidator : AbstractValidator<CancelGoodsReceiptRequest>
{
    public CancelGoodsReceiptRequestValidator() => RuleFor(x => x.Reason).MaximumLength(500);
}
