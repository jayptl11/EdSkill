using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Commands.ConfirmSession;

public record ConfirmSessionCommand(Guid SessionId) : IRequest<Result<SessionDto>>;
