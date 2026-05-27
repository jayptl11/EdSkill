using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using EdSkill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.API.Realtime;

public class SessionRealtimeSnapshotBuilder : ISessionRealtimeSnapshotBuilder
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ISystemConfigService _systemConfigService;

    public SessionRealtimeSnapshotBuilder(
        IDbContextFactory<AppDbContext> dbContextFactory,
        ISystemConfigService systemConfigService)
    {
        _dbContextFactory = dbContextFactory;
        _systemConfigService = systemConfigService;
    }

    public async Task<bool> CanUserAccessSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var session = await dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SessionId == sessionId, cancellationToken);

        return session is not null
            && (session.CompanionId == userId || session.LearnerId == userId);
    }

    public async Task<SessionRoomStateSnapshot?> BuildRoomStateSnapshotAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var session = await dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SessionId == sessionId, cancellationToken);

        if (session == null || session.DeliveryMode != SessionDeliveryMode.Online)
        {
            return null;
        }

        var joinEarlyMinutes = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionJoinEarlyMinutes, cancellationToken);
        var joinLateGraceMinutes = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionJoinLateGraceMinutes, cancellationToken);
        var joinWindow = SessionRoomAccessPolicy.BuildJoinWindow(session, joinEarlyMinutes, joinLateGraceMinutes);

        var openUserIds = await dbContext.SessionPresenceSegments
            .AsNoTracking()
            .Where(item => item.SessionId == sessionId && !item.LeftAt.HasValue)
            .Select(item => item.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var payload = new SessionRoomStateDto(
            session.SessionId,
            session.Status,
            session.JitsiRoomId,
            openUserIds.Contains(session.CompanionId),
            session.LearnerId.HasValue && openUserIds.Contains(session.LearnerId.Value),
            openUserIds.Count,
            session.ActualStartAt,
            session.ActualEndAt,
            session.ActualDuration,
            joinWindow.JoinOpenAt,
            joinWindow.JoinCloseAt,
            session.UpdatedAt);

        return new SessionRoomStateSnapshot(
            payload,
            SessionRealtimeGroupNames.ParticipantUsers(session.CompanionId, session.LearnerId));
    }
}
