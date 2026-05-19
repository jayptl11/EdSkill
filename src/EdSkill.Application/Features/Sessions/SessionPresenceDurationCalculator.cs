using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Sessions;

internal static class SessionPresenceDurationCalculator
{
    public static int CalculateSharedMinutes(
        IReadOnlyCollection<SessionPresenceSegment> learnerSegments,
        IReadOnlyCollection<SessionPresenceSegment> companionSegments)
    {
        if (learnerSegments.Count == 0 || companionSegments.Count == 0)
        {
            return 0;
        }

        var totalOverlap = TimeSpan.Zero;

        foreach (var learnerSegment in learnerSegments)
        {
            if (!learnerSegment.LeftAt.HasValue)
            {
                continue;
            }

            foreach (var companionSegment in companionSegments)
            {
                if (!companionSegment.LeftAt.HasValue)
                {
                    continue;
                }

                var overlapStart = learnerSegment.JoinedAt > companionSegment.JoinedAt
                    ? learnerSegment.JoinedAt
                    : companionSegment.JoinedAt;
                var overlapEnd = learnerSegment.LeftAt.Value < companionSegment.LeftAt.Value
                    ? learnerSegment.LeftAt.Value
                    : companionSegment.LeftAt.Value;

                if (overlapEnd > overlapStart)
                {
                    totalOverlap += overlapEnd - overlapStart;
                }
            }
        }

        return Math.Max(0, (int)Math.Round(totalOverlap.TotalMinutes));
    }
}
