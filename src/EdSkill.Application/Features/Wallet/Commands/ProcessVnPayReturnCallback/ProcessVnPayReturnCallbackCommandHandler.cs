using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Wallet.Commands.ProcessVnPayReturnCallback;

public class ProcessVnPayReturnCallbackCommandHandler : IRequestHandler<ProcessVnPayReturnCallbackCommand, Result<VnPayReturnResultDto>>
{
    private readonly IWalletPaymentProcessingService _walletPaymentProcessingService;

    public ProcessVnPayReturnCallbackCommandHandler(IWalletPaymentProcessingService walletPaymentProcessingService)
    {
        _walletPaymentProcessingService = walletPaymentProcessingService;
    }

    public async Task<Result<VnPayReturnResultDto>> Handle(ProcessVnPayReturnCallbackCommand request, CancellationToken cancellationToken)
    {
        var result = await _walletPaymentProcessingService.ProcessVnPayCallbackAsync(request.Payload, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<VnPayReturnResultDto>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        var value = result.Value!;
        return Result<VnPayReturnResultDto>.Success(
            new VnPayReturnResultDto(
                value.PaymentTransactionId,
                value.PointPackageId,
                value.PackageName,
                value.SubscriptionPlanId,
                value.SubscriptionPlanName,
                value.Status,
                value.CreditedPoints,
                value.AlreadyProcessed));
    }
}
