using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Companions.Queries.SearchCompanions;

public class SearchCompanionsQueryHandler : IRequestHandler<SearchCompanionsQuery, Result<CompanionSearchResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISessionPricingService _sessionPricingService;

    public SearchCompanionsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISessionPricingService sessionPricingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sessionPricingService = sessionPricingService;
    }

    public async Task<Result<CompanionSearchResultDto>> Handle(SearchCompanionsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.TryGetUserId();

        var skill = await _context.Skills
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SkillId == request.SkillId && item.IsActive && !item.IsDeleted, cancellationToken);
        if (skill == null)
        {
            return Result<CompanionSearchResultDto>.Failure("SKILL_NOT_FOUND", "Skill was not found.");
        }

        var filteredSessions = (await CompanionSessionFilters
            .ApplyAsync(_context.Sessions.AsNoTracking(), skill, request.DeliveryMode, request.Location, cancellationToken))
            .OrderBy(session => session.ScheduledAt)
            .ToList();

        if (filteredSessions.Count == 0)
        {
            return Result<CompanionSearchResultDto>.Success(new CompanionSearchResultDto([], 0, request.Page, request.Limit));
        }

        var companionIds = filteredSessions.Select(session => session.CompanionId).Distinct().ToList();
        var companions = await _context.Users
            .AsNoTracking()
            .Include(user => user.UserProfile)
            .Include(user => user.UserSkills)
            .ThenInclude(userSkill => userSkill.Skill)
            .Where(user => companionIds.Contains(user.UserId))
            .ToListAsync(cancellationToken);

        var companionLookup = companions
            .Where(user => user.UserProfile != null)
            .ToDictionary(user => user.UserId, user => user.UserProfile!);
        var reviewStats = await LoadReviewStatsAsync(companionIds, cancellationToken);
        var platformMarkupPct = filteredSessions.Any(session => session.PricingModel == SessionPricingModel.FormulaV1)
            ? await _sessionPricingService.GetPlatformMarkupPctAsync(cancellationToken)
            : (int?)null;

        var sessionStats = filteredSessions
            .GroupBy(session => session.CompanionId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var previews = group
                        .Select(session =>
                        {
                            companionLookup.TryGetValue(session.CompanionId, out var profile);
                            return SessionDtoMapper.BuildPricingPreview(session, skill, profile, platformMarkupPct);
                        })
                        .ToList();

                    return new
                    {
                        MatchingSessionCount = group.Count(),
                        LowestPointCost = previews.Min(item => item.MinLearnerChargePoints),
                        PricingPreview = new SessionPricingPreviewDto(
                            previews.Min(item => item.MinCompanionPayoutPoints),
                            previews.Max(item => item.MaxCompanionPayoutPoints),
                            previews.Min(item => item.MinLearnerChargePoints),
                            previews.Max(item => item.MaxLearnerChargePoints),
                            previews.Min(item => item.MinPlatformFeePoints),
                            previews.Max(item => item.MaxPlatformFeePoints)),
                        NextScheduledAt = group.Min(item => item.ScheduledAt)
                    };
                });

        var items = companions
            .Where(user =>
                user.UserProfile?.IsPublic == true
                && user.Roles.Contains("companion")
                && sessionStats.ContainsKey(user.UserId)
                && (!currentUserId.HasValue || user.UserId != currentUserId.Value))
            .Select(user =>
            {
                var stats = sessionStats[user.UserId];
                var review = reviewStats.TryGetValue(user.UserId, out var value)
                    ? value
                    : (AvgRating: 0d, TotalReviews: 0);

                return new CompanionSearchItemDto(
                    user.UserId,
                    user.UserProfile!.DisplayName,
                    user.UserProfile.AvatarUrl,
                    user.UserProfile.Bio,
                    GetTeachSkills(user),
                    review.AvgRating,
                    review.TotalReviews,
                    stats.MatchingSessionCount,
                    stats.LowestPointCost,
                    stats.PricingPreview,
                    stats.NextScheduledAt);
            })
            .OrderBy(item => item.NextScheduledAt)
            .ThenBy(item => item.LowestPointCost)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = items.Count;
        var pagedItems = items
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToList();

        return Result<CompanionSearchResultDto>.Success(new CompanionSearchResultDto(
            pagedItems,
            total,
            request.Page,
            request.Limit));
    }

    private async Task<Dictionary<Guid, (double AvgRating, int TotalReviews)>> LoadReviewStatsAsync(
        IReadOnlyCollection<Guid> companionIds,
        CancellationToken cancellationToken)
    {
        var reviewRows = await (
            from review in _context.Reviews.AsNoTracking()
            join session in _context.Sessions.AsNoTracking() on review.SessionId equals session.SessionId
            where companionIds.Contains(review.RevieweeId)
                  && session.CompanionId == review.RevieweeId
            select new
            {
                review.RevieweeId,
                review.Rating
            })
            .ToListAsync(cancellationToken);

        return reviewRows
            .GroupBy(item => item.RevieweeId)
            .ToDictionary(
                group => group.Key,
                group => (
                    AvgRating: Math.Round(group.Average(item => item.Rating), 2),
                    TotalReviews: group.Count()));
    }

    private static IReadOnlyCollection<string> GetTeachSkills(User user)
    {
        return user.UserSkills
            .Where(userSkill => userSkill.Type == UserSkillType.Teach && userSkill.Skill is not null)
            .Select(userSkill => userSkill.Skill.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
