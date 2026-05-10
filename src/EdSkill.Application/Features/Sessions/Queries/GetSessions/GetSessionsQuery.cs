using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Sessions.Queries.GetSessions;

public record GetSessionsQuery(string? Status, string? Role, int Page = 1, int Limit = 20) : IRequest<Result<SessionListDto>>;
