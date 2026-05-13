using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Common.Services;

public sealed class SessionPricingService : ISessionPricingService
{
    private static readonly IReadOnlyDictionary<int, int> DurationMultiplierPercents = new Dictionary<int, int>
    {
        [30] = 60,
        [45] = 75,
        [60] = 100,
        [90] = 140,
        [120] = 180
    };

    private readonly ISystemConfigService _systemConfigService;

    public SessionPricingService(ISystemConfigService systemConfigService)
    {
        _systemConfigService = systemConfigService;
    }

    public Task<int> GetPlatformMarkupPctAsync(CancellationToken cancellationToken)
    {
        return _systemConfigService.GetIntValueAsync(SystemConfigKeys.PointPlatformMarkupPct, cancellationToken);
    }

    public Result<SessionPricingPreview> BuildOfferPreview(
        Skill skill,
        int credentialCount,
        IReadOnlyCollection<int> durationOptions,
        int platformMarkupPct)
    {
        return TryBuildOfferPreview(skill, credentialCount, durationOptions, platformMarkupPct);
    }

    public Result<FormulaSessionPricingSnapshot> BuildBookingSnapshot(
        Skill skill,
        int credentialCount,
        int selectedDurationMinutes,
        int platformMarkupPct)
    {
        return TryBuildBookingSnapshot(skill, credentialCount, selectedDurationMinutes, platformMarkupPct);
    }

    public static Result<SessionPricingPreview> TryBuildOfferPreview(
        Skill skill,
        int credentialCount,
        IReadOnlyCollection<int> durationOptions,
        int platformMarkupPct)
    {
        var normalizedDurations = NormalizeDurations(durationOptions);
        if (normalizedDurations.Count == 0)
        {
            return Result<SessionPricingPreview>.Failure("INVALID_DURATION_OPTIONS", "Duration options are invalid.");
        }

        if (skill.BasePointCost <= 0)
        {
            return Result<SessionPricingPreview>.Failure("SKILL_BASE_POINTS_INVALID", "Skill base points are invalid.");
        }

        var credentialBonus = GetCredentialBonus(credentialCount);
        var calculations = normalizedDurations
            .Select(duration => CalculateBreakdown(skill.BasePointCost, credentialBonus, duration, platformMarkupPct))
            .ToList();

        return Result<SessionPricingPreview>.Success(new SessionPricingPreview(
            calculations.Min(item => item.CompanionPayoutPoints),
            calculations.Max(item => item.CompanionPayoutPoints),
            calculations.Min(item => item.LearnerChargePoints),
            calculations.Max(item => item.LearnerChargePoints),
            calculations.Min(item => item.PlatformFeePoints),
            calculations.Max(item => item.PlatformFeePoints)));
    }

    public static Result<FormulaSessionPricingSnapshot> TryBuildBookingSnapshot(
        Skill skill,
        int credentialCount,
        int selectedDurationMinutes,
        int platformMarkupPct)
    {
        if (!DurationMultiplierPercents.ContainsKey(selectedDurationMinutes))
        {
            return Result<FormulaSessionPricingSnapshot>.Failure("INVALID_SELECTED_DURATION", "Selected duration is invalid.");
        }

        if (skill.BasePointCost <= 0)
        {
            return Result<FormulaSessionPricingSnapshot>.Failure("SKILL_BASE_POINTS_INVALID", "Skill base points are invalid.");
        }

        var credentialBonus = GetCredentialBonus(credentialCount);
        return Result<FormulaSessionPricingSnapshot>.Success(
            CalculateBreakdown(skill.BasePointCost, credentialBonus, selectedDurationMinutes, platformMarkupPct));
    }

    public static IReadOnlyCollection<int> NormalizeDurations(IReadOnlyCollection<int>? durations)
    {
        if (durations is null || durations.Count == 0)
        {
            return [];
        }

        return durations
            .Where(DurationMultiplierPercents.ContainsKey)
            .Distinct()
            .OrderBy(value => value)
            .ToList();
    }

    public static int GetCredentialBonus(int credentialCount)
    {
        return credentialCount switch
        {
            <= 0 => 0,
            1 => 75,
            2 => 150,
            _ => 250
        };
    }

    public static SessionPricingPreview BuildLegacyPreview(int learnerChargePoints)
    {
        return new SessionPricingPreview(
            0,
            0,
            learnerChargePoints,
            learnerChargePoints,
            0,
            0);
    }

    private static FormulaSessionPricingSnapshot CalculateBreakdown(
        int skillBasePoints,
        int credentialBonusPoints,
        int durationMinutes,
        int platformMarkupPct)
    {
        var durationMultiplierPercent = DurationMultiplierPercents[durationMinutes];
        var baseDurationValue = (skillBasePoints * durationMultiplierPercent + 99) / 100;
        var companionPayoutPoints = baseDurationValue + credentialBonusPoints;
        var learnerChargePoints = (int)Math.Ceiling(companionPayoutPoints * (100m + platformMarkupPct) / 100m);
        var platformFeePoints = learnerChargePoints - companionPayoutPoints;

        return new FormulaSessionPricingSnapshot(
            durationMinutes,
            companionPayoutPoints,
            learnerChargePoints,
            platformFeePoints,
            skillBasePoints,
            credentialBonusPoints,
            durationMultiplierPercent);
    }
}
