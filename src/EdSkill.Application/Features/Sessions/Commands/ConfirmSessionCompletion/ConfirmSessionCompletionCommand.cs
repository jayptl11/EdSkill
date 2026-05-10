using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Commands.ConfirmSessionCompletion;

public record ConfirmSessionCompletionCommand(Guid SessionId) : IRequest<Result<SessionDto>>;
