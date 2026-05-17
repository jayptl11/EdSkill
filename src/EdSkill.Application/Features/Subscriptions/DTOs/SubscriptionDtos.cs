using EdSkill.Domain.Enums;

namespace EdSkill.Application.Features.Subscriptions.DTOs;

public record SubscriptionPlanEntitlementsDto(
    int ImmediateBonusPoints,
    int WeeklyLearnerSessionBonusPoints,
    int WeeklyCompanionSessionBonusPoints,
    decimal? LearnerTokenRewardRatePercent,
    decimal? CompanionTokenRewardRatePercent,
    int? CompanionDailySessionLimitOverride,
    string? CompanionBadgeText,
    bool HasPriorityVisibility
);

public record SubscriptionPlanDto(
    Guid PlanId,
    string Code,
    string Name,
    SubscriptionTargetRole TargetRole,
    int PriceVnd,
    string Currency,
    SubscriptionBillingCycle BillingCycle,
    IReadOnlyCollection<string> DisplayBenefits,
    SubscriptionPlanEntitlementsDto Entitlements
);

public record SubscriptionPlanListDto(IReadOnlyCollection<SubscriptionPlanDto> Data);

public record ActiveSubscriptionSummaryDto(
    Guid UserSubscriptionId,
    Guid PlanId,
    string Code,
    string Name,
    SubscriptionTargetRole TargetRole,
    UserSubscriptionStatus Status,
    DateTime StartedAt,
    DateTime ExpiresAt
);

public record ResolvedSubscriptionEntitlementsDto(
    bool HasLearnerCoverage,
    bool HasCompanionCoverage,
    string? CompanionBadgeText,
    bool HasPriorityVisibility,
    int? CompanionDailySessionLimitOverride,
    decimal? LearnerTokenRewardRatePercent,
    decimal? CompanionTokenRewardRatePercent,
    int WeeklyLearnerSessionBonusPoints,
    int WeeklyCompanionSessionBonusPoints
);

public record MySubscriptionsDto(
    IReadOnlyCollection<ActiveSubscriptionSummaryDto> ActiveSubscriptions,
    ResolvedSubscriptionEntitlementsDto Entitlements
);

public record CreateSubscriptionPurchaseRequest(Guid PlanId);

public record CreateSubscriptionPurchaseResultDto(
    Guid PaymentTransactionId,
    string PaymentUrl,
    DateTime ExpiresAt
);

public record SubscriptionPurchaseReturnResultDto(
    Guid PaymentTransactionId,
    Guid? PlanId,
    string? PlanName,
    PaymentStatus Status,
    int CreditedPoints,
    bool AlreadyProcessed
);
