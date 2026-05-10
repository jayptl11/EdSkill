using FluentValidation;

namespace EdSkill.Application.Features.Admin.Commands.GrantPoints;

public class GrantPointsCommandValidator : AbstractValidator<GrantPointsCommand>
{
    public GrantPointsCommandValidator()
    {
        RuleFor(item => item.UserIds).NotEmpty();
        RuleFor(item => item.Amount).GreaterThan(0);
        RuleFor(item => item.Note).NotEmpty().MaximumLength(500);
    }
}
