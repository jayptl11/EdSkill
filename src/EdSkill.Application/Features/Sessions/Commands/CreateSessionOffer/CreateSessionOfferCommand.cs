using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Commands.CreateSessionOffer;

public record CreateSessionOfferCommand(
    string Skill,
    string? Description,
    int DurationMinutes,
    int PointCost,
    DateTime ScheduledAt) : IRequest<Result<SessionDto>>;
