using FluentValidation;

namespace EdSkill.Application.Features.Subscriptions.Commands.ProcessSubscriptionVnPayIpnCallback;

public class ProcessSubscriptionVnPayIpnCallbackCommandValidator : AbstractValidator<ProcessSubscriptionVnPayIpnCallbackCommand>
{
    public ProcessSubscriptionVnPayIpnCallbackCommandValidator()
    {
        RuleFor(item => item.Payload)
            .NotNull()
            .Must(payload => payload.Count > 0)
            .WithErrorCode("PAYMENT_CALLBACK_INVALID");
    }
}
