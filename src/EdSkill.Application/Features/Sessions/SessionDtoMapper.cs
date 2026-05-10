using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Sessions;

public static class SessionDtoMapper
{
    public static SessionDto Map(Session session)
    {
        return new SessionDto(
            session.SessionId,
            session.CompanionId,
            session.LearnerId,
            session.Skill,
            session.Description,
            session.DurationMinutes,
            session.PointCost,
            session.ScheduledAt,
            session.Status,
            session.JitsiRoomId,
            session.ActualStartAt,
            session.ActualEndAt,
            session.ActualDuration,
            session.LearnerConfirmed,
            session.CompanionConfirmed,
            session.CancelReason,
            session.CancelledAt,
            session.DisbursedAt,
            session.CreatedAt,
            session.UpdatedAt);
    }
}
