using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Queries.GetSessions;

public class GetSessionsQueryHandler : IRequestHandler<GetSessionsQuery, Result<SessionListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetSessionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SessionListDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var query = _context.Sessions.AsNoTracking().AsQueryable();

        if (string.Equals(request.Role, "companion", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(item => item.CompanionId == userId);
        }
        else if (string.Equals(request.Role, "learner", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(item => item.LearnerId == userId);
        }
        else
        {
            query = query.Where(item =>
                item.Status == SessionStatus.Available
                || item.CompanionId == userId
                || item.LearnerId == userId);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<SessionStatus>(request.Status, true, out var sessionStatus))
            {
                return Result<SessionListDto>.Failure("SESSION_STATUS_INVALID", "Session status is invalid.");
            }

            query = query.Where(item => item.Status == sessionStatus);
        }

        var total = await query.CountAsync(cancellationToken);
        var sessions = await query
            .OrderBy(item => item.ScheduledAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        return Result<SessionListDto>.Success(new SessionListDto(
            sessions.Select(SessionDtoMapper.Map).ToList(),
            total,
            request.Page,
            request.Limit));
    }
}
