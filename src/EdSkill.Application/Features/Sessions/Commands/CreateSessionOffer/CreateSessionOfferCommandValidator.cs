using FluentValidation;

namespace EdSkill.Application.Features.Sessions.Commands.CreateSessionOffer;

public class CreateSessionOfferCommandValidator : AbstractValidator<CreateSessionOfferCommand>
{
    private static readonly int[] AllowedDurations = [30, 45, 60, 90, 120];

    public CreateSessionOfferCommandValidator()
    {
        RuleFor(item => item.SkillId).NotEmpty();
        RuleFor(item => item.Description).MaximumLength(2000);
        RuleFor(item => item.DurationOptions)
            .NotEmpty()
            .Must(options => options is not null
                && options.Count > 0
                && options.Distinct().Count() == options.Count
                && options.All(value => AllowedDurations.Contains(value)))
            .WithMessage("Duration options are invalid.")
            .WithErrorCode("INVALID_DURATION_OPTIONS");
        RuleFor(item => item.ScheduledAt).Must(value => value > DateTime.UtcNow);
    }
}
