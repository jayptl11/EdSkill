using FluentValidation;

namespace EdSkill.Application.Features.Wallet.Commands.ProcessVnPayIpnCallback;

public class ProcessVnPayIpnCallbackCommandValidator : AbstractValidator<ProcessVnPayIpnCallbackCommand>
{
    public ProcessVnPayIpnCallbackCommandValidator()
    {
        RuleFor(item => item.Payload)
            .Must(payload => payload.Count > 0)
            .WithErrorCode("PAYMENT_CALLBACK_INVALID")
            .WithMessage("VNPay callback payload is required.");
    }
}
