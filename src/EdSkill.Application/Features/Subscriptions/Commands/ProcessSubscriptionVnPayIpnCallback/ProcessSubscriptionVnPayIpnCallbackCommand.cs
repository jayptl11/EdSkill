using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Subscriptions.Commands.ProcessSubscriptionVnPayIpnCallback;

public record ProcessSubscriptionVnPayIpnCallbackCommand(IReadOnlyDictionary<string, string> Payload)
    : IRequest<Result<VnPayIpnResponseDto>>;
