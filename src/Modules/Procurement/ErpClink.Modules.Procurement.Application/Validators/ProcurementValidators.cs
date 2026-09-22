using ErpClink.Modules.Procurement.Application.PurchaseOrders.Models;
using ErpClink.Modules.Procurement.Application.Suppliers.Models;
using FluentValidation;

namespace ErpClink.Modules.Procurement.Application.Validators;

public sealed class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class PurchaseOrderLineInputDtoValidator : AbstractValidator<PurchaseOrderLineInputDto>
{
    public PurchaseOrderLineInputDtoValidator()
    {
        RuleFor(x => x.DescriptionSnapshot).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LineDiscountAmount).GreaterThanOrEqualTo(0).When(x => x.LineDiscountAmount.HasValue);
    }
}

public sealed class CreateDraftPurchaseOrderRequestValidator : AbstractValidator<CreateDraftPurchaseOrderRequest>
{
    public CreateDraftPurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue);
        RuleForEach(x => x.Lines).SetValidator(new PurchaseOrderLineInputDtoValidator()).When(x => x.Lines is { Count: > 0 });
    }
}

public sealed class UpdateDraftPurchaseOrderRequestValidator : AbstractValidator<UpdateDraftPurchaseOrderRequest>
{
    public UpdateDraftPurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue);
    }
}

public sealed class AddPurchaseOrderLineRequestValidator : AbstractValidator<AddPurchaseOrderLineRequest>
{
    public AddPurchaseOrderLineRequestValidator()
    {
        RuleFor(x => x.Line).SetValidator(new PurchaseOrderLineInputDtoValidator());
    }
}

public sealed class UpdatePurchaseOrderLineRequestValidator : AbstractValidator<UpdatePurchaseOrderLineRequest>
{
    public UpdatePurchaseOrderLineRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.LineDiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SortOrder).GreaterThan(0);
    }
}

public sealed class SetPurchaseOrderDiscountRequestValidator : AbstractValidator<SetPurchaseOrderDiscountRequest>
{
    public SetPurchaseOrderDiscountRequestValidator()
    {
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
    }
}

public sealed class CancelPurchaseOrderRequestValidator : AbstractValidator<CancelPurchaseOrderRequest>
{
    public CancelPurchaseOrderRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
