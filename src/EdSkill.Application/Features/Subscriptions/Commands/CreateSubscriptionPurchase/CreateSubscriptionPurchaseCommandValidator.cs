using FluentValidation;

namespace EdSkill.Application.Features.Subscriptions.Commands.CreateSubscriptionPurchase;

public class CreateSubscriptionPurchaseCommandValidator : AbstractValidator<CreateSubscriptionPurchaseCommand>
{
    public CreateSubscriptionPurchaseCommandValidator()
    {
        RuleFor(item => item.PlanId)
            .NotEmpty()
            .WithErrorCode("SUBSCRIPTION_PLAN_NOT_FOUND");
    }
}
