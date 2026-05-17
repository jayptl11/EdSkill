using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Subscriptions.Queries.GetMySubscriptions;

public record GetMySubscriptionsQuery() : IRequest<Result<MySubscriptionsDto>>;
