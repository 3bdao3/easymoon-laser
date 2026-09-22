using ErpClink.Modules.Assets.Application.Assets.Models;
using ErpClink.Modules.Assets.Application.Categories.Models;
using ErpClink.Modules.Assets.Application.Locations.Models;
using FluentValidation;

namespace ErpClink.Modules.Assets.Application.Validators;

public sealed class CreateAssetCategoryRequestValidator : AbstractValidator<CreateAssetCategoryRequest>
{
    public CreateAssetCategoryRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
}

public sealed class UpdateAssetCategoryRequestValidator : AbstractValidator<UpdateAssetCategoryRequest>
{
    public UpdateAssetCategoryRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
}

public sealed class CreateAssetLocationRequestValidator : AbstractValidator<CreateAssetLocationRequest>
{
    public CreateAssetLocationRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
}

public sealed class UpdateAssetLocationRequestValidator : AbstractValidator<UpdateAssetLocationRequest>
{
    public UpdateAssetLocationRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
}

public sealed class CreateAssetRequestValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AssetCategoryId).NotEmpty();
        RuleFor(x => x.AssetLocationId).NotEmpty();
        RuleFor(x => x.AcquisitionCost).GreaterThanOrEqualTo(0).When(x => x.AcquisitionCost.HasValue);
        RuleFor(x => x.WarrantyStartDate)
            .NotEmpty()
            .When(x => x.WarrantyEndDate.HasValue)
            .WithMessage("Warranty start date is required when warranty end date is set.");
        RuleFor(x => x)
            .Must(x => !x.WarrantyStartDate.HasValue || !x.WarrantyEndDate.HasValue || x.WarrantyEndDate >= x.WarrantyStartDate)
            .WithMessage("Warranty end date must be on or after warranty start date.");
    }
}

public sealed class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AssetCategoryId).NotEmpty();
        RuleFor(x => x.AcquisitionCost).GreaterThanOrEqualTo(0).When(x => x.AcquisitionCost.HasValue);
        RuleFor(x => x.WarrantyStartDate)
            .NotEmpty()
            .When(x => x.WarrantyEndDate.HasValue)
            .WithMessage("Warranty start date is required when warranty end date is set.");
        RuleFor(x => x)
            .Must(x => !x.WarrantyStartDate.HasValue || !x.WarrantyEndDate.HasValue || x.WarrantyEndDate >= x.WarrantyStartDate)
            .WithMessage("Warranty end date must be on or after warranty start date.");
    }
}

public sealed class ChangeAssetLocationRequestValidator : AbstractValidator<ChangeAssetLocationRequest>
{
    public ChangeAssetLocationRequestValidator() => RuleFor(x => x.AssetLocationId).NotEmpty();
}

public sealed class StartAssetMaintenanceRequestValidator : AbstractValidator<StartAssetMaintenanceRequest>
{
    public StartAssetMaintenanceRequestValidator() => RuleFor(x => x.Notes).MaximumLength(500);
}

public sealed class RetireAssetRequestValidator : AbstractValidator<RetireAssetRequest>
{
    public RetireAssetRequestValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}
