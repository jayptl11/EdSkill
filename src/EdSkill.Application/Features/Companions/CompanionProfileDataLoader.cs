using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Achievements;
using EdSkill.Application.Features.Achievements.DTOs;
using EdSkill.Application.Features.Companions.DTOs;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Companions;

internal static class CompanionProfileDataLoader
{
    public static IReadOnlyCollection<CompanionTeachingSkillDto> BuildTeachingSkills(
        User companion,
        IReadOnlyDictionary<Guid, List<SessionDto>> availableOffersBySkillId)
    {
        return companion.UserSkills
            .Where(userSkill => userSkill.Type == UserSkillType.Teach && userSkill.Skill is not null)
            .Select(userSkill => userSkill.Skill)
            .GroupBy(skill => skill.SkillId)
            .Select(group =>
            {
                var skill = group.First();
                var offers = availableOffersBySkillId.TryGetValue(skill.SkillId, out var items)
                    ? items
                    : [];

                return new CompanionTeachingSkillDto(
                    skill.SkillId,
                    skill.Name,
                    skill.IconKey,
                    offers.Count,
                    offers.Count == 0 ? null : offers.Min(item => item.PointCost),
                    offers.Count == 0 ? null : offers.Min(item => item.ScheduledAt),
                    offers.Count > 0);
            })
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static async Task<List<AchievementSummaryDto>> LoadAchievementsAsync(
        IApplicationDbContext context,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var achievements = await context.UserAchievements
            .AsNoTracking()
            .Include(item => item.AchievementDefinition)
            .Where(item => item.UserId == userId && item.AchievementDefinition.IsActive)
            .OrderBy(item => item.AchievementDefinition.SortOrder)
            .ThenBy(item => item.AwardedAt)
            .ToListAsync(cancellationToken);

        return achievements
            .Select(AchievementDtoMapper.MapSummary)
            .ToList();
    }

    public static async Task<(double AvgRating, int TotalReviews, List<CompanionReviewDto> Reviews)> LoadReviewsAsync(
        IApplicationDbContext context,
        Guid companionId,
        int reviewPage,
        int reviewLimit,
        CancellationToken cancellationToken)
    {
        var reviewBaseQuery =
            from review in context.Reviews.AsNoTracking()
            join session in context.Sessions.AsNoTracking() on review.SessionId equals session.SessionId
            where review.RevieweeId == companionId
                  && session.CompanionId == companionId
            select review;

        var totalReviews = await reviewBaseQuery.CountAsync(cancellationToken);
        var avgRating = totalReviews == 0
            ? 0d
            : Math.Round(await reviewBaseQuery.AverageAsync(review => review.Rating, cancellationToken), 2);

        var reviewPageItems = await reviewBaseQuery
            .OrderByDescending(review => review.CreatedAt)
            .Skip((reviewPage - 1) * reviewLimit)
            .Take(reviewLimit)
            .ToListAsync(cancellationToken);

        var reviewerIds = reviewPageItems.Select(review => review.ReviewerId).Distinct().ToList();
        var reviewers = await context.Users
            .AsNoTracking()
            .Include(user => user.UserProfile)
            .Where(user => reviewerIds.Contains(user.UserId))
            .ToListAsync(cancellationToken);

        var reviewerLookup = reviewers.ToDictionary(
            user => user.UserId,
            user => string.IsNullOrWhiteSpace(user.UserProfile?.DisplayName)
                ? user.Username
                : user.UserProfile!.DisplayName);

        var reviewDtos = reviewPageItems
            .Select(review => new CompanionReviewDto(
                review.ReviewId,
                review.Rating,
                review.Comment,
                reviewerLookup.TryGetValue(review.ReviewerId, out var displayName) ? displayName : "Unknown",
                review.CreatedAt))
            .ToList();

        return (avgRating, totalReviews, reviewDtos);
    }

    public static async Task<List<SessionDto>> LoadSkillOffersAsync(
        IApplicationDbContext context,
        ISessionPricingService sessionPricingService,
        User companion,
        Skill skill,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var candidateSessions = (await CompanionDiscoveryMatcher
                .LoadAvailableOnlineSkillSessionsAsync(
                    context.Sessions.AsNoTracking().Where(session => session.CompanionId == companion.UserId),
                    skill,
                    utcNow,
                    cancellationToken))
            .OrderByDescending(session => session.CreatedAt)
            .ThenByDescending(session => session.ScheduledAt)
            .ToList();

        var platformMarkupPct = candidateSessions.Any(session => session.PricingModel == SessionPricingModel.FormulaV1)
            ? await sessionPricingService.GetPlatformMarkupPctAsync(cancellationToken)
            : (int?)null;

        return CompanionDiscoveryMatcher.MatchOffers(
                candidateSessions,
                skill,
                new Dictionary<Guid, UserProfile> { [companion.UserId] = companion.UserProfile! },
                platformMarkupPct,
                new CompanionDiscoveryFilters(null, null, null))
            .Select(item => item.Offer)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.ScheduledAt)
            .ThenBy(item => item.PointCost)
            .ToList();
    }
}
