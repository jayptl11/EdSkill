using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Queries.GetSessionById;

public class GetSessionByIdQueryHandler : IRequestHandler<GetSessionByIdQuery, Result<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetSessionByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SessionDto>> Handle(GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var session = await _context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SessionId == request.SessionId, cancellationToken);

        if (session == null)
        {
            return Result<SessionDto>.Failure("SESSION_NOT_FOUND", "Session was not found.");
        }

        var canAccess = session.Status == SessionStatus.Available
            || session.CompanionId == userId
            || session.LearnerId == userId;

        if (!canAccess)
        {
            return Result<SessionDto>.Failure("FORBIDDEN", "You do not have access to this session.");
        }

        return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
    }
}
