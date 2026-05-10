using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Commands.BookSession;

public record BookSessionCommand(Guid SessionId) : IRequest<Result<SessionDto>>;
