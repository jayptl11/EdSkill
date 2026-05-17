using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Subscriptions.Commands.CreateSubscriptionPurchase;

public record CreateSubscriptionPurchaseCommand(Guid PlanId) : IRequest<Result<CreateSubscriptionPurchaseResultDto>>;
