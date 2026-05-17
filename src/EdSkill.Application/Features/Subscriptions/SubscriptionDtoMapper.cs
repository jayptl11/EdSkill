using System.Text.Json;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Subscriptions.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Subscriptions;

public static class SubscriptionDtoMapper
{
    public static SubscriptionPlanDto MapPlan(SubscriptionPlan plan)
    {
        return new SubscriptionPlanDto(
            plan.SubscriptionPlanId,
            plan.Code,
            plan.Name,
            plan.TargetRole,
            plan.PriceVnd,
            plan.Currency,
            plan.BillingCycle,
            ParseDisplayBenefits(plan.BenefitsJson),
            new SubscriptionPlanEntitlementsDto(
                plan.ImmediateBonusPoints,
                plan.WeeklyLearnerSessionBonusPoints,
                plan.WeeklyCompanionSessionBonusPoints,
                plan.LearnerTokenRewardRatePercent,
                plan.CompanionTokenRewardRatePercent,
                plan.CompanionDailySessionLimitOverride,
                plan.CompanionBadgeText,
                plan.HasPriorityVisibility));
    }

    public static ActiveSubscriptionSummaryDto MapActiveSubscription(ActiveUserSubscription subscription)
    {
        return new ActiveSubscriptionSummaryDto(
            subscription.UserSubscriptionId,
            subscription.PlanId,
            subscription.Code,
            subscription.Name,
            subscription.TargetRole,
            subscription.Status,
            subscription.StartedAt,
            subscription.ExpiresAt);
    }

    public static ResolvedSubscriptionEntitlementsDto MapEntitlements(ResolvedSubscriptionEntitlements entitlements)
    {
        return new ResolvedSubscriptionEntitlementsDto(
            entitlements.HasLearnerCoverage,
            entitlements.HasCompanionCoverage,
            entitlements.CompanionBadgeText,
            entitlements.HasPriorityVisibility,
            entitlements.CompanionDailySessionLimitOverride,
            entitlements.LearnerTokenRewardRatePercent,
            entitlements.CompanionTokenRewardRatePercent,
            entitlements.WeeklyLearnerSessionBonusPoints,
            entitlements.WeeklyCompanionSessionBonusPoints);
    }

    private static IReadOnlyCollection<string> ParseDisplayBenefits(string benefitsJson)
    {
        if (string.IsNullOrWhiteSpace(benefitsJson))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(benefitsJson) ?? [];
    }
}
