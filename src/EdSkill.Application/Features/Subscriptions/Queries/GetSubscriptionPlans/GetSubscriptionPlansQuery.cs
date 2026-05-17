using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Subscriptions.Queries.GetSubscriptionPlans;

public record GetSubscriptionPlansQuery() : IRequest<Result<SubscriptionPlanListDto>>;
