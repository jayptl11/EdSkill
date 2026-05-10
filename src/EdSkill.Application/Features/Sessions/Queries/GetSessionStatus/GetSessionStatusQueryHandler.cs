using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Queries.GetSessionStatus;

public class GetSessionStatusQueryHandler : IRequestHandler<GetSessionStatusQuery, Result<SessionStatusDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetSessionStatusQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SessionStatusDto>> Handle(GetSessionStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var session = await _context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SessionId == request.SessionId, cancellationToken);

        if (session == null)
        {
            return Result<SessionStatusDto>.Failure("SESSION_NOT_FOUND", "Session was not found.");
        }

        if (session.Status != SessionStatus.Available && session.CompanionId != userId && session.LearnerId != userId)
        {
            return Result<SessionStatusDto>.Failure("FORBIDDEN", "You do not have access to this session.");
        }

        return Result<SessionStatusDto>.Success(new SessionStatusDto(
            session.Status,
            session.LearnerConfirmed,
            session.CompanionConfirmed));
    }
}
