using FluentValidation;

namespace EdSkill.Application.Features.Subscriptions.Commands.ProcessSubscriptionVnPayReturnCallback;

public class ProcessSubscriptionVnPayReturnCallbackCommandValidator : AbstractValidator<ProcessSubscriptionVnPayReturnCallbackCommand>
{
    public ProcessSubscriptionVnPayReturnCallbackCommandValidator()
    {
        RuleFor(item => item.Payload)
            .NotNull()
            .Must(payload => payload.Count > 0)
            .WithErrorCode("PAYMENT_CALLBACK_INVALID");
    }
}
