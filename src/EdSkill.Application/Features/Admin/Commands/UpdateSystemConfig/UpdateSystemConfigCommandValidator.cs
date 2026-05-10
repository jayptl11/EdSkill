using FluentValidation;

namespace EdSkill.Application.Features.Admin.Commands.UpdateSystemConfig;

public class UpdateSystemConfigCommandValidator : AbstractValidator<UpdateSystemConfigCommand>
{
    public UpdateSystemConfigCommandValidator()
    {
        RuleFor(item => item.Key).NotEmpty().MaximumLength(128);
        RuleFor(item => item.Value).NotEmpty().MaximumLength(256);
    }
}
