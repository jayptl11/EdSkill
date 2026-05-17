using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Subscriptions.Commands.ProcessSubscriptionVnPayReturnCallback;

public class ProcessSubscriptionVnPayReturnCallbackCommandHandler : IRequestHandler<ProcessSubscriptionVnPayReturnCallbackCommand, Result<SubscriptionPurchaseReturnResultDto>>
{
    private readonly IWalletPaymentProcessingService _walletPaymentProcessingService;

    public ProcessSubscriptionVnPayReturnCallbackCommandHandler(IWalletPaymentProcessingService walletPaymentProcessingService)
    {
        _walletPaymentProcessingService = walletPaymentProcessingService;
    }

    public async Task<Result<SubscriptionPurchaseReturnResultDto>> Handle(ProcessSubscriptionVnPayReturnCallbackCommand request, CancellationToken cancellationToken)
    {
        var result = await _walletPaymentProcessingService.ProcessVnPayCallbackAsync(request.Payload, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<SubscriptionPurchaseReturnResultDto>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        var value = result.Value!;
        return Result<SubscriptionPurchaseReturnResultDto>.Success(
            new SubscriptionPurchaseReturnResultDto(
                value.PaymentTransactionId,
                value.SubscriptionPlanId,
                value.SubscriptionPlanName,
                value.Status,
                value.CreditedPoints,
                value.AlreadyProcessed));
    }
}
