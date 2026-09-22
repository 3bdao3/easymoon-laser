using ErpClink.Modules.Doctors.Application.Clinics.Models;
using ErpClink.Modules.Doctors.Application.Doctors.Models;
using ErpClink.Modules.Doctors.Application.Specialties.Models;
using FluentValidation;

namespace ErpClink.Modules.Doctors.Application.Validators;

public sealed class RegisterDoctorRequestValidator : AbstractValidator<RegisterDoctorRequest>
{
    public RegisterDoctorRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).MaximumLength(200).When(x => x.DisplayName is not null);
        RuleFor(x => x.LicenseNumber).MaximumLength(50).When(x => x.LicenseNumber is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(30)
            .Matches(@"^[0-9+\-\s()]{7,30}$").When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.UserId).MaximumLength(450).When(x => x.UserId is not null);
    }
}

public sealed class UpdateDoctorRequestValidator : AbstractValidator<UpdateDoctorRequest>
{
    public UpdateDoctorRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).MaximumLength(200).When(x => x.DisplayName is not null);
        RuleFor(x => x.LicenseNumber).MaximumLength(50).When(x => x.LicenseNumber is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(30)
            .Matches(@"^[0-9+\-\s()]{7,30}$").When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.UserId).MaximumLength(450).When(x => x.UserId is not null);
    }
}

public sealed class AssignDoctorToClinicRequestValidator : AbstractValidator<AssignDoctorToClinicRequest>
{
    public AssignDoctorToClinicRequestValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.EndDate)
            .Must((req, end) => !end.HasValue || end.Value >= req.StartDate)
            .WithMessage("EndDate cannot be earlier than StartDate.");
    }
}

public sealed class CreateClinicRequestValidator : AbstractValidator<CreateClinicRequest>
{
    public CreateClinicRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.Location).MaximumLength(200).When(x => x.Location is not null);
    }
}

public sealed class UpdateClinicRequestValidator : AbstractValidator<UpdateClinicRequest>
{
    public UpdateClinicRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.Location).MaximumLength(200).When(x => x.Location is not null);
    }
}

public sealed class CreateSpecialtyRequestValidator : AbstractValidator<CreateSpecialtyRequest>
{
    public CreateSpecialtyRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}

public sealed class UpdateSpecialtyRequestValidator : AbstractValidator<UpdateSpecialtyRequest>
{
    public UpdateSpecialtyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
