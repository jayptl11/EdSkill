using FluentValidation;

namespace EdSkill.Application.Features.Sessions.Commands.CreateSessionOffer;

public class CreateSessionOfferCommandValidator : AbstractValidator<CreateSessionOfferCommand>
{
    public CreateSessionOfferCommandValidator()
    {
        RuleFor(item => item.Skill).NotEmpty().MaximumLength(100);
        RuleFor(item => item.Description).MaximumLength(2000);
        RuleFor(item => item.DurationMinutes).Must(value => new[] { 30, 45, 60, 90, 120 }.Contains(value));
        RuleFor(item => item.PointCost).GreaterThan(0);
        RuleFor(item => item.ScheduledAt).GreaterThan(DateTime.UtcNow);
    }
}
