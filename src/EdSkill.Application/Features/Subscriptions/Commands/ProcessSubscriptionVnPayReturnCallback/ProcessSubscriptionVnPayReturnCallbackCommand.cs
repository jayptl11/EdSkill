using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Subscriptions.Commands.ProcessSubscriptionVnPayReturnCallback;

public record ProcessSubscriptionVnPayReturnCallbackCommand(IReadOnlyDictionary<string, string> Payload)
    : IRequest<Result<SubscriptionPurchaseReturnResultDto>>;
