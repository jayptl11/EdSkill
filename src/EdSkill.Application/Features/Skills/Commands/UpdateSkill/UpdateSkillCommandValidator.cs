using FluentValidation;

namespace EdSkill.Application.Features.Skills.Commands.UpdateSkill;

public class UpdateSkillCommandValidator : AbstractValidator<UpdateSkillCommand>
{
    public UpdateSkillCommandValidator()
    {
        RuleFor(x => x.SkillId)
            .NotEmpty()
            .WithMessage("Skill id is required")
            .WithErrorCode("SKILL_NOT_FOUND");

        When(x => x.HasName, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Skill name is required")
                .WithErrorCode("INVALID_SKILL_NAME")
                .Must(name => name is not null && SkillNormalization.NormalizeWhitespace(name).Length <= 50)
                .WithMessage("Skill name must not exceed 50 characters")
                .WithErrorCode("INVALID_SKILL_NAME");
        });

        When(x => x.HasSlug, () =>
        {
            RuleFor(x => x.Slug)
                .NotEmpty()
                .WithMessage("Skill slug is required")
                .WithErrorCode("INVALID_SKILL_SLUG")
                .Must(slug => slug is not null && SkillNormalization.NormalizeWhitespace(slug).Length <= 100)
                .WithMessage("Skill slug must not exceed 100 characters")
                .WithErrorCode("INVALID_SKILL_SLUG");
        });

        When(x => x.HasCategory && x.Category is not null, () =>
        {
            RuleFor(x => x.Category!)
                .Must(category => SkillNormalization.NormalizeWhitespace(category).Length <= 100)
                .WithMessage("Skill category must not exceed 100 characters")
                .WithErrorCode("INVALID_SKILL_CATEGORY");
        });

        When(x => x.HasBasePointCost, () =>
        {
            RuleFor(x => x.BasePointCost)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("Skill base point cost must be greater than zero")
                .WithErrorCode("INVALID_SKILL_BASE_POINTS");
        });

        When(x => x.HasAliases, () =>
        {
            RuleFor(x => x.Aliases)
                .Must(aliases => aliases is null || aliases.Count <= 20)
                .WithMessage("Skill aliases must not exceed 20 items")
                .WithErrorCode("INVALID_SKILL_ALIASES");
        });

        RuleFor(x => x)
            .Must(command =>
            {
                var name = command.HasName ? command.Name : "placeholder";
                if (name is null)
                {
                    return true;
                }

                return !command.HasAliases || !SkillCatalogRules.HasDuplicateAliases(name, command.Aliases);
            })
            .WithMessage("Skill aliases contain duplicates or invalid values")
            .WithErrorCode("INVALID_SKILL_ALIASES");
    }
}
