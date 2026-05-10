using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Commands.CreateSessionOffer;

public record CreateSessionOfferCommand(
    Guid SkillId,
    string? Description,
    SessionDeliveryMode DeliveryMode,
    string? Location,
    int DurationMinutes,
    int PointCost,
    DateTime ScheduledAt) : IRequest<Result<SessionDto>>;
