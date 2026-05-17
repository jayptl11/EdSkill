using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Wallet.Commands.ProcessVnPayIpnCallback;

public class ProcessVnPayIpnCallbackCommandHandler : IRequestHandler<ProcessVnPayIpnCallbackCommand, Result<VnPayIpnResponseDto>>
{
    private readonly IWalletPaymentProcessingService _walletPaymentProcessingService;

    public ProcessVnPayIpnCallbackCommandHandler(IWalletPaymentProcessingService walletPaymentProcessingService)
    {
        _walletPaymentProcessingService = walletPaymentProcessingService;
    }

    public async Task<Result<VnPayIpnResponseDto>> Handle(ProcessVnPayIpnCallbackCommand request, CancellationToken cancellationToken)
    {
        var result = await _walletPaymentProcessingService.ProcessVnPayCallbackAsync(request.Payload, cancellationToken);
        if (result.IsSuccess)
        {
            return Result<VnPayIpnResponseDto>.Success(new VnPayIpnResponseDto("00", "Confirm Success"));
        }

        var response = result.ErrorCode switch
        {
            "PAYMENT_TRANSACTION_NOT_FOUND" => new VnPayIpnResponseDto("01", "Order not found"),
            "PAYMENT_ALREADY_PROCESSED" => new VnPayIpnResponseDto("02", "Order already confirmed"),
            "PAYMENT_CALLBACK_INVALID" => new VnPayIpnResponseDto("04", "Invalid amount or payload"),
            "PAYMENT_PROVIDER_INVALID_SIGNATURE" => new VnPayIpnResponseDto("97", "Invalid signature"),
            _ => new VnPayIpnResponseDto("99", "Unknown error")
        };

        return Result<VnPayIpnResponseDto>.Success(response);
    }
}
