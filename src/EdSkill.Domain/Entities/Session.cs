using EdSkill.Domain.Enums;

namespace EdSkill.Domain.Entities;

public class Session
{
    public Guid SessionId { get; set; }
    public Guid CompanionId { get; set; }
    public virtual User Companion { get; set; } = null!;
    public Guid? LearnerId { get; set; }
    public virtual User? Learner { get; set; }
    public Guid? SkillId { get; set; }
    public string Skill { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SessionDeliveryMode DeliveryMode { get; set; } = SessionDeliveryMode.Online;
    public string? Location { get; set; }
    public int DurationMinutes { get; set; }
    public int PointCost { get; set; }
    public SessionPricingModel PricingModel { get; set; } = SessionPricingModel.LegacyManual;
    public List<int> DurationOptions { get; set; } = new();
    public int? SelectedDurationMinutes { get; set; }
    public int? CompanionPayoutPoints { get; set; }
    public int? LearnerChargePoints { get; set; }
    public int? PlatformFeePoints { get; set; }
    public int? SkillBasePointsSnapshot { get; set; }
    public int? CredentialBonusPointsSnapshot { get; set; }
    public int? DurationMultiplierPercentSnapshot { get; set; }
    public DateTime ScheduledAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Available;
    public string? JitsiRoomId { get; set; }
    public DateTime? ActualStartAt { get; set; }
    public DateTime? ActualEndAt { get; set; }
    public int? ActualDuration { get; set; }
    public bool LearnerConfirmed { get; set; }
    public bool CompanionConfirmed { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? DisbursedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
