using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Application.Features.Sessions;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.MySpace.Queries.GetMySpace;

public class GetMySpaceQueryHandler : IRequestHandler<GetMySpaceQuery, Result<MySpaceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISessionPricingService _sessionPricingService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISystemConfigService _systemConfigService;

    public GetMySpaceQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISessionPricingService sessionPricingService,
        IDateTimeProvider dateTimeProvider,
        ISystemConfigService systemConfigService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sessionPricingService = sessionPricingService;
        _dateTimeProvider = dateTimeProvider;
        _systemConfigService = systemConfigService;
    }

    public async Task<Result<MySpaceDto>> Handle(GetMySpaceQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();

        var companionSessions = await _context.Sessions
            .AsNoTracking()
            .Where(session => session.CompanionId == userId)
            .OrderByDescending(session => session.ScheduledAt)
            .ToListAsync(cancellationToken);

        var learnerSessions = await _context.Sessions
            .AsNoTracking()
            .Where(session => session.LearnerId == userId)
            .OrderByDescending(session => session.ScheduledAt)
            .ToListAsync(cancellationToken);

        var allSessions = companionSessions.Concat(learnerSessions).ToList();
        if (allSessions.Count == 0)
        {
            return Result<MySpaceDto>.Success(MySpaceDtoMapper.Map([], []));
        }

        var skillIds = allSessions
            .Where(session => session.SkillId.HasValue)
            .Select(session => session.SkillId!.Value)
            .Distinct()
            .ToList();

        var skills = skillIds.Count == 0
            ? new List<Skill>()
            : await _context.Skills
                .AsNoTracking()
                .Where(skill => skillIds.Contains(skill.SkillId))
                .ToListAsync(cancellationToken);

        var userIds = allSessions
            .SelectMany(session =>
                session.LearnerId.HasValue
                    ? new[] { session.CompanionId, session.LearnerId.Value }
                    : new[] { session.CompanionId })
            .Distinct()
            .ToList();

        var users = await _context.Users
            .AsNoTracking()
            .Include(user => user.UserProfile)
            .Where(user => userIds.Contains(user.UserId))
            .ToListAsync(cancellationToken);

        var userLookup = users.ToDictionary(
            user => user.UserId,
            user => new MySpaceUserSummaryDto(
                user.UserId,
                string.IsNullOrWhiteSpace(user.UserProfile?.DisplayName) ? user.Username : user.UserProfile!.DisplayName,
                user.UserProfile?.AvatarUrl));

        var companionProfileLookup = users
            .Where(user => user.UserProfile is not null)
            .ToDictionary(user => user.UserId, user => user.UserProfile!);
        var skillLookup = skills.ToDictionary(skill => skill.SkillId);
        var sessionIds = allSessions.Select(session => session.SessionId).Distinct().ToList();

        var needsPlatformMarkup = allSessions.Any(session =>
            session.PricingModel == SessionPricingModel.FormulaV1
            && (!session.LearnerChargePoints.HasValue
                || !session.CompanionPayoutPoints.HasValue
                || !session.PlatformFeePoints.HasValue));
        var platformMarkupPct = needsPlatformMarkup
            ? await _sessionPricingService.GetPlatformMarkupPctAsync(cancellationToken)
            : (int?)null;
        var joinEarlyMinutes = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionJoinEarlyMinutes, cancellationToken);
        var joinLateGraceMinutes = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.SessionJoinLateGraceMinutes, cancellationToken);
        var openPresenceSegments = await _context.SessionPresenceSegments
            .AsNoTracking()
            .Where(segment => sessionIds.Contains(segment.SessionId) && !segment.LeftAt.HasValue)
            .ToListAsync(cancellationToken);
        var openPresenceBySession = openPresenceSegments
            .GroupBy(segment => segment.SessionId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var companionSessionDtos = companionSessions
            .Select(session =>
            {
                skillLookup.TryGetValue(session.SkillId ?? Guid.Empty, out var skill);
                companionProfileLookup.TryGetValue(session.CompanionId, out var companionProfile);
                return MySpaceDtoMapper.MapSession(
                    session,
                    skill,
                    companionProfile,
                    platformMarkupPct,
                    userLookup,
                    BuildRoomAccessSummary(
                        session,
                        userId,
                        joinEarlyMinutes,
                        joinLateGraceMinutes,
                        openPresenceBySession));
            })
            .ToList();

        var learnerSessionDtos = learnerSessions
            .Select(session =>
            {
                skillLookup.TryGetValue(session.SkillId ?? Guid.Empty, out var skill);
                companionProfileLookup.TryGetValue(session.CompanionId, out var companionProfile);
                return MySpaceDtoMapper.MapSession(
                    session,
                    skill,
                    companionProfile,
                    platformMarkupPct,
                    userLookup,
                    BuildRoomAccessSummary(
                        session,
                        userId,
                        joinEarlyMinutes,
                        joinLateGraceMinutes,
                        openPresenceBySession));
            })
            .ToList();

        return Result<MySpaceDto>.Success(MySpaceDtoMapper.Map(companionSessionDtos, learnerSessionDtos));
    }

    private MySpaceRoomAccessDto? BuildRoomAccessSummary(
        Session session,
        Guid currentUserId,
        int joinEarlyMinutes,
        int joinLateGraceMinutes,
        IReadOnlyDictionary<Guid, List<SessionPresenceSegment>> openPresenceBySession)
    {
        if (session.DeliveryMode != SessionDeliveryMode.Online)
        {
            return null;
        }

        var isCompanion = session.CompanionId == currentUserId;
        var hasCompanionJoined = openPresenceBySession.TryGetValue(session.SessionId, out var openSegments)
            && openSegments.Any(segment => segment.UserId == session.CompanionId);
        var decision = SessionRoomAccessPolicy.Evaluate(
            session,
            _dateTimeProvider.UtcNow,
            joinEarlyMinutes,
            joinLateGraceMinutes,
            isCompanion,
            hasCompanionJoined);
        var canOpenRoomPage = decision.CanJoin || decision.DenyCode == "SESSION_HOST_NOT_READY";

        return new MySpaceRoomAccessDto(
            canOpenRoomPage,
            decision.CanJoin,
            hasCompanionJoined,
            hasCompanionJoined,
            decision.DenyCode,
            decision.DenyMessage,
            decision.Window.JoinOpenAt,
            decision.Window.JoinCloseAt);
    }
}
