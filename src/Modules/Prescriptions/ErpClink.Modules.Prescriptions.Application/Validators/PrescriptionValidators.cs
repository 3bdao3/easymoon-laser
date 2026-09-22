using ErpClink.Modules.Prescriptions.Application.Medications.Models;
using ErpClink.Modules.Prescriptions.Application.Prescriptions.Models;
using FluentValidation;

namespace ErpClink.Modules.Prescriptions.Application.Validators;

public sealed class CreateMedicationRequestValidator : AbstractValidator<CreateMedicationRequest>
{
    public CreateMedicationRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GenericName).MaximumLength(200);
        RuleFor(x => x.Strength).MaximumLength(64);
        RuleFor(x => x.DosageForm).MaximumLength(64);
        RuleFor(x => x.Route).MaximumLength(64);
    }
}

public sealed class UpdateMedicationRequestValidator : AbstractValidator<UpdateMedicationRequest>
{
    public UpdateMedicationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GenericName).MaximumLength(200);
        RuleFor(x => x.Strength).MaximumLength(64);
        RuleFor(x => x.DosageForm).MaximumLength(64);
        RuleFor(x => x.Route).MaximumLength(64);
    }
}

public sealed class PrescriptionItemInputValidator : AbstractValidator<PrescriptionItemInput>
{
    public PrescriptionItemInputValidator()
    {
        RuleFor(x => x.MedicationId).NotEmpty();
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Duration).MaximumLength(128);
        RuleFor(x => x.Route).MaximumLength(64);
        RuleFor(x => x.Instructions).MaximumLength(1000);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
    }
}

public sealed class CreatePrescriptionRequestValidator : AbstractValidator<CreatePrescriptionRequest>
{
    public CreatePrescriptionRequestValidator()
    {
        RuleFor(x => x.MedicalVisitId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Items).Must(i => i is null || i.Count <= 50)
            .WithMessage("A prescription cannot contain more than 50 items.");
        RuleForEach(x => x.Items).SetValidator(new PrescriptionItemInputValidator()).When(x => x.Items is not null);
    }
}

public sealed class UpdatePrescriptionRequestValidator : AbstractValidator<UpdatePrescriptionRequest>
{
    public UpdatePrescriptionRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class UpdatePrescriptionItemRequestValidator : AbstractValidator<UpdatePrescriptionItemRequest>
{
    public UpdatePrescriptionItemRequestValidator()
    {
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Duration).MaximumLength(128);
        RuleFor(x => x.Route).MaximumLength(64);
        RuleFor(x => x.Instructions).MaximumLength(1000);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
    }
}

public sealed class CancelPrescriptionRequestValidator : AbstractValidator<CancelPrescriptionRequest>
{
    public CancelPrescriptionRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}
