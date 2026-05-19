using FluentValidation;

namespace EdSkill.Application.Features.Wallet.Commands.RetryPointPurchase;

public class RetryPointPurchaseCommandValidator : AbstractValidator<RetryPointPurchaseCommand>
{
    public RetryPointPurchaseCommandValidator()
    {
        RuleFor(item => item.PaymentTransactionId)
            .NotEmpty()
            .WithErrorCode("PAYMENT_TRANSACTION_NOT_FOUND");
    }
}
