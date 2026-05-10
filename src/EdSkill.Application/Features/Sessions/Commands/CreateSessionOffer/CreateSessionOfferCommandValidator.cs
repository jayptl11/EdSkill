using FluentValidation;

namespace EdSkill.Application.Features.Sessions.Commands.CreateSessionOffer;

public class CreateSessionOfferCommandValidator : AbstractValidator<CreateSessionOfferCommand>
{
    public CreateSessionOfferCommandValidator()
    {
        RuleFor(item => item.SkillId).NotEmpty();
        RuleFor(item => item.Description).MaximumLength(2000);
        RuleFor(item => item.Location)
            .MaximumLength(500)
            .When(item => !string.IsNullOrWhiteSpace(item.Location));
        RuleFor(item => item.DurationMinutes).Must(value => new[] { 30, 45, 60, 90, 120 }.Contains(value));
        RuleFor(item => item.PointCost).GreaterThan(0);
        RuleFor(item => item.ScheduledAt).Must(value => value > DateTime.UtcNow);
        RuleFor(item => item.Location)
            .NotEmpty()
            .When(item => item.DeliveryMode == Domain.Enums.SessionDeliveryMode.Offline);
        RuleFor(item => item.Location)
            .Must(string.IsNullOrWhiteSpace)
            .When(item => item.DeliveryMode == Domain.Enums.SessionDeliveryMode.Online)
            .WithMessage("Location is only allowed for offline sessions.");
    }
}
