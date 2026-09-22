using FluentValidation;

namespace ErpClink.Modules.Finance.Application.Subledgers;

public sealed class SubledgerPostRequestValidator : AbstractValidator<SubledgerPostRequest>
{
    public SubledgerPostRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.SourceModule).NotEmpty().MaximumLength(64);
        RuleFor(x => x.SourceType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.SourceId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.EventType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.Direction).NotEmpty()
            .Must(d => d.Equals("Debit", StringComparison.OrdinalIgnoreCase)
                       || d.Equals("Credit", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Direction must be Debit or Credit.");
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
    }
}
