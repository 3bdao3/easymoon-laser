using ErpClink.Modules.Finance.Application.Accounts.Models;
using ErpClink.Modules.Finance.Application.FiscalPeriods.Models;
using ErpClink.Modules.Finance.Application.FiscalYears.Models;
using ErpClink.Modules.Finance.Application.Journals.Models;
using ErpClink.Modules.Finance.Application.TrialBalance.Models;
using ErpClink.Modules.Finance.Domain.Accounts;
using FluentValidation;

namespace ErpClink.Modules.Finance.Application.Validators;

public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.AccountType).Must(BeAccountType).WithMessage("Invalid account type.");
    }

    private static bool BeAccountType(string value) =>
        Enum.TryParse<AccountType>(value, true, out _);
}

public sealed class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.AccountType).Must(BeAccountType).WithMessage("Invalid account type.");
        RuleFor(x => x.RowVersion).NotEmpty();
    }

    private static bool BeAccountType(string value) =>
        Enum.TryParse<AccountType>(value, true, out _);
}

public sealed class CreateFiscalYearRequestValidator : AbstractValidator<CreateFiscalYearRequest>
{
    public CreateFiscalYearRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x).Must(x => x.StartDate < x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public sealed class UpdateFiscalYearRequestValidator : AbstractValidator<UpdateFiscalYearRequest>
{
    public UpdateFiscalYearRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x).Must(x => x.StartDate < x.EndDate).WithMessage("Start date must be before end date.");
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

public sealed class CreateFiscalPeriodRequestValidator : AbstractValidator<CreateFiscalPeriodRequest>
{
    public CreateFiscalPeriodRequestValidator()
    {
        RuleFor(x => x.FiscalYearId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x).Must(x => x.StartDate < x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public sealed class UpdateFiscalPeriodRequestValidator : AbstractValidator<UpdateFiscalPeriodRequest>
{
    public UpdateFiscalPeriodRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x).Must(x => x.StartDate < x.EndDate).WithMessage("Start date must be before end date.");
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

public sealed class CreateJournalDraftRequestValidator : AbstractValidator<CreateJournalDraftRequest>
{
    public CreateJournalDraftRequestValidator()
    {
        RuleForEach(x => x.Lines).SetValidator(new JournalLineInputValidator()).When(x => x.Lines is not null);
    }
}

public sealed class UpdateJournalDraftRequestValidator : AbstractValidator<UpdateJournalDraftRequest>
{
    public UpdateJournalDraftRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

public sealed class ReplaceJournalLinesRequestValidator : AbstractValidator<ReplaceJournalLinesRequest>
{
    public ReplaceJournalLinesRequestValidator()
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new JournalLineInputValidator());
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

public sealed class JournalLineInputValidator : AbstractValidator<JournalLineInput>
{
    public JournalLineInputValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Debit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Credit).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => (x.Debit > 0 && x.Credit == 0) || (x.Credit > 0 && x.Debit == 0))
            .WithMessage("Each line must have either debit or credit.");
    }
}

public sealed class PostJournalRequestValidator : AbstractValidator<PostJournalRequest>
{
    public PostJournalRequestValidator() => RuleFor(x => x.RowVersion).NotEmpty();
}

public sealed class ReverseJournalRequestValidator : AbstractValidator<ReverseJournalRequest>
{
    public ReverseJournalRequestValidator() => RuleFor(x => x.RowVersion).NotEmpty();
}

public sealed class GetTrialBalanceRequestValidator : AbstractValidator<GetTrialBalanceRequest>
{
    public GetTrialBalanceRequestValidator()
    {
        RuleFor(x => x).Must(x => x.FromDate <= x.ToDate).WithMessage("From date must be on or before to date.");
    }
}
