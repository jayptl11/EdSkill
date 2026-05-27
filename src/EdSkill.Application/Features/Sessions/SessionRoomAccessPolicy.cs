using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;

namespace EdSkill.Application.Features.Sessions;

public static class SessionRoomAccessPolicy
{
    public const string JitsiDomain = "meet.jit.si";

    public static int ResolveDurationMinutes(Session session)
    {
        return session.SelectedDurationMinutes ?? session.DurationMinutes;
    }

    public static SessionJoinWindow BuildJoinWindow(Session session, int joinEarlyMinutes, int joinLateGraceMinutes)
    {
        var durationMinutes = ResolveDurationMinutes(session);
        var joinOpenAt = session.ScheduledAt.AddMinutes(-joinEarlyMinutes);
        var joinCloseAt = session.ScheduledAt.AddMinutes(durationMinutes + joinLateGraceMinutes);
        return new SessionJoinWindow(durationMinutes, joinOpenAt, joinCloseAt);
    }

    public static SessionJoinDecision Evaluate(
        Session session,
        DateTime utcNow,
        int joinEarlyMinutes,
        int joinLateGraceMinutes,
        bool isCompanion,
        bool hasCompanionJoined)
    {
        var window = BuildJoinWindow(session, joinEarlyMinutes, joinLateGraceMinutes);

        if (session.DeliveryMode != SessionDeliveryMode.Online)
        {
            return new SessionJoinDecision(false, "SESSION_NOT_ONLINE", "Only online sessions support the session room.", window);
        }

        if (string.IsNullOrWhiteSpace(session.JitsiRoomId))
        {
            return new SessionJoinDecision(false, "SESSION_ROOM_NOT_READY", "Session room is not ready yet.", window);
        }

        if (session.Status is not (SessionStatus.Confirmed or SessionStatus.InProgress))
        {
            return new SessionJoinDecision(false, "SESSION_INVALID_STATUS", "Session room cannot be joined in the current status.", window);
        }

        if (utcNow < window.JoinOpenAt || utcNow > window.JoinCloseAt)
        {
            return new SessionJoinDecision(false, "SESSION_JOIN_WINDOW_CLOSED", "Session room is outside the allowed join window.", window);
        }

        if (!isCompanion && !hasCompanionJoined)
        {
            return new SessionJoinDecision(false, "SESSION_HOST_NOT_READY", "Companion has not opened the room yet.", window);
        }

        return new SessionJoinDecision(true, null, null, window);
    }
}

public sealed record SessionJoinWindow(
    int DurationMinutes,
    DateTime JoinOpenAt,
    DateTime JoinCloseAt
);

public sealed record SessionJoinDecision(
    bool CanJoin,
    string? DenyCode,
    string? DenyMessage,
    SessionJoinWindow Window
);
