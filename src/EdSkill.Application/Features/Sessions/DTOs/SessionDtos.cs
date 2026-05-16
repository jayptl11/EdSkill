using EdSkill.Domain.Enums;

namespace EdSkill.Application.Features.Sessions.DTOs;

public record SessionPricingPreviewDto(
    int MinCompanionPayoutPoints,
    int MaxCompanionPayoutPoints,
    int MinLearnerChargePoints,
    int MaxLearnerChargePoints,
    int MinPlatformFeePoints,
    int MaxPlatformFeePoints
);

public record SessionPricingBreakdownDto(
    int LearnerChargePoints,
    int CompanionPayoutPoints,
    int PlatformFeePoints,
    int? SkillBasePoints,
    int? CredentialBonusPoints,
    int? DurationMultiplierPercent
);

public record SessionDurationPricingOptionDto(
    int DurationMinutes,
    int LearnerChargePoints,
    int CompanionPayoutPoints,
    int PlatformFeePoints,
    int DurationMultiplierPercent,
    bool IsSelected
);

public record SessionDto(
    Guid SessionId,
    Guid CompanionId,
    Guid? LearnerId,
    string Skill,
    string? Description,
    SessionDeliveryMode DeliveryMode,
    string? Location,
    int DurationMinutes,
    int PointCost,
    SessionPricingModel PricingModel,
    IReadOnlyCollection<int> DurationOptions,
    IReadOnlyCollection<SessionDurationPricingOptionDto> DurationPricingOptions,
    int? SelectedDurationMinutes,
    SessionPricingPreviewDto PricingPreview,
    SessionPricingBreakdownDto? PricingBreakdown,
    DateTime ScheduledAt,
    SessionStatus Status,
    string? JitsiRoomId,
    DateTime? ActualStartAt,
    DateTime? ActualEndAt,
    int? ActualDuration,
    bool LearnerConfirmed,
    bool CompanionConfirmed,
    string? CancelReason,
    DateTime? CancelledAt,
    DateTime? DisbursedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record SessionListDto(
    IReadOnlyCollection<SessionDto> Data,
    int Total,
    int Page,
    int Limit
);

public record SessionStatusDto(
    SessionStatus Status,
    bool LearnerConfirmed,
    bool CompanionConfirmed
);

public record CreateSessionRequest(
    Guid SkillId,
    string? Description,
    IReadOnlyCollection<int> DurationOptions,
    DateTime ScheduledAt
);

public record BookSessionRequest(int SelectedDurationMinutes);

public record RejectSessionRequest(string? Reason);

public record CancelSessionRequest(string? Reason);

public record LeaveSessionRequest(int? ActualDuration);
