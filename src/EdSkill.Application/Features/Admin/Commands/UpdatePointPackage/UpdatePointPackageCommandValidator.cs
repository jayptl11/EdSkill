using FluentValidation;

namespace EdSkill.Application.Features.Admin.Commands.UpdatePointPackage;

public class UpdatePointPackageCommandValidator : AbstractValidator<UpdatePointPackageCommand>
{
    public UpdatePointPackageCommandValidator()
    {
        When(item => item.HasCode, () =>
        {
            RuleFor(item => item.Code)
                .NotEmpty()
                .MaximumLength(64);
        });

        When(item => item.HasName, () =>
        {
            RuleFor(item => item.Name)
                .NotEmpty()
                .MaximumLength(100);
        });

        When(item => item.HasDescription, () =>
        {
            RuleFor(item => item.Description)
                .MaximumLength(500);
        });

        When(item => item.HasBadgeText, () =>
        {
            RuleFor(item => item.BadgeText)
                .MaximumLength(100);
        });

        When(item => item.HasPoints, () =>
        {
            RuleFor(item => item.Points)
                .NotNull()
                .GreaterThan(0);
        });

        When(item => item.HasBonusPoints, () =>
        {
            RuleFor(item => item.BonusPoints)
                .NotNull()
                .GreaterThanOrEqualTo(0);
        });

        When(item => item.HasPriceVnd, () =>
        {
            RuleFor(item => item.PriceVnd)
                .NotNull()
                .GreaterThanOrEqualTo(0);
        });
    }
}
