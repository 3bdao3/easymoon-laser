using FluentValidation;

namespace ErpClink.Modules.Finance.Application.Integration;

public sealed class AccountingPostingRequestValidator : AbstractValidator<AccountingPostingRequest>
{
    public AccountingPostingRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty().WithMessage("OrganizationId is required.");
        RuleFor(x => x.SourceModule).NotEmpty().MaximumLength(64);
        RuleFor(x => x.SourceType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.SourceId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.EventType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OccurredAtUtc).NotEqual(default(DateTime));
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.CurrencyCode).MaximumLength(3).When(x => x.CurrencyCode is not null);
    }
}
