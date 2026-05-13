using FluentValidation;

namespace EdSkill.Application.Features.Skills.Commands.CreateSkill;

public class CreateSkillCommandValidator : AbstractValidator<CreateSkillCommand>
{
    public CreateSkillCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Skill name is required")
            .WithErrorCode("INVALID_SKILL_NAME")
            .Must(name => SkillNormalization.NormalizeOptionalText(name).Length <= 50)
            .WithMessage("Skill name must not exceed 50 characters")
            .WithErrorCode("INVALID_SKILL_NAME");

        RuleFor(x => x.Slug)
            .Must(slug => string.IsNullOrWhiteSpace(slug) || SkillNormalization.NormalizeOptionalText(slug).Length <= 100)
            .WithMessage("Skill slug must not exceed 100 characters")
            .WithErrorCode("INVALID_SKILL_SLUG");

        RuleFor(x => x.Category)
            .Must(category => string.IsNullOrWhiteSpace(category) || SkillNormalization.NormalizeOptionalText(category).Length <= 100)
            .WithMessage("Skill category must not exceed 100 characters")
            .WithErrorCode("INVALID_SKILL_CATEGORY");

        RuleFor(x => x.BasePointCost)
            .GreaterThan(0)
            .WithMessage("Skill base point cost must be greater than zero")
            .WithErrorCode("INVALID_SKILL_BASE_POINTS");

        RuleFor(x => x.Aliases)
            .Must(aliases => aliases is null || aliases.Count <= 20)
            .WithMessage("Skill aliases must not exceed 20 items")
            .WithErrorCode("INVALID_SKILL_ALIASES");

        RuleFor(x => x)
            .Must(x => !SkillCatalogRules.HasDuplicateAliases(x.Name, x.Aliases))
            .WithMessage("Skill aliases contain duplicates or invalid values")
            .WithErrorCode("INVALID_SKILL_ALIASES");
    }
}
