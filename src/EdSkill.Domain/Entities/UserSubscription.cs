using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities;

public class UserSubscription
{
    public Guid UserSubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public virtual User? User { get; set; }
    public Guid PlanId { get; set; }
    public virtual SubscriptionPlan? Plan { get; set; }
    public Guid? PaymentTransactionId { get; set; }
    public virtual PaymentTransaction? PaymentTransaction { get; set; }
    public UserSubscriptionStatus Status { get; set; } = UserSubscriptionStatus.Active;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
