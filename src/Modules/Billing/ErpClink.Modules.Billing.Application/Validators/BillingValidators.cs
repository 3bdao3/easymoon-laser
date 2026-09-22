using ErpClink.Modules.Billing.Application.Invoices.Models;
using ErpClink.Modules.Billing.Application.Payments.Models;
using ErpClink.Modules.Billing.Domain.Payments;
using FluentValidation;

namespace ErpClink.Modules.Billing.Application.Validators;

public sealed class InvoiceLineInputValidator : AbstractValidator<InvoiceLineInput>
{
    public InvoiceLineInputValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.LineDiscountAmount).GreaterThanOrEqualTo(0).When(x => x.LineDiscountAmount.HasValue);
        RuleFor(x => x)
            .Must(x => x.ServiceId.HasValue ^ x.PackageId.HasValue)
            .WithMessage("Each line must specify either ServiceId or PackageId, not both.");
    }
}

public sealed class CreateDraftInvoiceRequestValidator : AbstractValidator<CreateDraftInvoiceRequest>
{
    public CreateDraftInvoiceRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.InvoiceDiscountAmount).GreaterThanOrEqualTo(0).When(x => x.InvoiceDiscountAmount.HasValue);
        RuleForEach(x => x.Lines).SetValidator(new InvoiceLineInputValidator()).When(x => x.Lines is { Count: > 0 });
    }
}

public sealed class UpdateDraftInvoiceRequestValidator : AbstractValidator<UpdateDraftInvoiceRequest>
{
    public UpdateDraftInvoiceRequestValidator()
    {
        RuleFor(x => x.InvoiceDiscountAmount).GreaterThanOrEqualTo(0).When(x => x.InvoiceDiscountAmount.HasValue);
    }
}

public sealed class AddInvoiceLineRequestValidator : AbstractValidator<AddInvoiceLineRequest>
{
    public AddInvoiceLineRequestValidator()
    {
        RuleFor(x => x.Line).SetValidator(new InvoiceLineInputValidator());
    }
}

public sealed class UpdateInvoiceLineRequestValidator : AbstractValidator<UpdateInvoiceLineRequest>
{
    public UpdateInvoiceLineRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.LineDiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SortOrder).GreaterThan(0);
    }
}

public sealed class VoidInvoiceRequestValidator : AbstractValidator<VoidInvoiceRequest>
{
    public VoidInvoiceRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).NotEmpty()
            .Must(m => Enum.TryParse<PaymentMethod>(m, true, out _))
            .WithMessage("Invalid payment method.");
    }
}

public sealed class ReversePaymentRequestValidator : AbstractValidator<ReversePaymentRequest>
{
    public ReversePaymentRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
