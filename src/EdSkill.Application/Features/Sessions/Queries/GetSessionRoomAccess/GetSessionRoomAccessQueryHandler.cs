using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Queries.GetSessionRoomAccess;

public class GetSessionRoomAccessQueryHandler : IRequestHandler<GetSessionRoomAccessQuery, Result<SessionRoomAccessDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISystemConfigService _systemConfigService;

    public GetSessionRoomAccessQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ISystemConfigService systemConfigService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _systemConfigService = systemConfigService;
    }

    public async Task<Result<SessionRoomAccessDto>> Handle(GetSessionRoomAccessQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var session = await _context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SessionId == request.SessionId, cancellationToken);

        if (session == null)
        {
            return Result<SessionRoomAccessDto>.Failure("SESSION_NOT_FOUND", "Session was not found.");
        }

        if (session.CompanionId != userId && session.LearnerId != userId)
        {
            return Result<SessionRoomAccessDto>.Failure("FORBIDDEN", "You do not have access to this session room.");
        }

        var joinEarlyMinutes = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionJoinEarlyMinutes, cancellationToken);
        var joinLateGraceMinutes = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionJoinLateGraceMinutes, cancellationToken);
        var decision = SessionRoomAccessPolicy.Evaluate(session, _dateTimeProvider.UtcNow, joinEarlyMinutes, joinLateGraceMinutes);

        var currentUser = await _context.Users
            .AsNoTracking()
            .Include(item => item.UserProfile)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        var displayName = string.IsNullOrWhiteSpace(currentUser?.UserProfile?.DisplayName)
            ? currentUser?.Username ?? "User"
            : currentUser!.UserProfile!.DisplayName;
        var avatarUrl = currentUser?.UserProfile?.AvatarUrl;
        var role = session.CompanionId == userId ? "companion" : "learner";

        return Result<SessionRoomAccessDto>.Success(new SessionRoomAccessDto(
            session.SessionId,
            session.JitsiRoomId,
            SessionRoomAccessPolicy.JitsiDomain,
            displayName,
            avatarUrl,
            role,
            session.Status,
            decision.CanJoin,
            decision.DenyCode,
            decision.DenyMessage,
            session.ScheduledAt,
            decision.Window.DurationMinutes,
            decision.Window.JoinOpenAt,
            decision.Window.JoinCloseAt));
    }
}
