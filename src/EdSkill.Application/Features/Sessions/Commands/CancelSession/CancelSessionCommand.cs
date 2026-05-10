using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Commands.CancelSession;

public record CancelSessionCommand(Guid SessionId, string? Reason) : IRequest<Result<SessionDto>>;
