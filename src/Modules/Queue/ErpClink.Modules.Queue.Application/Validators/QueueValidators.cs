using ErpClink.Modules.Queue.Application.Entries.Models;
using ErpClink.Modules.Queue.Domain.Entries;
using FluentValidation;

namespace ErpClink.Modules.Queue.Application.Validators;

public sealed class CheckInRequestValidator : AbstractValidator<CheckInRequest>
{
    public CheckInRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Priority)
            .Must(p => string.IsNullOrWhiteSpace(p) || Enum.TryParse<QueuePriority>(p, true, out _))
            .WithMessage("Priority must be Normal or Urgent.");
    }
}
