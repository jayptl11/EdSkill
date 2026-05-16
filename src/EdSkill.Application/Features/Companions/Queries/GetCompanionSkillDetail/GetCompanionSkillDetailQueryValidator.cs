using FluentValidation;

namespace EdSkill.Application.Features.Companions.Queries.GetCompanionSkillDetail;

public class GetCompanionSkillDetailQueryValidator : AbstractValidator<GetCompanionSkillDetailQuery>
{
    public GetCompanionSkillDetailQueryValidator()
    {
        RuleFor(item => item.CompanionId).NotEmpty();
        RuleFor(item => item.SkillId).NotEmpty();
        RuleFor(item => item.ReviewPage).GreaterThan(0);
        RuleFor(item => item.ReviewLimit).InclusiveBetween(1, 100);
        RuleFor(item => item.OfferPage).GreaterThan(0);
        RuleFor(item => item.OfferLimit).InclusiveBetween(1, 100);
    }
}
