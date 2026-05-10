using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Commands.JoinSession;

public record JoinSessionCommand(Guid SessionId) : IRequest<Result<SessionDto>>;
