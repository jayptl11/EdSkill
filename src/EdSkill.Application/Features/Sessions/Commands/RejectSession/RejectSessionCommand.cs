using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Commands.RejectSession;

public record RejectSessionCommand(Guid SessionId, string? Reason) : IRequest<Result<SessionDto>>;
