using ErpClink.Modules.Services.Application.Catalog.Models;
using ErpClink.Modules.Services.Application.Categories.Models;
using ErpClink.Modules.Services.Application.Packages.Models;
using FluentValidation;

namespace ErpClink.Modules.Services.Application.Validators;

public sealed class CreateHealthcareServiceRequestValidator : AbstractValidator<CreateHealthcareServiceRequest>
{
    public CreateHealthcareServiceRequestValidator()
    {
        RuleFor(x => x.ServiceCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DefaultPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.DurationMinutes).GreaterThanOrEqualTo(0).When(x => x.DurationMinutes.HasValue);
    }
}

public sealed class UpdateHealthcareServiceRequestValidator : AbstractValidator<UpdateHealthcareServiceRequest>
{
    public UpdateHealthcareServiceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DefaultPrice).GreaterThanOrEqualTo(0).When(x => x.DefaultPrice.HasValue);
        RuleFor(x => x.CurrencyCode).Length(3).When(x => !string.IsNullOrWhiteSpace(x.CurrencyCode));
        RuleFor(x => x.DurationMinutes).GreaterThanOrEqualTo(0).When(x => x.DurationMinutes.HasValue);
    }
}

public sealed class CreateServiceCategoryRequestValidator : AbstractValidator<CreateServiceCategoryRequest>
{
    public CreateServiceCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateServiceCategoryRequestValidator : AbstractValidator<UpdateServiceCategoryRequest>
{
    public UpdateServiceCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateHealthcarePackageRequestValidator : AbstractValidator<CreateHealthcarePackageRequest>
{
    public CreateHealthcarePackageRequestValidator()
    {
        RuleFor(x => x.PackageCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleForEach(x => x.Items).SetValidator(new PackageItemInputValidator()).When(x => x.Items is not null);
    }
}

public sealed class UpdateHealthcarePackageRequestValidator : AbstractValidator<UpdateHealthcarePackageRequest>
{
    public UpdateHealthcarePackageRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public sealed class PackageItemInputValidator : AbstractValidator<PackageItemInput>
{
    public PackageItemInputValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class UpdatePackageItemRequestValidator : AbstractValidator<UpdatePackageItemRequest>
{
    public UpdatePackageItemRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.SortOrder).GreaterThan(0);
    }
}
