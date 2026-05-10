using FluentValidation;

namespace EdSkill.Application.Features.Skills.Commands.DeleteSkill;

public class DeleteSkillCommandValidator : AbstractValidator<DeleteSkillCommand>
{
    public DeleteSkillCommandValidator()
    {
        RuleFor(v => v.SkillId)
            .NotEmpty()
            .WithMessage("Skill ID is required.");
    }
}
