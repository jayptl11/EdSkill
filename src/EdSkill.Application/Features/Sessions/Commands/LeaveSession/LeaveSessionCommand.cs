using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Commands.LeaveSession;

public record LeaveSessionCommand(Guid SessionId, int? ActualDuration) : IRequest<Result<SessionDto>>;
