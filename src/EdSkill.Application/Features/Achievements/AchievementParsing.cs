using EdSkill.Domain.Enums;

namespace EdSkill.Application.Features.Achievements;

internal static class AchievementParsing
{
    public static bool TryParseTrack(string? value, out AchievementTrack track)
    {
        track = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "learner" => SetTrack(AchievementTrack.Learner, out track),
            "companion" => SetTrack(AchievementTrack.Companion, out track),
            _ => false
        };
    }

    public static bool TryParseMetric(string? value, out AchievementMetric metric)
    {
        metric = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "completed_sessions" => SetMetric(AchievementMetric.CompletedSessions, out metric),
            "completed_hours" => SetMetric(AchievementMetric.CompletedHours, out metric),
            "distinct_completed_learners" => SetMetric(AchievementMetric.DistinctCompletedLearners, out metric),
            _ => false
        };
    }

    public static string ToApiValue(AchievementTrack track)
    {
        return track switch
        {
            AchievementTrack.Learner => "learner",
            AchievementTrack.Companion => "companion",
            _ => track.ToString().ToLowerInvariant()
        };
    }

    public static string ToApiValue(AchievementMetric metric)
    {
        return metric switch
        {
            AchievementMetric.CompletedSessions => "completed_sessions",
            AchievementMetric.CompletedHours => "completed_hours",
            AchievementMetric.DistinctCompletedLearners => "distinct_completed_learners",
            _ => metric.ToString().ToLowerInvariant()
        };
    }

    private static bool SetTrack(AchievementTrack value, out AchievementTrack track)
    {
        track = value;
        return true;
    }

    private static bool SetMetric(AchievementMetric value, out AchievementMetric metric)
    {
        metric = value;
        return true;
    }
}
