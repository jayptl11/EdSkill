using FluentValidation;

namespace EdSkill.Application.Features.Wallet.Commands.ProcessVnPayReturnCallback;

public class ProcessVnPayReturnCallbackCommandValidator : AbstractValidator<ProcessVnPayReturnCallbackCommand>
{
    public ProcessVnPayReturnCallbackCommandValidator()
    {
        RuleFor(item => item.Payload)
            .Must(payload => payload.Count > 0)
            .WithErrorCode("PAYMENT_CALLBACK_INVALID")
            .WithMessage("VNPay callback payload is required.");
    }
}
