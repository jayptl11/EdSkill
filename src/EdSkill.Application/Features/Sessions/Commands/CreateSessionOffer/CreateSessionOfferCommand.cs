using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Commands.CreateSessionOffer;

public record CreateSessionOfferCommand(
    Guid SkillId,
    string? Description,
    IReadOnlyCollection<int> DurationOptions,
    DateTime ScheduledAt) : IRequest<Result<SessionDto>>;
