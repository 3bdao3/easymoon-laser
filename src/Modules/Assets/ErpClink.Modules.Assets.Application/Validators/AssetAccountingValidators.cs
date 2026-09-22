using ErpClink.Modules.Assets.Application.Accounting.Models;
using FluentValidation;

namespace ErpClink.Modules.Assets.Application.Validators;

public sealed class CapitalizeAssetRequestValidator : AbstractValidator<CapitalizeAssetRequest>
{
    public CapitalizeAssetRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.CapitalizedCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ResidualValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ResidualValue).LessThanOrEqualTo(x => x.CapitalizedCost);
        RuleFor(x => x.UsefulLifeMonths).GreaterThan(0).LessThanOrEqualTo(600);
        RuleFor(x => x.DepreciationStartDate)
            .GreaterThanOrEqualTo(x => x.CapitalizationDate)
            .WithMessage("Depreciation start date cannot be before capitalization date.");
        RuleFor(x => x.AcquisitionCost).GreaterThanOrEqualTo(0).When(x => x.AcquisitionCost.HasValue);
    }
}

public sealed class PostDepreciationRequestValidator : AbstractValidator<PostDepreciationRequest>
{
    public PostDepreciationRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.PeriodKey)
            .Matches(@"^\d{4}-\d{2}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PeriodKey));
    }
}

public sealed class DisposeAssetFinancialRequestValidator : AbstractValidator<DisposeAssetFinancialRequest>
{
    public DisposeAssetFinancialRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Proceeds).GreaterThanOrEqualTo(0).When(x => x.Proceeds.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
