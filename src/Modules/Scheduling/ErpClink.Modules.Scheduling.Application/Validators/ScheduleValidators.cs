using ErpClink.Modules.Scheduling.Application.Schedules.Models;
using ErpClink.Modules.Scheduling.Domain.Schedules;
using FluentValidation;

namespace ErpClink.Modules.Scheduling.Application.Validators;

public sealed class CreateDoctorScheduleRequestValidator : AbstractValidator<CreateDoctorScheduleRequest>
{
    public CreateDoctorScheduleRequestValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime).WithMessage("StartTime must be before EndTime.");
        RuleFor(x => x.SlotDurationMinutes)
            .InclusiveBetween(DoctorWorkingSchedule.MinSlotDurationMinutes, DoctorWorkingSchedule.MaxSlotDurationMinutes);
        RuleFor(x => x.EffectiveTo)
            .Must((req, to) => !to.HasValue || to.Value >= req.EffectiveFrom)
            .WithMessage("EffectiveTo cannot be before EffectiveFrom.");
    }
}

public sealed class UpdateDoctorScheduleRequestValidator : AbstractValidator<UpdateDoctorScheduleRequest>
{
    public UpdateDoctorScheduleRequestValidator()
    {
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime).WithMessage("StartTime must be before EndTime.");
        RuleFor(x => x.SlotDurationMinutes)
            .InclusiveBetween(DoctorWorkingSchedule.MinSlotDurationMinutes, DoctorWorkingSchedule.MaxSlotDurationMinutes);
        RuleFor(x => x.EffectiveTo)
            .Must((req, to) => !to.HasValue || to.Value >= req.EffectiveFrom)
            .WithMessage("EffectiveTo cannot be before EffectiveFrom.");
    }
}
