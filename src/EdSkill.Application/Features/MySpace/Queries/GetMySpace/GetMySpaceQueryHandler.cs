using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
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

    public GetMySpaceQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISessionPricingService sessionPricingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sessionPricingService = sessionPricingService;
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

        var needsPlatformMarkup = allSessions.Any(session =>
            session.PricingModel == SessionPricingModel.FormulaV1
            && (!session.LearnerChargePoints.HasValue
                || !session.CompanionPayoutPoints.HasValue
                || !session.PlatformFeePoints.HasValue));
        var platformMarkupPct = needsPlatformMarkup
            ? await _sessionPricingService.GetPlatformMarkupPctAsync(cancellationToken)
            : (int?)null;

        var companionSessionDtos = companionSessions
            .Select(session =>
            {
                skillLookup.TryGetValue(session.SkillId ?? Guid.Empty, out var skill);
                companionProfileLookup.TryGetValue(session.CompanionId, out var companionProfile);
                return MySpaceDtoMapper.MapSession(session, skill, companionProfile, platformMarkupPct, userLookup);
            })
            .ToList();

        var learnerSessionDtos = learnerSessions
            .Select(session =>
            {
                skillLookup.TryGetValue(session.SkillId ?? Guid.Empty, out var skill);
                companionProfileLookup.TryGetValue(session.CompanionId, out var companionProfile);
                return MySpaceDtoMapper.MapSession(session, skill, companionProfile, platformMarkupPct, userLookup);
            })
            .ToList();

        return Result<MySpaceDto>.Success(MySpaceDtoMapper.Map(companionSessionDtos, learnerSessionDtos));
    }
}
