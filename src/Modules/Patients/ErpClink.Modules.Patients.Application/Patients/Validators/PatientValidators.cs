using ErpClink.Modules.Patients.Application.Patients.Models;
using FluentValidation;

namespace ErpClink.Modules.Patients.Application.Patients.Validators;

public sealed class RegisterPatientRequestValidator : AbstractValidator<RegisterPatientRequest>
{
    public RegisterPatientRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100).When(x => x.MiddleName is not null);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth).Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30)
            .Matches(@"^[0-9+\-\s()]{7,30}$").WithMessage("Phone number format is invalid.");
        RuleFor(x => x.NationalId).MaximumLength(50).When(x => x.NationalId is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AddressLine1).MaximumLength(200).When(x => x.AddressLine1 is not null);
        RuleFor(x => x.AddressLine2).MaximumLength(200).When(x => x.AddressLine2 is not null);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City is not null);
        RuleFor(x => x.EmergencyContactName).MaximumLength(150).When(x => x.EmergencyContactName is not null);
        RuleFor(x => x.EmergencyContactPhone).MaximumLength(30)
            .Matches(@"^[0-9+\-\s()]{7,30}$")
            .When(x => !string.IsNullOrWhiteSpace(x.EmergencyContactPhone));
        RuleFor(x => x.Gender).IsInEnum();
    }
}

public sealed class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100).When(x => x.MiddleName is not null);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth).Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30)
            .Matches(@"^[0-9+\-\s()]{7,30}$").WithMessage("Phone number format is invalid.");
        RuleFor(x => x.NationalId).MaximumLength(50).When(x => x.NationalId is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AddressLine1).MaximumLength(200).When(x => x.AddressLine1 is not null);
        RuleFor(x => x.AddressLine2).MaximumLength(200).When(x => x.AddressLine2 is not null);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City is not null);
        RuleFor(x => x.EmergencyContactName).MaximumLength(150).When(x => x.EmergencyContactName is not null);
        RuleFor(x => x.EmergencyContactPhone).MaximumLength(30)
            .Matches(@"^[0-9+\-\s()]{7,30}$")
            .When(x => !string.IsNullOrWhiteSpace(x.EmergencyContactPhone));
        RuleFor(x => x.Gender).IsInEnum();
    }
}

public sealed class AddAllergyRequestValidator : AbstractValidator<AddAllergyRequest>
{
    public AddAllergyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reaction).MaximumLength(500).When(x => x.Reaction is not null);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
        RuleFor(x => x.Severity).IsInEnum();
    }
}

public sealed class UpdateAllergyRequestValidator : AbstractValidator<UpdateAllergyRequest>
{
    public UpdateAllergyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reaction).MaximumLength(500).When(x => x.Reaction is not null);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
        RuleFor(x => x.Severity).IsInEnum();
    }
}
