using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using EdSkill.Application.Features.Profile;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Application.Features.Skills;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Companions.Queries.SearchCompanions;

public class SearchCompanionsQueryHandler : IRequestHandler<SearchCompanionsQuery, Result<CompanionSearchResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISessionPricingService _sessionPricingService;
    private readonly ISubscriptionEntitlementService _subscriptionEntitlementService;

    public SearchCompanionsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ISessionPricingService sessionPricingService,
        ISubscriptionEntitlementService subscriptionEntitlementService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _sessionPricingService = sessionPricingService;
        _subscriptionEntitlementService = subscriptionEntitlementService;
    }

    public async Task<Result<CompanionSearchResultDto>> Handle(SearchCompanionsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.TryGetUserId();
        var requestedSkillId = request.SkillId.HasValue && request.SkillId.Value != Guid.Empty
            ? request.SkillId
            : null;
        var filters = new CompanionDiscoveryFilters(
            request.MinimumDurationMinutes,
            request.MaxLearnerChargePoints,
            request.GetCredentialCountGroup());

        Skill? skill = null;
        if (requestedSkillId.HasValue)
        {
            skill = await _context.Skills
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.SkillId == requestedSkillId.Value && item.IsActive && !item.IsDeleted, cancellationToken);
            if (skill == null)
            {
                return Result<CompanionSearchResultDto>.Failure("SKILL_NOT_FOUND", "Skill was not found.");
            }
        }

        var candidateSessions = skill is null
            ? await CompanionDiscoveryMatcher.LoadAvailableOnlineSessionsAsync(_context.Sessions.AsNoTracking(), _dateTimeProvider.UtcNow, cancellationToken)
            : await CompanionDiscoveryMatcher.LoadAvailableOnlineSkillSessionsAsync(_context.Sessions.AsNoTracking(), skill, _dateTimeProvider.UtcNow, cancellationToken);

        if (candidateSessions.Count == 0)
        {
            return Result<CompanionSearchResultDto>.Success(new CompanionSearchResultDto([], 0, request.Page, request.Limit));
        }

        var companionIds = candidateSessions.Select(session => session.CompanionId).Distinct().ToList();
        var companions = await _context.Users
            .AsNoTracking()
            .Include(user => user.UserProfile)
            .Include(user => user.UserSkills)
            .ThenInclude(userSkill => userSkill.Skill)
            .Where(user => companionIds.Contains(user.UserId))
            .ToListAsync(cancellationToken);
        var eligibleCompanions = skill is null
            ? companions
            : companions
                .Where(user => CompanionDiscoveryMatcher.HasOwnedTeachingSkill(user, skill.SkillId))
                .ToList();
        var companionEntitlements = await _subscriptionEntitlementService.GetResolvedEntitlementsAsync(companionIds, cancellationToken);

        var companionLookup = eligibleCompanions
            .Where(user => user.UserProfile != null)
            .ToDictionary(user => user.UserId, user => user.UserProfile!);
        var eligibleCompanionIds = eligibleCompanions.Select(user => user.UserId).ToList();
        var reviewStats = await LoadReviewStatsAsync(eligibleCompanionIds, cancellationToken);
        var platformMarkupPct = candidateSessions.Any(session => session.PricingModel == SessionPricingModel.FormulaV1)
            ? await _sessionPricingService.GetPlatformMarkupPctAsync(cancellationToken)
            : (int?)null;
        var matchedOffers = skill is null
            ? await MatchOffersAcrossSkillsAsync(candidateSessions, eligibleCompanions, companionLookup, platformMarkupPct, filters, cancellationToken)
            : CompanionDiscoveryMatcher.MatchOffers(candidateSessions, skill, companionLookup, platformMarkupPct, filters);

        var useNewestOfferSort = requestedSkillId is null
            && request.MinimumDurationMinutes is null
            && request.MaxLearnerChargePoints is null
            && request.GetCredentialCountGroup() is null;
        var companionById = eligibleCompanions
            .Where(user => user.UserProfile != null)
            .ToDictionary(user => user.UserId);
        var teachSkillsByCompanionId = eligibleCompanions
            .Where(user => user.UserProfile != null)
            .ToDictionary(user => user.UserId, GetTeachSkills);

        var items = matchedOffers
            .Where(item =>
                companionById.TryGetValue(item.CompanionId, out var companion)
                && companion.UserProfile!.IsPublic
                && companion.Roles.Contains("companion")
                && (!currentUserId.HasValue || companion.UserId != currentUserId.Value))
            .Select(item =>
            {
                var companion = companionById[item.CompanionId];
                var review = reviewStats.TryGetValue(companion.UserId, out var value)
                    ? value
                    : (AvgRating: 0d, TotalReviews: 0);
                var entitlements = companionEntitlements.TryGetValue(companion.UserId, out var entitlementValue)
                    ? entitlementValue
                    : Common.Models.ResolvedSubscriptionEntitlements.Empty;

                return new CompanionSearchItemDto(
                    companion.UserId,
                    companion.UserProfile!.DisplayName,
                    companion.UserProfile.AvatarUrl,
                    companion.UserProfile.Bio,
                    teachSkillsByCompanionId[companion.UserId],
                    item.CredentialCount,
                    review.AvgRating,
                    review.TotalReviews,
                    item.Offer,
                    entitlements.CompanionBadgeText,
                    entitlements.HasPriorityVisibility);
            });

        items = useNewestOfferSort
            ? items
                .OrderByDescending(item => item.Offer.CreatedAt)
                .ThenByDescending(item => item.HasPriorityVisibility)
                .ThenByDescending(item => item.Offer.ScheduledAt)
                .ThenBy(item => item.Offer.PointCost)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            : items
                .OrderByDescending(item => item.HasPriorityVisibility)
                .ThenByDescending(item => item.Offer.CreatedAt)
                .ThenByDescending(item => item.Offer.ScheduledAt)
                .ThenBy(item => item.Offer.PointCost)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase);

        var sortedItems = items.ToList();

        var total = sortedItems.Count;
        var pagedItems = sortedItems
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

    private async Task<IReadOnlyCollection<MatchedCompanionOffer>> MatchOffersAcrossSkillsAsync(
        IReadOnlyCollection<Session> sessions,
        IReadOnlyCollection<User> companions,
        IReadOnlyDictionary<Guid, UserProfile> companionProfiles,
        int? platformMarkupPct,
        CompanionDiscoveryFilters filters,
        CancellationToken cancellationToken)
    {
        var activeSkills = await _context.Skills
            .AsNoTracking()
            .Where(item => item.IsActive && !item.IsDeleted)
            .ToListAsync(cancellationToken);
        var activeSkillById = activeSkills.ToDictionary(item => item.SkillId);
        var activeSkillLookup = SkillNormalization.BuildLookup(activeSkills);
        var companionLookup = companions.ToDictionary(item => item.UserId);
        var matchedOffers = new List<MatchedCompanionOffer>();

        foreach (var session in sessions)
        {
            if (!companionProfiles.TryGetValue(session.CompanionId, out var companionProfile)
                || !companionLookup.TryGetValue(session.CompanionId, out var companion))
            {
                continue;
            }

            var skill = ResolveSkill(session, activeSkillById, activeSkillLookup);
            if (skill is null || !CompanionDiscoveryMatcher.HasOwnedTeachingSkill(companion, skill.SkillId))
            {
                continue;
            }

            var credentialCount = CompanionCredentialRules.GetCredentialCount(companionProfile);
            if (!CompanionCredentialCountGroupParser.Matches(filters.CredentialCountGroup, credentialCount))
            {
                continue;
            }

            var matchedOffer = CompanionDiscoveryMatcher.MatchOffer(session, skill, companionProfile, platformMarkupPct, filters);
            if (matchedOffer is null)
            {
                continue;
            }

            matchedOffers.Add(new MatchedCompanionOffer(session.CompanionId, credentialCount, matchedOffer));
        }

        return matchedOffers;
    }

    private static Skill? ResolveSkill(
        Session session,
        IReadOnlyDictionary<Guid, Skill> activeSkillById,
        IReadOnlyDictionary<string, Skill> activeSkillLookup)
    {
        if (session.SkillId.HasValue && activeSkillById.TryGetValue(session.SkillId.Value, out var mappedById))
        {
            return mappedById;
        }

        if (string.IsNullOrWhiteSpace(session.Skill))
        {
            return null;
        }

        var normalized = SkillNormalization.NormalizeLookup(session.Skill);
        return activeSkillLookup.TryGetValue(normalized, out var mappedByName) ? mappedByName : null;
    }

}
