using EdSkill.Application.Features.Profile;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Application.Features.Skills;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Companions;

internal static class CompanionDiscoveryMatcher
{
    public static bool HasOwnedTeachingSkill(User user, Guid skillId)
    {
        return user.UserSkills.Any(userSkill =>
            userSkill.Type == UserSkillType.Teach
            && userSkill.SkillId == skillId
            && userSkill.Skill is not null
            && userSkill.Skill.IsActive
            && !userSkill.Skill.IsDeleted);
    }

    public static async Task<List<Session>> LoadAvailableOnlineSkillSessionsAsync(
        IQueryable<Session> query,
        Skill skill,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var sessions = await LoadAvailableOnlineSessionsAsync(query, utcNow, cancellationToken);

        var validSkillKeys = BuildSkillKeys(skill);
        return sessions
            .Where(session =>
                session.SkillId == skill.SkillId
                || validSkillKeys.Contains(SkillNormalization.NormalizeLookup(session.Skill)))
            .ToList();
    }

    public static Task<List<Session>> LoadAvailableOnlineSessionsAsync(
        IQueryable<Session> query,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return query
            .Where(session =>
                session.Status == SessionStatus.Available
                && session.DeliveryMode == SessionDeliveryMode.Online
                && session.ScheduledAt > utcNow)
            .ToListAsync(cancellationToken);
    }

    public static IReadOnlyCollection<MatchedCompanionOffer> MatchOffers(
        IEnumerable<Session> sessions,
        Skill skill,
        IReadOnlyDictionary<Guid, UserProfile> companionProfiles,
        int? platformMarkupPct,
        CompanionDiscoveryFilters filters)
    {
        var matchedOffers = new List<MatchedCompanionOffer>();

        foreach (var session in sessions)
        {
            if (!companionProfiles.TryGetValue(session.CompanionId, out var companionProfile))
            {
                continue;
            }

            var credentialCount = CompanionCredentialRules.GetCredentialCount(companionProfile);
            if (!CompanionCredentialCountGroupParser.Matches(filters.CredentialCountGroup, credentialCount))
            {
                continue;
            }

            var matchedOffer = MatchOffer(session, skill, companionProfile, platformMarkupPct, filters);
            if (matchedOffer is not null)
            {
                matchedOffers.Add(new MatchedCompanionOffer(session.CompanionId, credentialCount, matchedOffer));
            }
        }

        return matchedOffers;
    }

    public static SessionDto? MatchOffer(
        Session session,
        Skill skill,
        UserProfile companionProfile,
        int? platformMarkupPct,
        CompanionDiscoveryFilters filters)
    {
        var mapped = SessionDtoMapper.Map(session, skill, companionProfile, platformMarkupPct);
        if (session.PricingModel != SessionPricingModel.FormulaV1)
        {
            return OfferMatchesLegacyFilters(mapped, filters) ? mapped : null;
        }

        var matchedDurationOptions = mapped.DurationPricingOptions
            .Where(option =>
                (!filters.MinimumDurationMinutes.HasValue || option.DurationMinutes >= filters.MinimumDurationMinutes.Value)
                && (!filters.MaxLearnerChargePoints.HasValue || option.LearnerChargePoints <= filters.MaxLearnerChargePoints.Value))
            .OrderBy(option => option.DurationMinutes)
            .ToList();

        if (matchedDurationOptions.Count == 0)
        {
            return null;
        }

        var pricingPreview = new SessionPricingPreviewDto(
            matchedDurationOptions.Min(option => option.CompanionPayoutPoints),
            matchedDurationOptions.Max(option => option.CompanionPayoutPoints),
            matchedDurationOptions.Min(option => option.LearnerChargePoints),
            matchedDurationOptions.Max(option => option.LearnerChargePoints),
            matchedDurationOptions.Min(option => option.PlatformFeePoints),
            matchedDurationOptions.Max(option => option.PlatformFeePoints));

        return mapped with
        {
            DurationMinutes = matchedDurationOptions.Max(option => option.DurationMinutes),
            PointCost = matchedDurationOptions.Min(option => option.LearnerChargePoints),
            DurationOptions = matchedDurationOptions.Select(option => option.DurationMinutes).ToList(),
            DurationPricingOptions = matchedDurationOptions,
            PricingPreview = pricingPreview
        };
    }

    private static bool OfferMatchesLegacyFilters(SessionDto offer, CompanionDiscoveryFilters filters)
    {
        if (filters.MinimumDurationMinutes.HasValue && offer.DurationMinutes < filters.MinimumDurationMinutes.Value)
        {
            return false;
        }

        if (filters.MaxLearnerChargePoints.HasValue && offer.PointCost > filters.MaxLearnerChargePoints.Value)
        {
            return false;
        }

        return true;
    }

    private static HashSet<string> BuildSkillKeys(Skill skill)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal)
        {
            SkillNormalization.NormalizeLookup(skill.Name),
            SkillNormalization.NormalizeLookup(skill.Slug)
        };

        foreach (var alias in skill.Aliases)
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                keys.Add(SkillNormalization.NormalizeLookup(alias));
            }
        }

        return keys;
    }
}
