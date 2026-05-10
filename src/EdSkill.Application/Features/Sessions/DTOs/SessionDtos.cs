using EdSkill.Domain.Enums;

namespace EdSkill.Application.Features.Sessions.DTOs;

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
    SessionDeliveryMode DeliveryMode,
    string? Location,
    int DurationMinutes,
    int PointCost,
    DateTime ScheduledAt
);

public record RejectSessionRequest(string? Reason);

public record CancelSessionRequest(string? Reason);

public record LeaveSessionRequest(int? ActualDuration);
