using EdSkill.Domain.Enums;

namespace EdSkill.Application.Common.Models;

public record ActiveUserSubscription(
    Guid UserSubscriptionId,
    Guid PlanId,
    string Code,
    string Name,
    SubscriptionTargetRole TargetRole,
    UserSubscriptionStatus Status,
    DateTime StartedAt,
    DateTime ExpiresAt,
    string? CompanionBadgeText,
    bool HasPriorityVisibility
);

public record ResolvedSubscriptionEntitlements(
    IReadOnlyCollection<ActiveUserSubscription> ActiveSubscriptions,
    bool HasLearnerCoverage,
    bool HasCompanionCoverage,
    string? CompanionBadgeText,
    bool HasPriorityVisibility,
    int? CompanionDailySessionLimitOverride,
    decimal? LearnerTokenRewardRatePercent,
    decimal? CompanionTokenRewardRatePercent,
    int WeeklyLearnerSessionBonusPoints,
    int WeeklyCompanionSessionBonusPoints
)
{
    public static ResolvedSubscriptionEntitlements Empty { get; } = new(
        [],
        false,
        false,
        null,
        false,
        null,
        null,
        null,
        0,
        0);
}

public record SubscriptionActivationResult(
    Guid UserSubscriptionId,
    Guid PlanId,
    string PlanName,
    DateTime StartedAt,
    DateTime ExpiresAt,
    int ImmediateBonusPoints
);

public record SubscriptionWeeklyBonusResult(
    int LearnerBonusPoints,
    int CompanionBonusPoints
);
