using FluentValidation;

namespace EdSkill.Application.Features.Admin.Commands.CreatePointPackage;

public class CreatePointPackageCommandValidator : AbstractValidator<CreatePointPackageCommand>
{
    public CreatePointPackageCommandValidator()
    {
        RuleFor(item => item.Code)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(item => item.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(item => item.Description)
            .MaximumLength(500);

        RuleFor(item => item.BadgeText)
            .MaximumLength(100);

        RuleFor(item => item.Points)
            .GreaterThan(0);

        RuleFor(item => item.BonusPoints)
            .GreaterThanOrEqualTo(0);

        RuleFor(item => item.PriceVnd)
            .GreaterThanOrEqualTo(0);

        RuleFor(item => item)
            .Must(item => !item.StartsAt.HasValue || !item.EndsAt.HasValue || item.StartsAt.Value <= item.EndsAt.Value)
            .WithErrorCode("POINT_PACKAGE_INVALID_TIME_WINDOW")
            .WithMessage("Point package start time must be before or equal to end time.");
    }
}
