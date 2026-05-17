using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities;

public class SubscriptionPlan
{
    public Guid SubscriptionPlanId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SubscriptionTargetRole TargetRole { get; set; }
    public int PriceVnd { get; set; }
    public string Currency { get; set; } = "VND";
    public SubscriptionBillingCycle BillingCycle { get; set; } = SubscriptionBillingCycle.Monthly;
    public int ImmediateBonusPoints { get; set; }
    public int WeeklyLearnerSessionBonusPoints { get; set; }
    public int WeeklyCompanionSessionBonusPoints { get; set; }
    public decimal? LearnerTokenRewardRatePercent { get; set; }
    public decimal? CompanionTokenRewardRatePercent { get; set; }
    public int? CompanionDailySessionLimitOverride { get; set; }
    public string? CompanionBadgeText { get; set; }
    public bool HasPriorityVisibility { get; set; }
    public string BenefitsJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}
