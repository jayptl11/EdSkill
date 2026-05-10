using FluentValidation;

namespace EdSkill.Application.Features.Skills.Queries.SearchSkills;

public class SearchSkillsQueryValidator : AbstractValidator<SearchSkillsQuery>
{
    public SearchSkillsQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100)
            .WithMessage("Limit must be between 1 and 100")
            .WithErrorCode("INVALID_LIMIT");
    }
}
