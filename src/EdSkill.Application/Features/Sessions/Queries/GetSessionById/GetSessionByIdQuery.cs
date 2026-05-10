using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Queries.GetSessionById;

public record GetSessionByIdQuery(Guid SessionId) : IRequest<Result<SessionDto>>;
