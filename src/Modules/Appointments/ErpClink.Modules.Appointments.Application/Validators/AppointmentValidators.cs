using ErpClink.Modules.Appointments.Application.Appointments.Models;
using FluentValidation;

namespace ErpClink.Modules.Appointments.Application.Validators;

public sealed class BookAppointmentRequestValidator : AbstractValidator<BookAppointmentRequest>
{
    public BookAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason is not null);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}

public sealed class RescheduleAppointmentRequestValidator : AbstractValidator<RescheduleAppointmentRequest>
{
    public RescheduleAppointmentRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
    }
}

public sealed class CancelAppointmentRequestValidator : AbstractValidator<CancelAppointmentRequest>
{
    public CancelAppointmentRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason is not null);
    }
}
