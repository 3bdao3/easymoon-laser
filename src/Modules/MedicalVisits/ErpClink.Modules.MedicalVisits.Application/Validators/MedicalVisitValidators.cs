using ErpClink.Modules.MedicalVisits.Application.Visits.Models;
using FluentValidation;

namespace ErpClink.Modules.MedicalVisits.Application.Validators;

public sealed class StartMedicalVisitRequestValidator : AbstractValidator<StartMedicalVisitRequest>
{
    public StartMedicalVisitRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}

public sealed class UpdateClinicalNotesRequestValidator : AbstractValidator<UpdateClinicalNotesRequest>
{
    public UpdateClinicalNotesRequestValidator()
    {
        RuleFor(x => x.ChiefComplaint).MaximumLength(1000);
        RuleFor(x => x.ClinicalNotes).MaximumLength(4000);
        RuleFor(x => x.ExaminationFindings).MaximumLength(4000);
        RuleFor(x => x.DiagnosisNotes).MaximumLength(2000);
        RuleFor(x => x.FollowUpNotes).MaximumLength(2000);
    }
}

public sealed class CancelMedicalVisitRequestValidator : AbstractValidator<CancelMedicalVisitRequest>
{
    public CancelMedicalVisitRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
