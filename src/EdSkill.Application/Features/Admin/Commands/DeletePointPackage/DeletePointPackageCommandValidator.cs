using FluentValidation;

namespace EdSkill.Application.Features.Admin.Commands.DeletePointPackage;

public class DeletePointPackageCommandValidator : AbstractValidator<DeletePointPackageCommand>
{
    public DeletePointPackageCommandValidator()
    {
        RuleFor(item => item.PackageId).NotEmpty();
    }
}
