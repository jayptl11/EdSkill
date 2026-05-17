using FluentValidation;

namespace EdSkill.Application.Features.Wallet.Commands.CreatePointPurchase;

public class CreatePointPurchaseCommandValidator : AbstractValidator<CreatePointPurchaseCommand>
{
    public CreatePointPurchaseCommandValidator()
    {
        RuleFor(item => item.PackageId)
            .NotEmpty()
            .WithErrorCode("POINT_PACKAGE_NOT_FOUND");
    }
}
