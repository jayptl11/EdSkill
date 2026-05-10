using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Queries.GetSessionStatus;

public record GetSessionStatusQuery(Guid SessionId) : IRequest<Result<SessionStatusDto>>;
