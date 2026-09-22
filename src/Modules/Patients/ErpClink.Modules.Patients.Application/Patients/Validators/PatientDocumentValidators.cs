using ErpClink.Modules.Patients.Application.Patients.Models;
using ErpClink.Modules.Patients.Domain.Patients;
using FluentValidation;

namespace ErpClink.Modules.Patients.Application.Patients.Validators;

public sealed class UpdatePatientDocumentMetadataRequestValidator : AbstractValidator<UpdatePatientDocumentMetadataRequest>
{
    public UpdatePatientDocumentMetadataRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x)
            .Must(x => PatientDocumentTypeRules.BelongsToCategory(x.DocumentType, x.Category))
            .WithMessage("Document type does not belong to the selected category.");
    }
}

public sealed class SearchPatientDocumentsRequestValidator : AbstractValidator<SearchPatientDocumentsRequest>
{
    public SearchPatientDocumentsRequestValidator()
    {
        RuleFor(x => x.Query).MaximumLength(200).When(x => x.Query is not null);
        RuleFor(x => x.UploadedBy).MaximumLength(64).When(x => x.UploadedBy is not null);
        RuleFor(x => x.Category).IsInEnum().When(x => x.Category.HasValue);
        RuleFor(x => x.DocumentType).IsInEnum().When(x => x.DocumentType.HasValue);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
