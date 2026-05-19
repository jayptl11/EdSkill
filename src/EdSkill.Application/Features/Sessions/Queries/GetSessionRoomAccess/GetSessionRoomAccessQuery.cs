using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Queries.GetSessionRoomAccess;

public record GetSessionRoomAccessQuery(Guid SessionId) : IRequest<Result<SessionRoomAccessDto>>;
