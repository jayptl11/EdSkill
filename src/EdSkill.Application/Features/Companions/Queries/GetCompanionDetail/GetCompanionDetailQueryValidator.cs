using EdSkill.Domain.Enums;
using FluentValidation;

namespace EdSkill.Application.Features.Companions.Queries.GetCompanionDetail;

public class GetCompanionDetailQueryValidator : AbstractValidator<GetCompanionDetailQuery>
{
    public GetCompanionDetailQueryValidator()
    {
        RuleFor(item => item.CompanionId).NotEmpty();
        RuleFor(item => item.SkillId).NotEmpty();
        RuleFor(item => item.ReviewPage).GreaterThan(0);
        RuleFor(item => item.ReviewLimit).InclusiveBetween(1, 100);
        RuleFor(item => item.Location)
            .NotEmpty()
            .When(item => item.DeliveryMode == SessionDeliveryMode.Offline);
        RuleFor(item => item.Location)
            .MaximumLength(500)
            .When(item => !string.IsNullOrWhiteSpace(item.Location));
        RuleFor(item => item.Location)
            .Must(string.IsNullOrWhiteSpace)
            .When(item => item.DeliveryMode != SessionDeliveryMode.Offline)
            .WithMessage("Location filter is only allowed for offline search.");
    }
}
