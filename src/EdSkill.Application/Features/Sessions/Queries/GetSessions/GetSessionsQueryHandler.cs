using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Queries.GetSessions;

public class GetSessionsQueryHandler : IRequestHandler<GetSessionsQuery, Result<SessionListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISessionPricingService _sessionPricingService;

    public GetSessionsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISessionPricingService sessionPricingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sessionPricingService = sessionPricingService;
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

        var formulaPreviewSessions = sessions
            .Where(session => session.PricingModel == SessionPricingModel.FormulaV1
                && (!session.LearnerChargePoints.HasValue || !session.CompanionPayoutPoints.HasValue || !session.PlatformFeePoints.HasValue))
            .ToList();

        var skillIds = formulaPreviewSessions
            .Where(session => session.SkillId.HasValue)
            .Select(session => session.SkillId!.Value)
            .Distinct()
            .ToList();
        var companionIds = formulaPreviewSessions
            .Select(session => session.CompanionId)
            .Distinct()
            .ToList();

        var skills = skillIds.Count == 0
            ? new List<Skill>()
            : await _context.Skills
                .AsNoTracking()
                .Where(item => skillIds.Contains(item.SkillId) && item.IsActive && !item.IsDeleted)
                .ToListAsync(cancellationToken);
        var profiles = companionIds.Count == 0
            ? new List<UserProfile>()
            : await _context.UserProfiles
                .AsNoTracking()
                .Where(item => companionIds.Contains(item.UserId))
                .ToListAsync(cancellationToken);

        var skillLookup = skills.ToDictionary(item => item.SkillId);
        var profileLookup = profiles.ToDictionary(item => item.UserId);
        var platformMarkupPct = formulaPreviewSessions.Count == 0
            ? (int?)null
            : await _sessionPricingService.GetPlatformMarkupPctAsync(cancellationToken);

        return Result<SessionListDto>.Success(new SessionListDto(
            sessions.Select(session =>
            {
                skillLookup.TryGetValue(session.SkillId ?? Guid.Empty, out var skill);
                profileLookup.TryGetValue(session.CompanionId, out var companionProfile);
                return SessionDtoMapper.Map(session, skill, companionProfile, platformMarkupPct);
            }).ToList(),
            total,
            request.Page,
            request.Limit));
    }
}
